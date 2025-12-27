using AigoraNet.WebApi;
using AigoraNet.WebApi.Models;
using GN2Studio.Library.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.GetConfiguration(args);
builder.Configuration.SetLoggerConfiguration();
builder.SetSerilog();
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var configuration = new Startup(builder.Environment, builder.Configuration);
configuration.ConfigureServices(builder.Services);
var app = builder.Build();
configuration.Configure(app, app.Environment);
app.UseMiddleware<GlobalExceptionMiddleware>();
app.Run();
