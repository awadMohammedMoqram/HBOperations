using HBOperations.Application.Common.DTOs;

namespace HBOperations.Application.Common.Reports;

/// <summary>
/// طبقة دفاع إضافية: تحجب الحقول الحساسة قبل تسليمها للتقرير
/// إذا لم يكن للمستخدم صلاحية رؤيتها (Defense in Depth).
/// السياسة الأساسية تمنع الوصول للنوع، لكن هذه الخدمة تضمن
/// عدم تسرّب أي حقل حساس حتى لو تم تجاوز السياسة بالخطأ.
/// </summary>
public interface IReportSanitizer
{
    /// <summary>هل يستطيع المستخدم رؤية ملاحظات الرفض؟</summary>
    bool CanSeeRejectionNotes(UserScope scope);

    /// <summary>هل يستطيع المستخدم رؤية ملاحظات الإدارة؟</summary>
    bool CanSeeAdminNotes(UserScope scope);

    /// <summary>تطبيق الحجب على صفوف تقرير المراجعة</summary>
    List<AuditTrailReportRow> Sanitize(List<AuditTrailReportRow> rows, UserScope scope);
}

public sealed class ReportSanitizer : IReportSanitizer
{
    private const string Redacted = "[محجوب]";

    public bool CanSeeRejectionNotes(UserScope scope) =>
        scope.HasGlobalAccess || scope.IsAuditor || scope.IsCompliance || scope.IsShariahAuditor;

    public bool CanSeeAdminNotes(UserScope scope) =>
        scope.HasGlobalAccess || scope.IsAuditor || scope.IsCompliance;

    public List<AuditTrailReportRow> Sanitize(List<AuditTrailReportRow> rows, UserScope scope)
    {
        var canSeeRejection = CanSeeRejectionNotes(scope);
        var canSeeAdmin = CanSeeAdminNotes(scope);

        if (canSeeRejection && canSeeAdmin)
            return rows;

        return rows.Select(r => new AuditTrailReportRow
        {
            ReferenceNumber = r.ReferenceNumber,
            Subject = r.Subject,
            Type = r.Type,
            Status = r.Status,
            Priority = r.Priority,
            SenderName = r.SenderName,
            ReceiverName = r.ReceiverName,
            SenderBranch = r.SenderBranch,
            ReceiverBranch = r.ReceiverBranch,
            CreatedAt = r.CreatedAt,
            SentAt = r.SentAt,
            ReceivedAt = r.ReceivedAt,
            RejectionNote = canSeeRejection ? r.RejectionNote : (string.IsNullOrEmpty(r.RejectionNote) ? null : Redacted),
            AdminNote = canSeeAdmin ? r.AdminNote : (string.IsNullOrEmpty(r.AdminNote) ? null : Redacted),
            SenderNote = r.SenderNote
        }).ToList();
    }
}
