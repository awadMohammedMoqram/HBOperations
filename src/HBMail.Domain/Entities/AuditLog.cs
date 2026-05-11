using HBMail.Domain.Common;

namespace HBMail.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
