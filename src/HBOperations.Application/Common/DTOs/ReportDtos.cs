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
