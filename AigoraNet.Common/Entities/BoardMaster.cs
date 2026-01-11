using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common;

public enum BoardTypecs : byte
{
    Normal = 0,
    Gallery = 1,
    Question = 2,
    ReadOnly = 4,
}

public class BoardMaster : BaseEntity
{
    [Column(TypeName = "varchar(255)")]
    public string MasterCode { get; set; } = string.Empty;

    public BoardTypecs BoardType { get; set; } = BoardTypecs.Normal;

    [Column(TypeName = "nvarchar(100)")]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "char(36)")]
    public string? OwnerId { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? Section { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? Site { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    public string? Icon { get; set; }

    public int Seq { get; set; }

    [JsonIgnore]
    public ICollection<BoardContent> Contents { get; set; } = new HashSet<BoardContent>();

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();
}

public class BoardMasterConfiguration : IEntityTypeConfiguration<BoardMaster>
{
    public void Configure(EntityTypeBuilder<BoardMaster> builder)
    {
        builder.HasKey(x => x.Id);
        

        builder.HasIndex(x => new { x.MasterCode }).IsUnique();

        builder.Property(m => m.BoardType).HasDefaultValue(BoardTypecs.Normal).HasSentinel(BoardTypecs.Normal);

        builder.Property(m => m.Seq).HasDefaultValue(0);

        builder.HasMany(x => x.Contents)
               .WithOne(x => x.Master)
               .HasForeignKey(x => x.MasterId);

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

public static class ExtendBoardMaster
{
    public static BoardMaster? Get(this List<BoardMaster> masters, string masterCode = "")
    {
        if (masters != null && masters.Count > 0 && !string.IsNullOrWhiteSpace(masterCode))
        {
            return masters.Where(x => x.MasterCode.Equals(masterCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }
        else
        {
            return null;
        }
    }
}