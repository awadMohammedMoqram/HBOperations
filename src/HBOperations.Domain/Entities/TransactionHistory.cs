using HBOperations.Domain.Common;
using HBOperations.Domain.Enums;

namespace HBOperations.Domain.Entities;

public class TransactionHistory : BaseEntity
{
    public Guid TransactionId { get; set; }
    public TransactionStatus FromStatus { get; set; }
    public TransactionStatus ToStatus { get; set; }
    public string Action { get; set; } = default!;
    public string? Notes { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime PerformedAt { get; set; }

    // Navigation
    public Transaction Transaction { get; set; } = default!;
}
