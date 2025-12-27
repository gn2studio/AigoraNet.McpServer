using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common;

public class CommentHistory : BaseEntity
{
    [Required]
    [Column(TypeName = "char(36)")]
    public string CommentId { get; set; } = string.Empty;

    [JsonIgnore]
    public Comment? Comment { get; set; }

    [Required]
    [Column(TypeName = "char(36)")]
    public string OwnerId { get; set; } = string.Empty;

    public CommentHistoryType HistoryType { get; set; }

    public DateTime RegistDate { get; set; }
}

public enum CommentHistoryType : byte
{
    Like = 1,
    UnLike = 2
}

public class CommentHistoryConfiguration : IEntityTypeConfiguration<CommentHistory>
{
    public void Configure(EntityTypeBuilder<CommentHistory> builder)
    {
        builder.HasIndex(x => new { x.CommentId, x.OwnerId }).IsUnique(true);
        builder.Property(m => m.RegistDate).HasDefaultValueSql("getutcdate()");

        builder.HasOne(x => x.Comment)
                   .WithMany(x => x.Historian)
                   .HasForeignKey(x => x.CommentId);

    }
}