using Resend;

namespace DwellScript.Web.Services;

public interface IResendEmailService
{
    Task SendAsync(string to, string subject, string html);
}

public class ResendEmailService : IResendEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IResend resend, IConfiguration config, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string html)
    {
        var from = _config["Resend:FromEmail"] ?? "noreply@dwellscript.com";
        try
        {
            var message = new EmailMessage
            {
                From    = from,
                To      = { to },
                Subject = subject,
                HtmlBody = html
            };
            await _resend.EmailSendAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }
}
