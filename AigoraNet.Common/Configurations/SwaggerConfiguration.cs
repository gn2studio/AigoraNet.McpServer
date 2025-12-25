namespace AigoraNet.Common.Configurations;

public record SwaggerConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;

    public string IdentityUrl { get; set; } = string.Empty;

    public string ApiServiceName { get; set; } = string.Empty;
    public string oAuthClientID { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string TermsUrl { get; set; } = string.Empty;

    public string AuthName { get; set; } = string.Empty;

    public string AuthEmail { get; set; } = string.Empty;

    public string AuthUrl { get; set; } = string.Empty;

    public string LicenseName { get; set; } = string.Empty;

    public string LicenseUrl { get; set; } = string.Empty;
}
