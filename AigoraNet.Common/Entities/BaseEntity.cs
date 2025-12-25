using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AigoraNet.Common;

public abstract class BaseEntity
{
    [Required]
    [Column(TypeName = "char(36)")]
    public string Id { get; set; } = string.Empty;
}