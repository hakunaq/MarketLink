using Microsoft.AspNetCore.Identity.UI.Services;

namespace MarketLink.Services;

/// <summary>
/// Minimal email contract. In a real deployment you would plug in SendGrid,
/// SMTP, etc. For this project emails are written to the application log so the
/// feature works end to end without external credentials.
/// </summary>
public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string message)
    {
        _logger.LogInformation(
            "----- EMAIL -----\nTo: {Email}\nSubject: {Subject}\n{Message}\n-----------------",
            email, subject, message);
        return Task.CompletedTask;
    }
}
