namespace HBOperations.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
    Task SendToUserAsync(Guid userId, string subject, string htmlBody);
}
