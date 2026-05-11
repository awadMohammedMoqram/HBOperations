using HBMail.Application.Common.Interfaces;
using HBMail.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HBMail.Web.Services;

public class SignalRNotifier(IHubContext<NotificationHub> hubContext, NotificationEventService eventService) : IRealTimeNotifier
{
    public async Task SendToUserAsync(Guid userId, string title, string message, Guid? MailId = null)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                MailId = MailId,
                Timestamp = DateTime.UtcNow
            });

        await eventService.RaiseAsync(new NotificationPayload(userId, title, message, MailId));
    }

    public async Task SendToAllAsync(string title, string message, Guid? MailId = null)
    {
        await hubContext.Clients.All
            .SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                MailId = MailId,
                Timestamp = DateTime.UtcNow
            });
    }

    public async Task RefreshUserAsync(Guid userId)
    {
        await eventService.RaiseRefreshAsync(userId);
    }
}
