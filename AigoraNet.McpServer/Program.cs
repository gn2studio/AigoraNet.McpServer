using AigoraNet.Common;
using AigoraNet.Common.CQRS;
using AigoraNet.McpServer.Services;
using AigoraNet.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Configure database context
// Connection string can be provided via environment variable: AIGORANET_CONNECTION_STRING
var connectionString = Environment.GetEnvironmentVariable("AIGORANET_CONNECTION_STRING")
    ?? builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not found. Set AIGORANET_CONNECTION_STRING environment variable.");

builder.Services.AddDbContext<DefaultContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("AigoraNet.Common")));

// Register cache service
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPromptCache, InMemoryPromptCache>();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RandomNumberTools>()
    .WithTools<TokenManagementTools>();

await builder.Build().RunAsync();
