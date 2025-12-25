namespace AigoraNet.Common.Configurations;

public record SlackWebhookConfiguration
{
    public string TKey { get; set; } = string.Empty;

    public string BKey { get; set; } = string.Empty;

    public string token { get; set; } = string.Empty;
}
