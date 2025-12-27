using AigoraNet.Common;
using AigoraNet.Common.Abstracts;
using AigoraNet.Common.Configurations;
using AigoraNet.Common.Helpers;
using GN2.Github.Library;
using GN2Studio.Library.Helpers;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using StackExchange.Redis;

namespace AigoraNet.WebApi;

public class Startup
{
    private readonly IConfiguration Configuration;
    private readonly IWebHostEnvironment HostingEnvironment;

    private const string ProjectName = "AigoraNet.Common";

    private readonly SwaggerConfiguration _swaggerConfig;
    private readonly DatabaseConnectionStrings _databaseConnection;
    private readonly RedisConfiguration _redisConfig;
    private readonly HostSettings _hostSettings;

    public Startup(IWebHostEnvironment env, IConfiguration configuration)
    {
        this.HostingEnvironment = env;
        this.Configuration = configuration;
        this._swaggerConfig = configuration.GetSection(nameof(SwaggerConfiguration)).Get<SwaggerConfiguration>() ?? new SwaggerConfiguration();
        this._databaseConnection = configuration.GetSection(DatabaseConnectionStrings.Name).Get<DatabaseConnectionStrings>() ?? new DatabaseConnectionStrings();
        this._hostSettings = configuration.GetSection(nameof(HostSettings)).Get<HostSettings>() ?? new HostSettings();
        this._redisConfig = configuration.GetSection(nameof(RedisConfiguration)).Get<RedisConfiguration>() ?? new RedisConfiguration();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<ClientConfiguration>(Configuration.GetSection(nameof(ClientConfiguration)));
        services.Configure<AzureBlobSettings>(Configuration.GetSection(nameof(AzureBlobSettings)));
        services.Configure<SwaggerConfiguration>(Configuration.GetSection(nameof(SwaggerConfiguration)));
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
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "AigoraNet MCP Server API";
                document.Info.Version = "v1";
                document.Info.Description = "AigoraNet MCP Server의 API 문서입니다. 로그인 후 발급된 Access Token을 Authorize 버튼에 입력하세요.";

                //// 1. 보안 스키마 정의 (Bearer Token)
                //document.Components ??= new OpenApiComponents();
                //var bearerScheme = new OpenApiSecurityScheme
                //{
                //    Type = SecuritySchemeType.Http,
                //    Name = "Authorization",
                //    In = ParameterLocation.Header,
                //    Scheme = "bearer",
                //    BearerFormat = "JWT",
                //    Description = "JWT Access Token을 입력하세요. (예: 'abc12345...')"
                //};

                //document.Components.SecuritySchemes["Bearer"] = bearerScheme;

                //// 2. 모든 API 엔드포인트에 전역적으로 보안 요구 사항 적용
                //document.Security = new List<OpenApiSecurityRequirement>();
                return Task.CompletedTask;
            });
        });
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
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("AigoraNet API Reference")
                   .WithTheme(ScalarTheme.DeepSpace);
        });
        app.MapGet("/", () => Results.Redirect("/scalar/v1"));
        app.Services.DatabaseMigrate();
    }

}