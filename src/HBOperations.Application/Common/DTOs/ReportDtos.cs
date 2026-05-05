using HBOperations.Domain.Enums;

namespace HBOperations.Application.Common.DTOs;

public class BranchReportRow
{
    public string BranchName { get; set; } = default!;
    public int Outgoing { get; set; }
    public int Incoming { get; set; }
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int Rejected { get; set; }
}

public class DepartmentReportRow
{
    public string DepartmentName { get; set; } = default!;
    public int Outgoing { get; set; }
    public int Incoming { get; set; }
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int Rejected { get; set; }
}

public class PerformanceReportRow
{
    public string Name { get; set; } = default!;
    public int TotalHandled { get; set; }
    public int Completed { get; set; }
    public int Rejected { get; set; }
    public double AvgHours { get; set; }
    public double CompletionRate { get; set; }
}

public class AdminAffairsReportRow
{
    public string HandlerName { get; set; } = default!;
    public int PickedUp { get; set; }
    public int Delivered { get; set; }
    public int Pending { get; set; }
    public double AvgDeliveryHours { get; set; }
}

public class ReportSummary
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public double CompletionRate => Total > 0 ? (double)Completed / Total * 100 : 0;
    public double AvgProcessingHours { get; set; }
}

public class ReportFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TransactionType? Type { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
}

/// <summary>
/// صف تقرير المراجعة (Audit Trail) — للمدققين والمراجعين فقط
/// يحتوي على الحقول الحساسة: ملاحظة الرفض، ملاحظة الإدارة
/// </summary>
public class AuditTrailReportRow
{
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public TransactionPriority Priority { get; set; }
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }
    public string? SenderBranch { get; set; }
    public string? ReceiverBranch { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? RejectionNote { get; set; }
    public string? AdminNote { get; set; }
    public string? SenderNote { get; set; }
}

/// <summary>
/// صف التقرير الشخصي — يعرض معاملات المستخدم الخاصة فقط
/// </summary>
public class PersonalReportRow
{
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public string? CounterpartName { get; set; }   // الطرف الآخر (مرسل أو مستقبل)
    public string Direction { get; set; } = default!; // "صادرة" / "واردة"
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// صف تحليل أسباب الرفض — لتحديد الأنماط المتكررة
/// </summary>
public class RejectionAnalysisRow
{
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public TransactionType Type { get; set; }
    public string? SenderBranch { get; set; }
    public string? ReceiverBranch { get; set; }
    public DateTime RejectedAt { get; set; }
    public string? RejectionNote { get; set; }
    public string RejectedBy { get; set; } = default!; // "الشؤون" / "المستلم"
}

/// <summary>
/// تجميع تحليل الرفض بحسب النوع/الفرع/المصدر
/// </summary>
public class RejectionGroup
{
    public string Label { get; set; } = default!;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// التقرير التنفيذي الشامل — للإدارة العليا
/// </summary>
public class ExecutiveReportData
{
    public ReportSummary Summary { get; set; } = new();
    public List<BranchReportRow> Branches { get; set; } = [];
    public List<DepartmentReportRow> Departments { get; set; } = [];
    public Dictionary<TransactionType, int> TypeBreakdown { get; set; } = [];
    public Dictionary<string, int> WeeklyActivity { get; set; } = [];
    public int TotalRejected { get; set; }
    public int TotalArchived { get; set; }
    public string PeriodLabel { get; set; } = default!;
}


