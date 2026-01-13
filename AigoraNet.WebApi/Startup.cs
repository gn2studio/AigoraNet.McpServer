using AigoraNet.Common;
using AigoraNet.Common.Abstracts;
using AigoraNet.Common.Configurations;
using AigoraNet.Common.CQRS;
using AigoraNet.Common.Services;
using AigoraNet.WebApi.Authorization;
using AigoraNet.WebApi.Middleware;
using AigoraNet.WebApi.Services;
using GN2.Common.Library.Abstracts;
using GN2.Common.Library.Helpers;
using GN2.Core.Configurations;
using GN2.Github.Library;
using GN2Studio.Library.Helpers;
using Scalar.AspNetCore;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace AigoraNet.WebApi;

public class Startup
{
    private readonly IConfiguration Configuration;
    private readonly IWebHostEnvironment HostingEnvironment;

    internal const string ProjectName = "AigoraNet.Common";
    internal const string TokenSecuritySchemeName = "TokenKey";

    private readonly DatabaseConnectionStrings _databaseConnection;
    private readonly RedisConfiguration _redisConfig;
    private readonly HostSettings _hostSettings;
    private readonly ApplicationInsights _applicationInsights;

    public Startup(IWebHostEnvironment env, IConfiguration configuration)
    {
        this.HostingEnvironment = env;
        this.Configuration = configuration;
        this._databaseConnection = configuration.GetSection(DatabaseConnectionStrings.Name).Get<DatabaseConnectionStrings>() ?? new DatabaseConnectionStrings();
        this._hostSettings = configuration.GetSection(nameof(HostSettings)).Get<HostSettings>() ?? new HostSettings();
        this._redisConfig = configuration.GetSection(nameof(RedisConfiguration)).Get<RedisConfiguration>() ?? new RedisConfiguration();
        this._applicationInsights = configuration.GetSection(nameof(ApplicationInsights)).Get<ApplicationInsights>() ?? new ApplicationInsights();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<ClientConfiguration>(Configuration.GetSection(nameof(ClientConfiguration)));
        services.Configure<AzureBlobSettings>(Configuration.GetSection(nameof(AzureBlobSettings)));
        services.Configure<DatabaseConnectionStrings>(Configuration.GetSection(DatabaseConnectionStrings.Name));
        services.Configure<SmtpConfiguration>(Configuration.GetSection(nameof(SmtpConfiguration)));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmtpConfiguration>>().Value);
        services.Configure<RedisConfiguration>(Configuration.GetSection(nameof(RedisConfiguration)));
        services.Configure<SlackWebhookConfiguration>(Configuration.GetSection(nameof(SlackWebhookConfiguration)));
        services.Configure<GitHubConfiguration>(Configuration.GetSection(nameof(GitHubConfiguration)));

        services.AddControllersWithViews()
                  .AddJsonOptions(options =>
                  {
                      options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                      options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                      options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                      options.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip;
                  });
        services.AddHttpContextAccessor();

        // Application Insights (optional)
        if (_applicationInsights != null && !string.IsNullOrWhiteSpace(_applicationInsights.ConnectionString))
        {
            services.AddApplicationInsightsTelemetry(options => { options.ConnectionString = _applicationInsights.ConnectionString; });
            services.AddSingleton<ITelemetryService, AppInsightsTelemetryService>();
        }

        services.AddMemoryCache();
        services.AddSingleton<IPromptCache, InMemoryPromptCache>();
        services.AddScoped<TokenValidationMiddleware>();
        services.AddSingleton<IAzureBlobFileService, AzureBlobFileService>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.RegisterContentDb(ProjectName, _databaseConnection.ConnectionString);
        services.RegisterIdentitySelfhost();
        services.RegisterAuthenticationSelfhost();
        services.RegisterDataProtection<DefaultContext>(ProjectName);
        services.RegisterAllowedCors(_hostSettings.AllowedHosts);
        services.RegisterLog();
        services.RegisterGithub();
        services.RegisterApiHelper();
        services.CookieSetting();
        services.AddAzureBlobService();
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(_redisConfig.ConnectionString));
        services.AddLocatorProvider();
        services.AddControllers();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(new TokenSecuritySchemeTransformer(TokenSecuritySchemeName, TokenValidationMiddleware.HttpContextTokenKey));
            options.AddOperationTransformer(new TokenSecurityOperationTransformer(TokenSecuritySchemeName));
        });
        services.AddActionBridge(
            typeof(Program).Assembly,
            typeof(AigoraNet.Common.AigoraSecret).Assembly
            );
        services.AddObjectLinker();
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseResponseCaching();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseMiddleware<TokenValidationMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors();
        app.MapControllers();
        app.MapDefaultEndpoints();
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("AigoraNet API Reference")
                   .WithTheme(ScalarTheme.DeepSpace);
        });
        app.MapGet("/", () => Results.Redirect("/scalar/v1"));
        app.Services.DatabaseMigrate();
    }
}

internal sealed class TokenSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private readonly string _schemeName;
    private readonly string _headerName;

    public TokenSecuritySchemeTransformer(string schemeName, string headerName)
    {
        _schemeName = schemeName;
        _headerName = headerName;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[_schemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = _headerName,
            Description = "발급된 토큰 키를 'X-Token-Key' 헤더에 전달하세요.",
        };

        return Task.CompletedTask;
    }
}

internal sealed class TokenSecurityOperationTransformer : IOpenApiOperationTransformer
{
    private readonly string _schemeName;

    public TokenSecurityOperationTransformer(string schemeName)
    {
        _schemeName = schemeName;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor?.EndpointMetadata;
        if (metadata is null)
        {
            return Task.CompletedTask;
        }

        if (metadata.OfType<AllowAnonymousAttribute>().Any())
        {
            return Task.CompletedTask;
        }

        var requiresAuth = metadata.OfType<AuthorizeAttribute>().Any() || metadata.OfType<AdminOnlyAttribute>().Any();
        if (!requiresAuth)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        var securityRequirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(_schemeName, null, null)
                {
                    Reference = new OpenApiReferenceWithDescription
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = _schemeName
                    }
                },
                new List<string>()
            }
        };

        operation.Security.Add(securityRequirement);

        var roles = metadata
            .OfType<AuthorizeAttribute>()
            .SelectMany(a => (a.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (metadata.OfType<AdminOnlyAttribute>().Any() && !roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            roles.Add("Admin");
        }

        if (roles.Count > 0)
        {
            var roleText = $"필요 권한: {string.Join(", ", roles)}";
            operation.Description = string.IsNullOrWhiteSpace(operation.Description)
                ? roleText
                : $"{operation.Description}\n{roleText}";
        }

        return Task.CompletedTask;
    }
}