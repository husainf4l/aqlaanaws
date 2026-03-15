namespace AqlaAwsS3Manager.Services;

public class LogOnlyEmailSender : IEmailSender
{
    private readonly ILogger<LogOnlyEmailSender> _logger;

    public LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger) => _logger = logger;

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email (not sent - configure SMTP to send): To={To}, Subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}
