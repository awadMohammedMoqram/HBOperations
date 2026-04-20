using HBOperations.Domain.Common;
using HBOperations.Domain.Enums;

namespace HBOperations.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string TitleAr { get; set; } = default!;
    public string MessageAr { get; set; } = default!;
    public Guid? TransactionId { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Transaction? Transaction { get; set; }
}
