namespace AigoraNet.Common.Configurations;

public record SwaggerConfiguration : CurrentSiteConfiguration
{
    public string ApiServiceName { get; set; } = string.Empty;
}
