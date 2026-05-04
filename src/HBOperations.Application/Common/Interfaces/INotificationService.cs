using HBOperations.Domain.Enums;

namespace HBOperations.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, string titleAr, string messageAr,
        Guid? transactionId = null, NotificationType type = NotificationType.SystemAlert);
    Task NotifyRoleAsync(string role, string titleAr, string messageAr,
        Guid? transactionId = null, Guid? branchId = null, NotificationType type = NotificationType.SystemAlert);
    Task NotifyDepartmentAsync(Guid departmentId, string titleAr, string messageAr,
        Guid? transactionId = null, NotificationType type = NotificationType.SystemAlert,
        Guid? excludeUserId = null);
    Task MarkTeamNotificationsReadAsync(Guid departmentId, Guid transactionId, Guid actingUserId);
}
