using AigoraNet.WebApi;
using AigoraNet.WebApi.Models;
using GN2Studio.Library.Helpers;
using Wolverine;
using AigoraNet.Common.CQRS.Auth;
using AigoraNet.Common.CQRS.Prompts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.GetConfiguration(args);
builder.Configuration.SetLoggerConfiguration();
builder.SetSerilog();
builder.AddServiceDefaults();

builder.Host.UseWolverine(options =>
{
    options.Policies.AutoApplyTransactions();
    options.Discovery.IncludeAssembly(typeof(RegisterUserCommand).Assembly);
    options.Discovery.IncludeAssembly(typeof(GetPromptByKeywordQuery).Assembly);
});

var configuration = new Startup(builder.Environment, builder.Configuration);
configuration.ConfigureServices(builder.Services);
var app = builder.Build();
configuration.Configure(app, app.Environment);
app.UseMiddleware<GlobalExceptionMiddleware>();
app.Run();
