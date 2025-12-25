using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common.Entities;

public class FileMaster : BaseEntity
{
    [Column(TypeName = "nvarchar(255)")]
    public string FileName { get; set; } = string.Empty;

    public long FileLength { get; set; } = 0;

    [Column(TypeName = "nvarchar(50)")]
    public string ContentType { get; set; } = string.Empty;

    public DateTime RegistDate { get; set; }

    public byte[]? FileBlob { get; set; }

    [Column(TypeName = "nvarchar(512)")]
    public string? Properties { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(512)")]
    public string? PublicURL { get; set; } = string.Empty;

    [JsonIgnore]
    public AuditableEntity Condition { get; set; } = new AuditableEntity();
}

public class FileMasterConfiguration : IEntityTypeConfiguration<FileMaster>
{
	public void Configure(EntityTypeBuilder<FileMaster> builder)
	{
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id).ValueGeneratedOnAdd();

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