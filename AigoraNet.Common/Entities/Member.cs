using GN2.Core.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common.Entities;

public class Member : BaseEntity
{
    public enum MemberType : byte
    {
        [StringValue("User")] User = 10,
        [StringValue("Premium")] Premium = 20,
        [StringValue("Ultimate")] Ultimate = 50,
        [StringValue("Manager")] Manager = 90,
        [StringValue("Admin")] Admin = 100
    }

    [Required]
    [EmailAddress]
    [Column(TypeName = "varchar(255)")]
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsEmailConfirm { get; set; } = false;

    [JsonIgnore]
    public DateTime? EmailConfirmDate { get; set; }

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public MemberType Type { get; set; } = Member.MemberType.User;

    public string? Bio { get; set; }

    [Column(TypeName = "nvarchar(80)")]
    public string? NickName { get; set; }

    [Column(TypeName = "varchar(255)")]
    public string? Photo { get; set; }

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();

    [JsonIgnore]
    public ICollection<Token> Tokens { get; set; } = new List<Token>();
}

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.Type).HasDefaultValue(Member.MemberType.User).HasSentinel(Member.MemberType.User);
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(m => m.IsEmailConfirm).HasDefaultValue(false).HasSentinel(false);

        builder.HasMany(x => x.Tokens)
               .WithOne(t => t.Member)
               .HasForeignKey(t => t.MemberId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(x => x.Condition, y =>
        {
            y.Property(b => b.CreatedBy).IsRequired();
            y.Property(b => b.IsEnabled).HasDefaultValue(true).HasSentinel(true);
            y.Property(b => b.Status).HasDefaultValue(ConditionStatus.Active).HasSentinel(ConditionStatus.Active);
            y.Property(b => b.RegistDate).HasDefaultValueSql("getutcdate()").HasSentinel(DateTime.MinValue);
            y.Property(b => b.LastUpdate).HasDefaultValueSql("getutcdate()").HasSentinel(DateTime.MinValue);
            y.Property(b => b.DeletedDate).HasDefaultValueSql("getutcdate()").HasSentinel(DateTime.MinValue);
        });
    }
}