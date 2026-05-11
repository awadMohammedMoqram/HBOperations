using Microsoft.JSInterop;

namespace HBMail.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// JS-based toast notification service for Blazor Server.
/// Renders toasts via pure JavaScript — auto-dismiss & manual dismiss work reliably.
/// Inject as Scoped.
/// </summary>
public class ToastService
{
    private readonly IJSRuntime _js;

    public ToastService(IJSRuntime js)
    {
        _js = js;
    }

    public Task ShowSuccessAsync(string title, string? message = null, int durationMs = 4000)
        => InvokeJsAsync("success", title, message, durationMs);

    public Task ShowErrorAsync(string title, string? message = null, int durationMs = 6000)
        => InvokeJsAsync("error", title, message, durationMs);

    public Task ShowWarningAsync(string title, string? message = null, int durationMs = 5000)
        => InvokeJsAsync("warning", title, message, durationMs);

    public Task ShowInfoAsync(string title, string? message = null, int durationMs = 4000)
        => InvokeJsAsync("info", title, message, durationMs);

    private async Task InvokeJsAsync(string type, string title, string? message, int durationMs)
    {
        try
        {
            await _js.InvokeVoidAsync("HBToast.show", type, title, message, durationMs);
        }
        catch (InvalidOperationException) { /* prerendering — JS not available yet */ }
        catch (JSDisconnectedException) { /* circuit closed */ }
        catch (JSException) { /* JS error */ }
    }
}
