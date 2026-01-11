using AigoraNet.Common.DTO;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common;

public class BoardContent : BaseEntity
{
    [JsonIgnore]
    public BoardMaster Master { get; set; } = new BoardMaster();

    [Column(TypeName = "char(36)")]
    public string MasterId { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(100)")]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? Answer { get; set; }

    public BoardCategory? Category { get; set; }

    [Column(TypeName = "char(36)")]
    public string CategoryId { get; set; } =  string.Empty;

    public DateTime? AnswerDate { get; set; }

    [Column(TypeName = "char(36)")]
    public string? AnwerOwnerId { get; set; }

    public int ReadCount { get; set; } = 0;

    [Column(TypeName = "char(36)")]
    public string? OwnerId { get; set; }

    [NotMapped]
    public MemberDTO? Owner { get; set; }

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();

    public BoardContent()
    {
    }
}

public class BoardContentConfiguration : IEntityTypeConfiguration<BoardContent>
{
    public void Configure(EntityTypeBuilder<BoardContent> builder)
    {
        builder.HasKey(x => x.Id);
        

        builder.Property(m => m.ReadCount).HasDefaultValue(0).HasSentinel(0);

        builder.HasOne(x => x.Master)
               .WithMany(x => x.Contents)
               .HasForeignKey(x => x.MasterId);

        builder.HasOne(x => x.Category)
               .WithMany(x => x.Contents)
               .HasForeignKey(x => x.CategoryId);

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