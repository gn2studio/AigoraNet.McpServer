using AigoraNet.Common;
using AigoraNet.Common.Abstracts;
using AigoraNet.Common.Configurations;
using AigoraNet.Common.CQRS;
using AigoraNet.Common.CQRS.Auth;
using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.Common.CQRS.Tokens;
using AigoraNet.Common.DTO;
using AigoraNet.Common.Services;
using AigoraNet.McpServer.Services;
using AigoraNet.McpServer.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

var basePath = AppContext.BaseDirectory;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Configure logging
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

const string defaultTokenHeader = "X-Token-Key";
var configuredTokenHeader = builder.Configuration.GetSection(nameof(ClientConfiguration)).GetValue<string>(nameof(ClientConfiguration.AccessToken))?.Trim();
var tokenHeaderName = string.IsNullOrWhiteSpace(configuredTokenHeader)
    ? defaultTokenHeader
    : configuredTokenHeader;

// Configure services
builder.Services.Configure<DatabaseConnectionStrings>(builder.Configuration.GetSection(DatabaseConnectionStrings.Name));
builder.Services.Configure<AzureBlobSettings>(builder.Configuration.GetSection(nameof(AzureBlobSettings)));
builder.Services.Configure<RedisConfiguration>(builder.Configuration.GetSection(nameof(RedisConfiguration)));
builder.Services.Configure<ClientConfiguration>(builder.Configuration.GetSection(nameof(ClientConfiguration)));
builder.Services.Configure<SmtpConfiguration>(builder.Configuration.GetSection(nameof(SmtpConfiguration)));

// Resolve database connection string from configuration (env > appsettings)
var configDbSection = builder.Configuration.GetSection(DatabaseConnectionStrings.Name);
var connectionString = configDbSection.GetValue<string>(nameof(DatabaseConnectionStrings.ConnectionString));

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Database connection string not found. Set Database__ConnectionString environment variable or configure Database:ConnectionString.");

var useInMemory = connectionString.StartsWith("InMemory", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(connectionString, "UseInMemory", StringComparison.OrdinalIgnoreCase);

if (useInMemory || builder.Environment.IsEnvironment("Testing"))
{
    connectionString = "UseInMemory";
    builder.Services.AddDbContext<DefaultContext>(options =>
        options.UseInMemoryDatabase("McpServerTests"));
}
else
{
    builder.Services.AddDbContext<DefaultContext>(options =>
        options.UseSqlServer(connectionString, b => b.MigrationsAssembly("AigoraNet.Common")));
}

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPromptCache, InMemoryPromptCache>();
builder.Services.AddSingleton<IAzureBlobFileService, AzureBlobFileService>();

// MCP tools (still available for stdio/local use)
builder.Services
    .AddMcpServer()
    .WithTools<RandomNumberTools>()
    .WithTools<TokenManagementTools>();

var app = builder.Build();

// Basic health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// List tools over HTTP (no auth required for discovery)
app.MapGet("/listtools", () =>
{
    var tools = new[]
    {
        new ToolDescriptor(
            "get_random_number",
            "Generates a random number between the specified minimum and maximum values.",
            new ToolInputSchema(
                "object",
                new Dictionary<string, ToolInputProperty>
                {
                    { "min", new ToolInputProperty("integer", "Minimum value (inclusive)") },
                    { "max", new ToolInputProperty("integer", "Maximum value (exclusive)") }
                },
                Array.Empty<string>(),
                additionalProperties: false)),
        new ToolDescriptor(
            "list_tokens_for_owner",
            "Lists all tokens for the owner of the provided token. Returns token metadata with masked keys for security.",
            new ToolInputSchema(
                "object",
                new Dictionary<string, ToolInputProperty>
                {
                    { "tokenKey", new ToolInputProperty("string", "The token key to identify the owner") }
                },
                new[] { "tokenKey" },
                additionalProperties: false)),
        new ToolDescriptor(
            "get_prompts_for_token",
            "Gets all prompts (prompt templates) that are mapped to a specific token.",
            new ToolInputSchema(
                "object",
                new Dictionary<string, ToolInputProperty>
                {
                    { "tokenKey", new ToolInputProperty("string", "The token key to retrieve prompts for") }
                },
                new[] { "tokenKey" },
                additionalProperties: false))
    };

    return Results.Json(new { tools });
});

// Token validation middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;
    if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, "/listtools", StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, "/listTools", StringComparison.OrdinalIgnoreCase))
    {
        await next(context);
        return;
    }

    if (!context.Request.Headers.TryGetValue(tokenHeaderName, out var tokenHeader) || string.IsNullOrWhiteSpace(tokenHeader))
    {
        var tokenQuery = context.Request.Query["token"].FirstOrDefault() ?? context.Request.Query["tokenKey"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tokenQuery))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token key required");
            return;
        }

        tokenHeader = tokenQuery;
    }

    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<ValidateTokenQuery>();
    var db = context.RequestServices.GetRequiredService<DefaultContext>();
    var validation = await ValidateTokenHandler.Handle(new ValidateTokenQuery(tokenHeader!), db, logger, context.RequestAborted);

    if (!validation.Success)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync(validation.Error ?? "Invalid token");
        return;
    }

    context.Items["TokenKey"] = tokenHeader.ToString();
    context.Items["MemberId"] = validation.MemberId ?? string.Empty;
    await next(context);
});

// Prompt match (keyword-based)
app.MapPost("/mcp/prompts/match", async (
    GetPromptRequest request,
    DefaultContext db,
    IPromptCache cache,
    ILogger<GetPromptByKeywordQuery> logger,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Requirement))
    {
        return Results.BadRequest("Requirement is required");
    }

    var result = await GetPromptByKeywordHandler.Handle(
        new GetPromptByKeywordQuery(request.Requirement, request.Locale, request.AllowRegex),
        db,
        cache,
        logger,
        ct);

    if (!result.Success)
    {
        return Results.NotFound(result);
    }

    return Results.Ok(result);
});

// Prompts mapped to a token
app.MapGet("/mcp/prompts/{tokenKey}", async (
    string tokenKey,
    DefaultContext db,
    ILogger<GetPromptsForTokenQuery> logger,
    CancellationToken ct) =>
{
    var query = new GetPromptsForTokenQuery(tokenKey);
    var result = await TokenQueryHandlers.Handle(query, db, logger, ct);
    if (!result.Success)
    {
        return Results.BadRequest(result.Error ?? "Failed to get prompts");
    }

    return Results.Ok(result.Prompts ?? Array.Empty<PromptTemplateDTO>());
});

// Token metadata
app.MapGet("/mcp/tokens/{tokenKey}", async (
    string tokenKey,
    DefaultContext db,
    CancellationToken ct) =>
{
    var result = await TokenHandlers.Handle(new GetTokenQuery(tokenKey), db, ct);
    if (!result.Success)
    {
        return Results.NotFound(result.Error);
    }

    return Results.Ok(new
    {
        token = result.Token,
        status = result.Token?.Status.ToString(),
        expiresAt = result.Token?.ExpiresAt,
        lastUsedAt = result.Token?.LastUsedAt
    });
});

app.Run();

internal record GetPromptRequest(string Requirement, string? Locale = null, bool AllowRegex = true);
internal record ToolDescriptor(string name, string description, ToolInputSchema inputSchema);
internal record ToolInputSchema(string type, IDictionary<string, ToolInputProperty> properties, string[] required, bool additionalProperties);
internal record ToolInputProperty(string type, string? description = null);

public partial class Program { }
