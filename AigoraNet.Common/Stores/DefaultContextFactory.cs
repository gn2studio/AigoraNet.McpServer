using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AigoraNet.Common.Stores;

/// <summary>
/// Design-time factory for DefaultContext to support EF Core migrations.
/// </summary>
public class DefaultContextFactory : IDesignTimeDbContextFactory<DefaultContext>
{
    public DefaultContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DefaultContext>();
        
        // Use a dummy connection string for migrations
        // In production, this will be overridden by the actual connection string
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AigoraNet;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new DefaultContext(optionsBuilder.Options);
    }
}
