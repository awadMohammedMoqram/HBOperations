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

    public async Task NotifyDepartmentAsync(Guid departmentId, string titleAr, string messageAr,
        Guid? transactionId = null, NotificationType type = NotificationType.SystemAlert,
        Guid? excludeUserId = null)
    {
        var usersInDept = await userManager.Users
            .Where(u => u.DepartmentId == departmentId && u.IsActive)
            .ToListAsync();

        // استبعاد المرسل إذا كان من نفس الإدارة
        if (excludeUserId.HasValue)
            usersInDept = usersInDept.Where(u => u.Id != excludeUserId.Value).ToList();

        var notifications = usersInDept.Select(u => new Notification
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

        foreach (var notif in notifications)
        {
            await realTimeNotifier.SendToUserAsync(notif.UserId, titleAr, messageAr, transactionId);
        }
    }

    public async Task MarkTeamNotificationsReadAsync(Guid departmentId, Guid transactionId, Guid actingUserId)
    {
        // عند قراءة أي عضو في الفريق → يتحول لمقروء عند الجميع
        var teamUserIds = await userManager.Users
            .Where(u => u.DepartmentId == departmentId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        var unread = await context.Notifications
            .Where(n => teamUserIds.Contains(n.UserId)
                        && n.TransactionId == transactionId
                        && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }
        await context.SaveChangesAsync();

        // إرسال تحديث real-time لكل أعضاء الفريق (ليتم تحديث عداد الإشعارات)
        foreach (var uid in teamUserIds.Where(id => id != actingUserId))
        {
            await realTimeNotifier.RefreshUserAsync(uid);
        }
    }
}
