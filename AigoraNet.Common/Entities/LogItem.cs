using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AigoraNet.Common.Entities;

public class LogItem
{
    public long Id { get; set; }

    public string Message { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(128)")]
    public string Level { get; set; } = string.Empty;

    public DateTimeOffset TimeStamp { get; set; }

    public string Exception { get; set; } = string.Empty;

    public string LogEvent { get; set; } = string.Empty;

    public string Properties { get; set; } = string.Empty;
}

public class LogItemConfiguration : IEntityTypeConfiguration<LogItem>
{
	public void Configure(EntityTypeBuilder<LogItem> builder)
	{
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id).ValueGeneratedOnAdd();
	}
}