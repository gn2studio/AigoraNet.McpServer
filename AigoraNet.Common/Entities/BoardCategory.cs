using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AigoraNet.Common;

public class BoardCategory : BaseEntity
{
    [Column(TypeName = "char(36)")]
    public string BoardMasterId { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(50)")]
    public string Title { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<BoardContent> Contents { get; set; } = new HashSet<BoardContent>();
}

public class BoardCategoryConfiguration : IEntityTypeConfiguration<BoardCategory>
{
    public void Configure(EntityTypeBuilder<BoardCategory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.Contents)
               .WithOne(x => x.Category)
               .HasForeignKey(x => x.CategoryId);
    }
}