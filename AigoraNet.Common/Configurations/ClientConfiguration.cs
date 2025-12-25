namespace AigoraNet.Common.Configurations;

public record ClientConfiguration : CurrentSiteConfiguration
{
    public string AuthenticationID { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public bool IsDebug { get; set; } = false;

    public string SiteName { get; set; } = string.Empty;

    public string NoImageUrl { get; set; } = string.Empty;

    public string ImageSaveType { get; set; } = "FileSystem";

    public int MaxSize { get; set; } = 10 * 1024 * 1024; // 10 MB
}

public record ClientImageSaveType
{
    public const string FileSystem = "FileSystem";
    public const string Database = "Database";
    public const string Azure = "Azure";
}