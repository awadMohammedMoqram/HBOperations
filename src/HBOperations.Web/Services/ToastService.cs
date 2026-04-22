namespace HBOperations.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public record ToastMessage(Guid Id, ToastType Type, string Title, string? Message, int DurationMs);

/// <summary>
/// In-process toast notification service for Blazor Server.
/// Inject as Scoped — each circuit gets its own instance.
/// </summary>
public class ToastService
{
    public event Func<ToastMessage, Task>? OnShow;

    public Task ShowSuccessAsync(string title, string? message = null, int durationMs = 4000)
        => RaiseAsync(ToastType.Success, title, message, durationMs);

    public Task ShowErrorAsync(string title, string? message = null, int durationMs = 6000)
        => RaiseAsync(ToastType.Error, title, message, durationMs);

    public Task ShowWarningAsync(string title, string? message = null, int durationMs = 5000)
        => RaiseAsync(ToastType.Warning, title, message, durationMs);

    public Task ShowInfoAsync(string title, string? message = null, int durationMs = 4000)
        => RaiseAsync(ToastType.Info, title, message, durationMs);

    private async Task RaiseAsync(ToastType type, string title, string? message, int durationMs)
    {
        var toast = new ToastMessage(Guid.NewGuid(), type, title, message, durationMs);
        if (OnShow is not null)
            await OnShow.Invoke(toast);
    }
}
