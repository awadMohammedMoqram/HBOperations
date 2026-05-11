using HBMail.Domain.Enums;

namespace HBMail.Application.Common.DTOs;

public class MailSummaryDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public MailType Type { get; set; }
    public MailPriority Priority { get; set; }
    public MailStatus Status { get; set; }
    public string? SenderBranchName { get; set; }
    public string? ReceiverBranchName { get; set; }
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DocumentCount { get; set; }
}

public class MailDetailDto : MailSummaryDto
{
    public string? Description { get; set; }
    public string? SenderDepartmentName { get; set; }
    public string? ReceiverDepartmentName { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<AttachmentDto> Documents { get; set; } = [];
    public List<HistoryDto> History { get; set; } = [];
    public List<NoteDto> Notes { get; set; } = [];
}

public class AttachmentDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public AttachmentType AttachmentType { get; set; }
    public int Version { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class HistoryDto
{
    public MailStatus FromStatus { get; set; }
    public MailStatus ToStatus { get; set; }
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
    public int TodayMails { get; set; }
    public int PendingMails { get; set; }
    public int UrgentMails { get; set; }
    public int OverdueMails { get; set; }
    public int TotalMails { get; set; }
    public Dictionary<MailStatus, int> StatusBreakdown { get; set; } = [];
}
