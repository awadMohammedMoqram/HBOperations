namespace HBMail.Application.Common.Interfaces;

/// <summary>
/// Pushes real-time notifications to connected clients.
/// </summary>
public interface IRealTimeNotifier
{
    Task SendToUserAsync(Guid userId, string title, string message, Guid? MailId = null);
    Task SendToAllAsync(string title, string message, Guid? MailId = null);
    Task RefreshUserAsync(Guid userId);
}
