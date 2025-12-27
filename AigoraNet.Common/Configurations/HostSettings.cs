namespace AigoraNet.Common.Configurations;

public record HostSettings
{
    public string AllowedHosts { get; set; } = string.Empty;
}