namespace AigoraNet.Common.Abstracts;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}