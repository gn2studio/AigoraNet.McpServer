using AigoraNet.Common;
using AigoraNet.Common.Abstracts;
using AigoraNet.Common.Configurations;
using AigoraNet.Common.Helpers;
using GN2.Github.Library;
using GN2Studio.Library.Helpers;
using StackExchange.Redis;

namespace AigoraNet.WebApi;

public class Startup
{
    private readonly IConfiguration Configuration;
    private readonly IWebHostEnvironment HostingEnvironment;

    private const string ProjectName = "AigoraNet.Common";

    private readonly ClientConfiguration SiteConfig;
    private readonly AzureBlobSettings AzureBlogConfiguration;
    private readonly SwaggerConfiguration SwaggerConfig;
    private readonly DatabaseConnectionStrings DatabaseConnection;
    private readonly SmtpConfiguration SmtpConfiguration;
    private readonly RedisConfiguration RedisConfig;
    private readonly SlackWebhookConfiguration SlackConfig;
    private readonly GitHubConfiguration GitHubConfiguration;
    private readonly HostSettings HostSettings;

    public Startup(IWebHostEnvironment env, IConfiguration configuration)
    {
        this.HostingEnvironment = env;
        this.Configuration = configuration;
        this.SiteConfig = configuration.GetSection(nameof(ClientConfiguration)).Get<ClientConfiguration>() ?? new ClientConfiguration();
        this.AzureBlogConfiguration = configuration.GetSection(nameof(AzureBlobSettings)).Get<AzureBlobSettings>() ?? new AzureBlobSettings();
        this.SwaggerConfig = configuration.GetSection(nameof(SwaggerConfiguration)).Get<SwaggerConfiguration>() ?? new SwaggerConfiguration();
        this.DatabaseConnection = configuration.GetSection(DatabaseConnectionStrings.Name).Get<DatabaseConnectionStrings>() ?? new DatabaseConnectionStrings();
        this.SmtpConfiguration = configuration.GetSection(nameof(SmtpConfiguration)).Get<SmtpConfiguration>() ?? new SmtpConfiguration();
        this.RedisConfig = configuration.GetSection(nameof(RedisConfiguration)).Get<RedisConfiguration>() ?? new RedisConfiguration();
        this.SlackConfig = configuration.GetSection(nameof(SlackWebhookConfiguration)).Get<SlackWebhookConfiguration>() ?? new SlackWebhookConfiguration();
        this.GitHubConfiguration = configuration.GetSection(nameof(GitHubConfiguration)).Get<GitHubConfiguration>() ?? new GitHubConfiguration();
        this.HostSettings = configuration.GetSection(nameof(HostSettings)).Get<HostSettings>() ?? new HostSettings();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews()
                  .AddJsonOptions(options =>
                  {
                      options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                      options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                      options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                      options.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip;
                  });
        services.AddSingleton(SiteConfig);
        services.AddSingleton(AzureBlogConfiguration);
        services.AddSingleton(SwaggerConfig);
        services.AddSingleton(DatabaseConnection);
        services.AddSingleton(SmtpConfiguration);
        services.AddSingleton(RedisConfig);
        services.AddSingleton(SlackConfig);
        services.AddSingleton(GitHubConfiguration);
        services.AddHttpContextAccessor();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.RegisterContentDb(ProjectName, DatabaseConnection.ConnectionString);
        services.RegisterIdentitySelfhost();
        services.RegisterAuthenticationSelfhost();
        services.RegisterDataProtection<DefaultContext>(ProjectName);
        services.RegisterAllowedCors(HostSettings.AllowedHosts);
        services.RegisterLog();
        services.RegisterGithub();
        services.RegisterApiHelper();
        services.CookieSetting();
        services.AddAzureBlobService();
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(RedisConfig.ConnectionString));
        services.AddLocatorProvider();
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseResponseCaching();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors();
        app.MapDefaultEndpoints();
        app.MapOpenApi();
        app.Services.DatabaseMigrate();
    }

}