using AigoraNet.Common;
using AigoraNet.Common.Abstracts;
using AigoraNet.Common.Configurations;
using AigoraNet.Common.CQRS;
using AigoraNet.Common.Services;
using AigoraNet.WebApi.Middleware;
using AigoraNet.WebApi.Services;
using GN2.Common.Library.Abstracts;
using GN2.Common.Library.Helpers;
using GN2.Core.Configurations;
using GN2.Github.Library;
using GN2Studio.Library.Helpers;
using Scalar.AspNetCore;
using StackExchange.Redis;

namespace AigoraNet.WebApi;

public class Startup
{
    private readonly IConfiguration Configuration;
    private readonly IWebHostEnvironment HostingEnvironment;

    private const string ProjectName = "AigoraNet.Common";

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
        services.AddOpenApi();
        services.AddActionBridge(typeof(Program).Assembly);
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