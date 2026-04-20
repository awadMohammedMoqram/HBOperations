using System.Text.Json;
using HBOperations.Application.Common.Interfaces;
using HBOperations.Domain.Entities;
using HBOperations.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace HBOperations.Infrastructure.Services;

public class AuditService(AppDbContext context, ICurrentUserService currentUser, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task LogAsync(string entityName, Guid entityId, string action,
        object? oldValues = null, object? newValues = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is not null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues is not null ? JsonSerializer.Serialize(newValues) : null,
            UserId = currentUser.UserId,
            UserName = currentUser.FullName ?? currentUser.UserName,
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        };

        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();
    }
}
