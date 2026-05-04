using HBOperations.Application.Common.Interfaces;
using HBOperations.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HBOperations.Web.Services;

public class SignalRNotifier(IHubContext<NotificationHub> hubContext, NotificationEventService eventService) : IRealTimeNotifier
{
    public async Task SendToUserAsync(Guid userId, string title, string message, Guid? transactionId = null)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                TransactionId = transactionId,
                Timestamp = DateTime.UtcNow
            });

        await eventService.RaiseAsync(new NotificationPayload(userId, title, message, transactionId));
    }

    public async Task SendToAllAsync(string title, string message, Guid? transactionId = null)
    {
        await hubContext.Clients.All
            .SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                TransactionId = transactionId,
                Timestamp = DateTime.UtcNow
            });
    }

    public async Task RefreshUserAsync(Guid userId)
    {
        await eventService.RaiseRefreshAsync(userId);
    }
}
