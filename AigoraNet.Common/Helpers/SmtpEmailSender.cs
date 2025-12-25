using AigoraNet.Common.Abstracts;
using AigoraNet.Common.Configurations;
using System.Net;
using System.Net.Mail;

namespace AigoraNet.Common.Helpers;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpConfiguration _configuration;
    private readonly Serilog.ILogger _logger;
    private readonly SmtpClient _client;


    public SmtpEmailSender(
        SmtpConfiguration configuration,
        Serilog.ILogger logger
    )
    {
        _configuration = configuration;
        _logger = logger;
        _client = new SmtpClient
        {
            Host = _configuration.Host,
            Port = _configuration.Port,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _configuration.UseSSL
        };
        if (!string.IsNullOrEmpty(_configuration.Password))
        {
            _client.Credentials = new NetworkCredential(_configuration.Login, _configuration.Password);
        }
        else
        {
            _client.UseDefaultCredentials = true;
        }
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.Information($"Sending email: {email}, subject: {subject}, message: {htmlMessage}");
        try
        {
            MailMessage mailMessage = new MailMessage(string.IsNullOrEmpty(_configuration.From) ? _configuration.Login : _configuration.From, email);
            mailMessage.IsBodyHtml = true;
            mailMessage.Subject = subject;
            mailMessage.Body = htmlMessage;
            _client.Send(mailMessage);
            _logger.Information($"Email: {email}, subject: {subject}, message: {htmlMessage} successfully sent");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception {ex.Message} during sending email: {email}, subject: {subject}");
            throw;
        }
    }
}