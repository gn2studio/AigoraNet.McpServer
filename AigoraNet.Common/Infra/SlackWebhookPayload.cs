using AigoraNet.Common.Configurations;
using System.Text.Json.Serialization;

namespace AigoraNet.Common.Infra;

public class SlackWebhookPayload
{
    public string Text { get; set; } = string.Empty;

    [JsonInclude]
    public List<SlackBlock>? Blocks { get; set; }

    public static string CreateUrl(string TKey, string BKey, string token)
    {
        return $"https://hooks.slack.com/services/{TKey}/${BKey}/{token}";
    }

    public static string CreateUrl(SlackWebhookConfiguration config)
    {
        return $"https://hooks.slack.com/services/{config.TKey}/${config.BKey}/{config.token}";
    }
}

public class SlackBlock
{
    public string Type { get; set; } = string.Empty;
    public SlackTextElement Text { get; set; } = new SlackTextElement();
    public string BlockId { get; set; } = string.Empty;
    public SlackAccessory Accessory { get; set; } = new SlackAccessory();
    public List<SlackField> Fields { get; set; } = new List<SlackField>();
}

public class SlackTextElement
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class SlackAccessory
{
    public string Type { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
}

public class SlackField
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}