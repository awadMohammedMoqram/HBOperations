using HBOperations.Application.Common.Interfaces;

namespace HBOperations.Application.Common.Reports;

/// <summary>
/// أنواع التقارير المتاحة في النظام.
/// كل نوع له صلاحيات خاصة وتصميم مخصص حسب دور المستخدم.
/// </summary>
public enum ReportType
{
    /// <summary>تقرير تنفيذي شامل — للأدوار العليا</summary>
    Executive = 0,

    /// <summary>تقرير الفروع — للأدوار العامة</summary>
    Branches = 1,

    /// <summary>تقرير الإدارات — للأدوار العامة</summary>
    Departments = 2,

    /// <summary>سجل التدقيق — للمدققين والامتثال</summary>
    AuditTrail = 3,

    /// <summary>تقرير المعاملات المرفوضة — للامتثال</summary>
    RejectionAnalysis = 4,

    /// <summary>تقرير الرقابة الشرعية — للمراقب الشرعي (Cash Transfer فقط)</summary>
    ShariahCompliance = 5,

    /// <summary>تقرير إدارة محددة — لمدير الإدارة</summary>
    DepartmentScoped = 6,

    /// <summary>تقرير فرع محدد — لمدير الفرع</summary>
    BranchScoped = 7,

    /// <summary>تقرير مكتب محدد — لمدير المكتب</summary>
    OfficeScoped = 8,

    /// <summary>تقرير الشؤون الإدارية اليومي</summary>
    AdminAffairsDaily = 9,

    /// <summary>تقرير شخصي — للموظف العادي</summary>
    Personal = 10,
}

public enum ReportFormat
{
    Pdf = 0,
    Excel = 1,
}

/// <summary>
/// نطاق المستخدم: يحدد ما يمكنه رؤيته
/// </summary>
public sealed class UserScope
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? DepartmentId { get; init; }

    /// <summary>صلاحيات عامة على كل المعاملات في النظام</summary>
    public bool HasGlobalAccess { get; init; }

    /// <summary>عضو في إدارة الشؤون الإدارية (DEP-ADM)</summary>
    public bool IsAdminAffairs { get; init; }

    /// <summary>مدير إدارة</summary>
    public bool IsDepartmentManager { get; init; }

    /// <summary>مدير فرع</summary>
    public bool IsBranchManager { get; init; }

    /// <summary>مدير مكتب</summary>
    public bool IsOfficeManager { get; init; }

    /// <summary>مدقق داخلي</summary>
    public bool IsAuditor { get; init; }

    /// <summary>مسؤول امتثال</summary>
    public bool IsCompliance { get; init; }

    /// <summary>مراقب شرعي</summary>
    public bool IsShariahAuditor { get; init; }

    /// <summary>قائد عام (CEO/AssistantCEO/SuperAdmin)</summary>
    public bool IsExecutive { get; init; }
}

/// <summary>
/// واجهة سياسة الوصول للتقارير.
/// تحدد ما إذا كان المستخدم يمكنه الوصول لنوع تقرير معين، وتُرجع نطاقه.
/// </summary>
public interface IReportAccessPolicy
{
    /// <summary>يبني نطاق المستخدم الحالي</summary>
    Task<UserScope> GetUserScopeAsync(ICurrentUserService currentUser, IAppDbContext db);

    /// <summary>هل يمكن للمستخدم الوصول لهذا النوع من التقارير؟</summary>
    bool CanAccessReport(UserScope scope, ReportType type);

    /// <summary>قائمة التقارير المتاحة لهذا المستخدم</summary>
    IReadOnlyList<ReportType> GetAvailableReports(UserScope scope);

    /// <summary>التقرير الافتراضي لهذا المستخدم (الذي يظهر أولاً)</summary>
    ReportType GetDefaultReport(UserScope scope);
}
