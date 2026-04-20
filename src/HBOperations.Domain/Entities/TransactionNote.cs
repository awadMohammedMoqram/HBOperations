using HBOperations.Domain.Common;

namespace HBOperations.Domain.Entities;

public class TransactionNote : BaseEntity
{
    public Guid TransactionId { get; set; }
    public string Content { get; set; } = default!;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Transaction Transaction { get; set; } = default!;
}
