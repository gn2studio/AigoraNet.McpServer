using AigoraNet.Common.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AigoraNet.Common;

public class DefaultContext : DbContext, IDataProtectionKeyContext
{
    public DefaultContext(DbContextOptions options) : base(options)
    {
    }

	public DbSet<LogItem> Logs { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<FileMaster> FileMasters { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<KeywordPrompt> KeywordPrompts { get; set; }
    public DbSet<PromptTemplate> PromptTemplates { get; set; }
    public DbSet<TokenPromptMapping> TokenPromptMappings { get; set; }
    public DbSet<BoardMaster> BoardMasters { get; set; }
    public DbSet<BoardCategory> BoardCategories { get; set; }
    public DbSet<BoardContent> BoardContents { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentHistory> CommentHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new MemberConfiguration());
        builder.ApplyConfiguration(new LogItemConfiguration());
        builder.ApplyConfiguration(new FileMasterConfiguration());
        builder.ApplyConfiguration(new TokenConfiguration());
        builder.ApplyConfiguration(new KeywordPromptConfiguration());
        builder.ApplyConfiguration(new PromptTemplateConfiguration());
        builder.ApplyConfiguration(new TokenPromptMappingConfiguration());
        builder.ApplyConfiguration(new BoardMasterConfiguration());
        builder.ApplyConfiguration(new BoardCategoryConfiguration());
        builder.ApplyConfiguration(new BoardContentConfiguration());
        builder.ApplyConfiguration(new CommentConfiguration());
        builder.ApplyConfiguration(new CommentHistoryConfiguration());
    }
}
