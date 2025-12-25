namespace AigoraNet.Common.Configurations;

public record SmtpConfiguration
{
    public string From { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseSSL { get; set; } = true;
}