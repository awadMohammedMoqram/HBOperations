namespace HBMail.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string entityName, Guid entityId, string action,
        object? oldValues = null, object? newValues = null);
}
