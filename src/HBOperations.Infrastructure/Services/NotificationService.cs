using HBOperations.Application.Common.Interfaces;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;
using HBOperations.Infrastructure.Data;
using HBOperations.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HBOperations.Infrastructure.Services;

public class NotificationService(AppDbContext context, UserManager<ApplicationUser> userManager) : INotificationService
{
    public async Task NotifyUserAsync(Guid userId, string titleAr, string messageAr, Guid? transactionId = null)
    {
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TitleAr = titleAr,
            MessageAr = messageAr,
            Type = NotificationType.SystemAlert,
            TransactionId = transactionId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    public async Task NotifyRoleAsync(string role, string titleAr, string messageAr,
        Guid? transactionId = null, Guid? branchId = null)
    {
        var usersInRole = await userManager.GetUsersInRoleAsync(role);
        var targetUsers = branchId.HasValue
            ? usersInRole.Where(u => u.BranchId == branchId.Value)
            : usersInRole;

        var notifications = targetUsers.Select(u => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = u.Id,
            TitleAr = titleAr,
            MessageAr = messageAr,
            Type = NotificationType.SystemAlert,
            TransactionId = transactionId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();
    }
}
