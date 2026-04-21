using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HBOperations.Web.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        var branchId = Context.User?.FindFirst("BranchId")?.Value;
        if (branchId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"branch_{branchId}");

        var departmentId = Context.User?.FindFirst("DepartmentId")?.Value;
        if (departmentId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dept_{departmentId}");

        await base.OnConnectedAsync();
    }
}
