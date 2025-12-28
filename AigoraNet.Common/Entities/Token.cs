using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common.Entities;

public enum TokenStatus : byte
{
    Issued = 1,
    Revoked = 2,
    Expired = 3
}

public class Token : BaseEntity
{
    [Required]
    [Column(TypeName = "varchar(128)")]
    public string TokenKey { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "char(36)")]
    public string MemberId { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(100)")]
    public string? Name { get; set; }

    [Required]
    public TokenStatus Status { get; set; } = TokenStatus.Issued;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();

    [JsonIgnore]
    public Member? Member { get; set; }
}

public class TokenConfiguration : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.TokenKey).IsRequired();
        builder.HasIndex(t => t.TokenKey).IsUnique();

        builder.Property(t => t.Status).HasDefaultValue(TokenStatus.Issued).HasSentinel(TokenStatus.Issued);
        builder.Property(t => t.IssuedAt).HasDefaultValueSql("getutcdate()").HasSentinel(DateTime.MinValue);

        builder.HasOne(t => t.Member)
               .WithMany(m => m.Tokens)
               .HasForeignKey(t => t.MemberId)
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
