namespace AigoraNet.Common.Configurations;

public abstract record CurrentSiteConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
}
