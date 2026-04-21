namespace HBOperations.Web.Services;

/// <summary>
/// In-process notification event bus for Blazor Server components.
/// Components subscribe to receive real-time notification count updates.
/// </summary>
public class NotificationEventService
{
    public event Func<Guid, Task>? OnNotification;

    public async Task RaiseAsync(Guid userId)
    {
        if (OnNotification is not null)
            await OnNotification.Invoke(userId);
    }
}
