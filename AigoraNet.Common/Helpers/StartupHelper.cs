using AigoraNet.Common.Helpers;
using AigoraNet.Common.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    public static IServiceProvider ContentMigrate(this IServiceProvider services)
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

    public static IServiceCollection RegisterLibraryServices(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection RegisterDataProtection<TDbContext>(this IServiceCollection services, string ApplicationName) where TDbContext : DbContext, IDataProtectionKeyContext
    {
        services.AddDataProtection().SetApplicationName(ApplicationName).PersistKeysToDbContext<TDbContext>();
        return services;
    }
}
