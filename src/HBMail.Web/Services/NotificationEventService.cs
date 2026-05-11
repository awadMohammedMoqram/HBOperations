namespace HBMail.Web.Services;

public record NotificationPayload(Guid UserId, string Title, string Message, Guid? MailId);

/// <summary>
/// In-process notification event bus for Blazor Server components.
/// Components subscribe to receive real-time notification updates.
/// </summary>
public class NotificationEventService
{
    public event Func<NotificationPayload, Task>? OnNotification;
    public event Func<Guid, Task>? OnRefresh;

    public async Task RaiseAsync(NotificationPayload payload)
    {
        if (OnNotification is not null)
            await OnNotification.Invoke(payload);
    }

    public async Task RaiseRefreshAsync(Guid userId)
    {
        if (OnRefresh is not null)
            await OnRefresh.Invoke(userId);
    }
}
