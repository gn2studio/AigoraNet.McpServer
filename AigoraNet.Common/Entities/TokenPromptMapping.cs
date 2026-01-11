using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common.Entities;

public class TokenPromptMapping : BaseEntity
{
    [Required]
    [Column(TypeName = "char(36)")]
    public string TokenId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "char(36)")]
    public string PromptTemplateId { get; set; } = string.Empty;

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();

    [JsonIgnore]
    public Token? Token { get; set; }

    [JsonIgnore]
    public PromptTemplate? PromptTemplate { get; set; }
}

public class TokenPromptMappingConfiguration : IEntityTypeConfiguration<TokenPromptMapping>
{
    public void Configure(EntityTypeBuilder<TokenPromptMapping> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasIndex(t => new { t.TokenId, t.PromptTemplateId }).IsUnique();

        builder.HasOne(t => t.Token)
               .WithMany()
               .HasForeignKey(t => t.TokenId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.PromptTemplate)
               .WithMany()
               .HasForeignKey(t => t.PromptTemplateId)
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
