using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common.Entities;

public class KeywordPrompt : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string Keyword { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(10)")]
    public string? Locale { get; set; }

    public bool IsRegex { get; set; } = false;

    [Column(TypeName = "nvarchar(512)")]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "char(36)")]
    public string PromptTemplateId { get; set; } = string.Empty;

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();

    [JsonIgnore]
    public PromptTemplate? PromptTemplate { get; set; }
}

public class KeywordPromptConfiguration : IEntityTypeConfiguration<KeywordPrompt>
{
    public void Configure(EntityTypeBuilder<KeywordPrompt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(k => k.IsRegex).HasDefaultValue(false).HasSentinel(false);

        builder.HasIndex(k => new { k.Keyword, k.Locale }).IsUnique();

        builder.HasOne(k => k.PromptTemplate)
               .WithMany(p => p.KeywordPrompts)
               .HasForeignKey(k => k.PromptTemplateId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(x => x.Condition, y =>
        {
            y.Property(b => b.CreatedBy).IsRequired();
            y.Property(b => b.IsEnabled).HasDefaultValue(true).HasSentinel(true);
            y.Property(b => b.Status).HasDefaultValue(ConditionStatus.Active).HasSentinel(ConditionStatus.Active);
            y.Property(b => b.RegistDate).HasDefaultValueSql("getutcdate()");
            y.Property(b => b.LastUpdate).HasDefaultValueSql("getutcdate()");
            y.Property(b => b.DeletedDate).HasDefaultValueSql("getutcdate()");
        });
    }
}
