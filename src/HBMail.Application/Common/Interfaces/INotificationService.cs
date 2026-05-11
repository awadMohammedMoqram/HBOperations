using HBMail.Domain.Enums;

namespace HBMail.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, string titleAr, string messageAr,
        Guid? MailId = null, NotificationType type = NotificationType.SystemAlert);
    Task NotifyRoleAsync(string role, string titleAr, string messageAr,
        Guid? MailId = null, Guid? branchId = null, NotificationType type = NotificationType.SystemAlert);
    Task NotifyDepartmentAsync(Guid departmentId, string titleAr, string messageAr,
        Guid? MailId = null, NotificationType type = NotificationType.SystemAlert,
        Guid? excludeUserId = null);
    Task MarkTeamNotificationsReadAsync(Guid departmentId, Guid MailId, Guid actingUserId);
}
