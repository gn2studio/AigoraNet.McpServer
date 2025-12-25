using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace AigoraNet.Common.Stores;

public abstract class DefaultContext : DbContext
{
    public DefaultContext(DbContextOptions options) : base(options)
    {
    }

	public DbSet<LogItem> Logs { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<FileMaster> FileMasters { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new MemberConfiguration());
        builder.ApplyConfiguration(new LogItemConfiguration());
        builder.ApplyConfiguration(new FileMasterConfiguration());
    }
}
