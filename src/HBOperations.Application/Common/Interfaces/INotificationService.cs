using HBOperations.Domain.Enums;

namespace HBOperations.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, string titleAr, string messageAr,
        Guid? transactionId = null, NotificationType type = NotificationType.SystemAlert);
    Task NotifyRoleAsync(string role, string titleAr, string messageAr,
        Guid? transactionId = null, Guid? branchId = null, NotificationType type = NotificationType.SystemAlert);
}
