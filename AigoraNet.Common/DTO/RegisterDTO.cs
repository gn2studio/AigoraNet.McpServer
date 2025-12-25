using System.ComponentModel.DataAnnotations;

namespace AigoraNet.Common.DTO;

public class RegisterDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(2, ErrorMessage = "NickName must be at least 2 characters long.")]
    public string NickName { get; set; } = string.Empty;

    public string? Thumbnail { get; set; }

    public string? Bio { get; set; }

    public string? Photo { get; set; }
}