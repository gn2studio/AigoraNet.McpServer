using AigoraNet.Common.DTO;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common;

public class Comment : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(255)")]
    public string Key { get; set; } = string.Empty;

    public string Content { get; set; } =  string.Empty;

    public int Depth { get; set; }

    [Column(TypeName = "char(36)")]
    public string? ParentId { get; set; }

    [NotMapped]
    public int LikeCount
    {
        get
        {
            return Historian?.Count(x => x.HistoryType == CommentHistoryType.Like) ?? 0;
        }
    }

    [NotMapped]
    public int UnLikeCount
    {
        get
        {
            return Historian?.Count(x => x.HistoryType == CommentHistoryType.UnLike) ?? 0;
        }
    }

    [Required]
    [Column(TypeName = "char(36)")]
    public string OwnerId { get; set; } = string.Empty;

    [NotMapped]
    public MemberDTO? Owner { get; set; }

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();

    [JsonIgnore]
    public ICollection<CommentHistory> Historian { get; set; } = new HashSet<CommentHistory>();

    [JsonIgnore]
    [NotMapped]
    public List<Comment> SubComments { get; set; } = new List<Comment>();

}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.Historian)
                   .WithOne(x => x.Comment)
                   .HasForeignKey(x => x.CommentId);

        builder.OwnsOne(x => x.Condition, y =>
        {
            y.Property(b => b.CreatedBy).IsRequired();
            y.Property(b => b.IsEnabled).HasDefaultValue(true);
            y.Property(b => b.Status).HasDefaultValue(ConditionStatus.Active).HasSentinel(ConditionStatus.Active);
            y.Property(b => b.RegistDate).HasDefaultValueSql("getutcdate()");
            y.Property(b => b.LastUpdate).HasDefaultValueSql("getutcdate()");
            y.Property(b => b.DeletedDate).HasDefaultValueSql("getutcdate()");
        });
    }
}
