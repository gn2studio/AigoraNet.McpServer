namespace AigoraNet.Common.DTO;

public class MemberInfoDTO
{
    public string Id { get; set; } = string.Empty;

    public string? NickName { get; set; } = string.Empty;

    public string? Photo { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public MemberInfoDTO()
    {
    }
}
