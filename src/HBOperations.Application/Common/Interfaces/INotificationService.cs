namespace HBOperations.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, string titleAr, string messageAr,
        Guid? transactionId = null);
    Task NotifyRoleAsync(string role, string titleAr, string messageAr,
        Guid? transactionId = null, Guid? branchId = null);
}
