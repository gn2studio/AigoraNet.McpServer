using AigoraNet.Common;
using AigoraNet.Common.Helpers;
using GN2.Github.Library;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Wolverine;

namespace GN2Studio.Library.Helpers;

public static class StartupHelper
{
    public static IServiceCollection RegisterContentDb(this IServiceCollection services, string AssemblyName, string ConnectionString)
    {
        services.AddDbContext<DefaultContext>(
            options => options.UseSqlServer(ConnectionString,
                b => b.MigrationsAssembly(AssemblyName)
            )
        );

        return services;
    }

    public static IServiceCollection CookieSetting(this IServiceCollection services)
    {
        services.ConfigureApplicationCookie(options =>
        {
            // Cookie settings
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
        });

        return services;
    }

    public static IServiceCollection RegisterApiHelper(this IServiceCollection services)
    {
        services.AddHttpClient<ApiHelper>();
        return services;
    }

    public static IServiceProvider DatabaseMigrate(this IServiceProvider services)
    {
        using (var serviceScope = services.CreateScope())
        {
            var defaultContext = serviceScope.ServiceProvider.GetRequiredService<DefaultContext>();

            if (defaultContext != null)
            {
                defaultContext.Database.Migrate();
            }
        }

        return services;
    }

    public static IServiceCollection AddLocatorProvider(this IServiceCollection services)
    {
        IServiceProvider locatorProvider = services.BuildServiceProvider();
        ServiceLocator.SetLocatorProvider(locatorProvider);
        return services;
    }

    public static IServiceCollection RegisterDataProtection<TDbContext>(this IServiceCollection services, string ApplicationName) where TDbContext : DbContext, IDataProtectionKeyContext
    {
        services.AddDataProtection().SetApplicationName(ApplicationName).PersistKeysToDbContext<TDbContext>();
        return services;
    }

    public static IServiceCollection AddAzureBlobService(this IServiceCollection services)
    {
        //services.AddSingleton<IAzureBlobFileService, AzureBlobFileService>();
        return services;
    }

    public static IServiceCollection RegisterIdentitySelfhost(this IServiceCollection services)
    {
        services.Configure<IdentityOptions>(options =>
        {
            // Password settings.
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings.
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        });

        return services;
    }

    public static IServiceCollection RegisterAuthenticationSelfhost(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AigoraSecret.JwtAuthKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true,
                ValidateLifetime = true
            };
        });
        return services;
    }

    public static IServiceCollection RegisterAllowedCors(this IServiceCollection services, string allowedHosts = "")
    {
        string[] AllowCors = allowedHosts.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (AllowCors != null && AllowCors.Count() > 0)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(AllowCors);
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
            });
        }
        else
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
            });
        }

        return services;
    }

    public static IServiceCollection RegisterLog(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        });

        return services;
    }

    public static IConfiguration GetConfiguration(this IHostApplicationBuilder app, string[] args)
    {
        string text = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development";
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile("appsettings." + text + ".json", optional: true, reloadOnChange: true);
        configurationBuilder.AddCommandLine(args);
        configurationBuilder.AddEnvironmentVariables();
        return configurationBuilder.Build();
    }

    public static IServiceCollection RegisterGithub(this IServiceCollection services)
    {
        services.AddOptions<GitHubConfiguration>()
        .Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetSection(GitHubConfiguration.Name).Bind(options);
        });

        services.AddTransient<GitHubService>();

        services.AddHttpClient(GitHubService.HTTP_CLIENT_GITHUB_API, c =>
        {
            c.BaseAddress = new Uri(GitHubService.BASE_URL);
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,
        });

        return services;
    }

    public static void SetLoggerConfiguration(this IConfiguration _config)
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(_config).CreateLogger();
    }

    public static void SetSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog();
        builder.Services.AddSingleton(Log.Logger);
    }


}
