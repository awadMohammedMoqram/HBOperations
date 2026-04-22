using HBOperations.Domain.Enums;

namespace HBOperations.Application.Common.DTOs;

public class TransactionSummaryDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public TransactionType Type { get; set; }
    public TransactionPriority Priority { get; set; }
    public TransactionStatus Status { get; set; }
    public string? SenderBranchName { get; set; }
    public string? ReceiverBranchName { get; set; }
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DocumentCount { get; set; }
}

public class TransactionDetailDto : TransactionSummaryDto
{
    public string? Description { get; set; }
    public string? SenderDepartmentName { get; set; }
    public string? ReceiverDepartmentName { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<DocumentDto> Documents { get; set; } = [];
    public List<HistoryDto> History { get; set; } = [];
    public List<NoteDto> Notes { get; set; } = [];
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public DocumentType DocumentType { get; set; }
    public int Version { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class HistoryDto
{
    public TransactionStatus FromStatus { get; set; }
    public TransactionStatus ToStatus { get; set; }
    public string Action { get; set; } = default!;
    public string? Notes { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime PerformedAt { get; set; }
}

public class NoteDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = default!;
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BranchDto
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string Code { get; set; } = default!;
    public BranchType BranchType { get; set; }
    public bool IsActive { get; set; }
}

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class DashboardStatsDto
{
    public int TodayTransactions { get; set; }
    public int PendingTransactions { get; set; }
    public int UrgentTransactions { get; set; }
    public int OverdueTransactions { get; set; }
    public int TotalTransactions { get; set; }
    public Dictionary<TransactionStatus, int> StatusBreakdown { get; set; } = [];
}
