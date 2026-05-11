using System.Net;
using System.Net.Mail;
using HBMail.Application.Common.Interfaces;
using HBMail.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HBMail.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration config,
        UserManager<ApplicationUser> userManager,
        ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var enabled = _config.GetValue<bool>("SmtpSettings:Enabled");
        if (!enabled)
        {
            _logger.LogInformation("Email disabled. Would send to {Email}: {Subject}", toEmail, subject);
            return;
        }

        var host = _config["SmtpSettings:Host"]!;
        var port = _config.GetValue<int>("SmtpSettings:Port");
        var useSsl = _config.GetValue<bool>("SmtpSettings:UseSsl");
        var username = _config["SmtpSettings:Username"]!;
        var password = _config["SmtpSettings:Password"]!;
        var fromEmail = _config["SmtpSettings:FromEmail"]!;
        var fromName = _config["SmtpSettings:FromName"] ?? "HBMail";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            Credentials = new NetworkCredential(username, password)
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }

    public async Task SendToUserAsync(Guid userId, string subject, string htmlBody)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is not null)
            await SendAsync(user.Email, subject, htmlBody);
    }
}
