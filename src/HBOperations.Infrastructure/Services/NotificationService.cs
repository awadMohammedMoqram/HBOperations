using HBOperations.Application.Common.Interfaces;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;
using HBOperations.Infrastructure.Data;
using HBOperations.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HBOperations.Infrastructure.Services;

public class NotificationService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IRealTimeNotifier realTimeNotifier) : INotificationService
{
    public async Task NotifyUserAsync(Guid userId, string titleAr, string messageAr,
        Guid? transactionId = null, NotificationType type = NotificationType.SystemAlert)
    {
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TitleAr = titleAr,
            MessageAr = messageAr,
            Type = type,
            TransactionId = transactionId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Push real-time
        await realTimeNotifier.SendToUserAsync(userId, titleAr, messageAr, transactionId);
    }

    public async Task NotifyRoleAsync(string role, string titleAr, string messageAr,
        Guid? transactionId = null, Guid? branchId = null, NotificationType type = NotificationType.SystemAlert)
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
            Type = type,
            TransactionId = transactionId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        // Push real-time to each user
        foreach (var notif in notifications)
        {
            await realTimeNotifier.SendToUserAsync(notif.UserId, titleAr, messageAr, transactionId);
        }
    }
}
