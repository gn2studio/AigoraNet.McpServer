namespace AigoraNet.Common.DTO;

public class MemberInfoDTO
{
    public string Email { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;

    public string Photo { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public MemberInfoDTO()
    {
    }
}
