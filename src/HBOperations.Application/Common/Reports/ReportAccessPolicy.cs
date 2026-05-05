using HBOperations.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HBOperations.Application.Common.Reports;

/// <summary>
/// تطبيق سياسة الوصول للتقارير.
/// يحدد لكل دور في النظام ما يمكنه رؤيته من تقارير، ويبني نطاقه.
/// </summary>
public sealed class ReportAccessPolicy : IReportAccessPolicy
{
    // الأدوار ذات الوصول العام لجميع المعاملات
    private static readonly HashSet<string> GlobalRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SuperAdmin", "CEO", "ITAdmin", "AssistantCEO",
        "Auditor", "ComplianceOfficer", "ShariahAuditor"
    };

    public async Task<UserScope> GetUserScopeAsync(ICurrentUserService currentUser, IAppDbContext db)
    {
        var roles = currentUser.Roles?.ToList() ?? new List<string>();

        var isAdminAffairs = false;
        if (currentUser.DepartmentId.HasValue)
        {
            isAdminAffairs = await db.Departments.AsNoTracking()
                .AnyAsync(d => d.Id == currentUser.DepartmentId.Value && d.Code == "DEP-ADM");
        }

        return new UserScope
        {
            UserId = currentUser.UserId,
            FullName = currentUser.FullName ?? currentUser.UserName ?? "—",
            Roles = roles,
            BranchId = currentUser.BranchId,
            DepartmentId = currentUser.DepartmentId,
            HasGlobalAccess = roles.Any(r => GlobalRoles.Contains(r)),
            IsAdminAffairs = isAdminAffairs,
            IsDepartmentManager = roles.Contains("DepartmentManager", StringComparer.OrdinalIgnoreCase),
            IsBranchManager = roles.Contains("BranchManager", StringComparer.OrdinalIgnoreCase),
            IsOfficeManager = roles.Contains("OfficeManager", StringComparer.OrdinalIgnoreCase),
            IsAuditor = roles.Contains("Auditor", StringComparer.OrdinalIgnoreCase),
            IsCompliance = roles.Contains("ComplianceOfficer", StringComparer.OrdinalIgnoreCase),
            IsShariahAuditor = roles.Contains("ShariahAuditor", StringComparer.OrdinalIgnoreCase),
            IsExecutive = roles.Contains("CEO", StringComparer.OrdinalIgnoreCase) ||
                          roles.Contains("AssistantCEO", StringComparer.OrdinalIgnoreCase) ||
                          roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase),
        };
    }

    public bool CanAccessReport(UserScope scope, ReportType type) => type switch
    {
        ReportType.Executive          => scope.IsExecutive || scope.IsAuditor,
        ReportType.Branches           => scope.HasGlobalAccess,
        ReportType.Departments        => scope.HasGlobalAccess,
        ReportType.AuditTrail         => scope.IsAuditor || scope.IsCompliance ||
                                         scope.Roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase),
        ReportType.RejectionAnalysis  => scope.IsCompliance || scope.IsAuditor || scope.IsExecutive,
        ReportType.ShariahCompliance  => scope.IsShariahAuditor,
        ReportType.DepartmentScoped   => scope.IsDepartmentManager && scope.DepartmentId.HasValue,
        ReportType.BranchScoped       => scope.IsBranchManager && scope.BranchId.HasValue,
        ReportType.OfficeScoped       => scope.IsOfficeManager && scope.BranchId.HasValue,
        ReportType.AdminAffairsDaily  => scope.IsAdminAffairs || scope.HasGlobalAccess,
        ReportType.Personal           => true, // كل مستخدم يمكنه تصدير تقريره الشخصي
        _ => false
    };

    public IReadOnlyList<ReportType> GetAvailableReports(UserScope scope)
    {
        var list = new List<ReportType>();
        foreach (ReportType t in Enum.GetValues<ReportType>())
        {
            if (CanAccessReport(scope, t))
                list.Add(t);
        }
        return list;
    }

    public ReportType GetDefaultReport(UserScope scope)
    {
        if (scope.IsExecutive) return ReportType.Executive;
        if (scope.IsAuditor || scope.IsCompliance) return ReportType.AuditTrail;
        if (scope.IsShariahAuditor) return ReportType.ShariahCompliance;
        if (scope.HasGlobalAccess) return ReportType.Branches;
        if (scope.IsAdminAffairs) return ReportType.AdminAffairsDaily;
        if (scope.IsDepartmentManager && scope.DepartmentId.HasValue) return ReportType.DepartmentScoped;
        if (scope.IsBranchManager && scope.BranchId.HasValue) return ReportType.BranchScoped;
        if (scope.IsOfficeManager && scope.BranchId.HasValue) return ReportType.OfficeScoped;
        return ReportType.Personal;
    }

    /// <summary>الاسم العربي لنوع التقرير (للعرض في الواجهة)</summary>
    public static string GetArabicName(ReportType type) => type switch
    {
        ReportType.Executive          => "التقرير التنفيذي الشامل",
        ReportType.Branches           => "تقرير الفروع",
        ReportType.Departments        => "تقرير الإدارات",
        ReportType.AuditTrail         => "سجل التدقيق",
        ReportType.RejectionAnalysis  => "تحليل المعاملات المرفوضة",
        ReportType.ShariahCompliance  => "تقرير الرقابة الشرعية",
        ReportType.DepartmentScoped   => "تقرير الإدارة",
        ReportType.BranchScoped       => "تقرير الفرع",
        ReportType.OfficeScoped       => "تقرير المكتب",
        ReportType.AdminAffairsDaily  => "تقرير الشؤون الإدارية",
        ReportType.Personal           => "تقريري الشخصي",
        _ => "تقرير"
    };

    /// <summary>وصف موجز للتقرير (يظهر في القائمة)</summary>
    public static string GetDescription(ReportType type) => type switch
    {
        ReportType.Executive          => "تقرير استراتيجي شامل يعرض المؤشرات الرئيسية لكل البنك",
        ReportType.Branches           => "إحصاءات تفصيلية لكل الفروع مع المقارنات",
        ReportType.Departments        => "إحصاءات تفصيلية لكل الإدارات مع الأداء",
        ReportType.AuditTrail         => "سجل تدقيق شامل لكل العمليات الحساسة",
        ReportType.RejectionAnalysis  => "تحليل أسباب الرفض والاتجاهات",
        ReportType.ShariahCompliance  => "تقرير المعاملات المالية فقط للرقابة الشرعية",
        ReportType.DepartmentScoped   => "تقرير شامل لإدارتك مع أداء الموظفين",
        ReportType.BranchScoped       => "تقرير شامل لفرعك مع أداء الموظفين",
        ReportType.OfficeScoped       => "تقرير لمكتبك",
        ReportType.AdminAffairsDaily  => "المعاملات بانتظار الاستلام والتسليم",
        ReportType.Personal           => "تقرير معاملاتك الشخصية فقط",
        _ => ""
    };
}
