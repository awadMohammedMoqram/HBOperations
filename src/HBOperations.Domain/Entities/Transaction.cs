using HBOperations.Domain.Common;
using HBOperations.Domain.Enums;

namespace HBOperations.Domain.Entities;

public class Transaction : BaseEntity, IHasTimestamps
{
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string? Description { get; set; }

    public TransactionType Type { get; set; }
    public TransactionPriority Priority { get; set; }
    public TransactionStatus Status { get; set; }

    // Sender
    public Guid SenderUserId { get; set; }
    public Guid? SenderBranchId { get; set; }
    public Guid? SenderDepartmentId { get; set; }

    // Receiver
    public Guid ReceiverUserId { get; set; }
    public Guid? ReceiverBranchId { get; set; }
    public Guid? ReceiverDepartmentId { get; set; }

    // Dates
    public DateTime? DueDate { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    // Dual Approval (for Critical priority)
    public Guid? FirstApprovedByUserId { get; set; }
    public DateTime? FirstApprovedAt { get; set; }
    public Guid? SecondApprovedByUserId { get; set; }
    public DateTime? SecondApprovedAt { get; set; }

    // Navigation
    public Branch? SenderBranch { get; set; }
    public Branch? ReceiverBranch { get; set; }
    public Department? SenderDepartment { get; set; }
    public Department? ReceiverDepartment { get; set; }

    public ICollection<TransactionDocument> Documents { get; set; } = [];
    public ICollection<TransactionHistory> History { get; set; } = [];
    public ICollection<TransactionNote> Notes { get; set; } = [];
}
