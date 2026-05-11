using Microsoft.Extensions.Caching.Memory;

namespace HBMail.Application.Common.Reports;

/// <summary>
/// خدمة تحديد معدل تصدير التقارير (Rate Limiting) لكل مستخدم.
/// تستخدم MemoryCache لتتبع عدد التصديرات لكل مستخدم خلال اليوم/الساعة.
/// </summary>
public interface IReportRateLimiter
{
    /// <summary>تحقق هل يمكن للمستخدم التصدير الآن</summary>
    RateLimitResult CheckLimit(Guid userId, IReadOnlyList<string> roles);

    /// <summary>سجل عملية تصدير ناجحة</summary>
    void RecordExport(Guid userId);

    /// <summary>عدد التصديرات اليومية الحالية</summary>
    int GetDailyCount(Guid userId);
}

public sealed record RateLimitResult(bool IsAllowed, string? Reason, int DailyLimit, int CurrentDaily);

public sealed class ReportRateLimiter : IReportRateLimiter
{
    private readonly IMemoryCache _cache;

    // الحدود اليومية حسب الدور (الأعلى يفوز)
    private static readonly Dictionary<string, int> DailyLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SuperAdmin",        int.MaxValue },
        { "ITAdmin",           200 },
        { "Auditor",           50 },
        { "ComplianceOfficer", 50 },
        { "ShariahAuditor",    30 },
        { "CEO",               30 },
        { "AssistantCEO",      30 },
        { "DepartmentManager", 20 },
        { "BranchManager",     20 },
        { "OfficeManager",     10 },
        { "DepartmentStaff",   5 },
        { "BranchStaff",       5 },
    };

    private const int HourlyLimit = 20; // حد الساعة العام (إضافة على اليومي)

    public ReportRateLimiter(IMemoryCache cache) => _cache = cache;

    public RateLimitResult CheckLimit(Guid userId, IReadOnlyList<string> roles)
    {
        // أعلى حد بناءً على أدوار المستخدم
        var dailyLimit = roles
            .Where(r => DailyLimits.ContainsKey(r))
            .Select(r => DailyLimits[r])
            .DefaultIfEmpty(5)
            .Max();

        var dailyCount = GetDailyCount(userId);
        if (dailyCount >= dailyLimit)
        {
            return new RateLimitResult(false,
                $"تم الوصول للحد اليومي للتصدير ({dailyLimit} تقرير). الرجاء المحاولة غداً.",
                dailyLimit, dailyCount);
        }

        var hourlyKey = HourlyKey(userId);
        var hourlyCount = _cache.TryGetValue<int>(hourlyKey, out var hc) ? hc : 0;
        if (hourlyCount >= HourlyLimit)
        {
            return new RateLimitResult(false,
                $"تم الوصول للحد بالساعة ({HourlyLimit} تقرير). انتظر قبل المحاولة مرة أخرى.",
                dailyLimit, dailyCount);
        }

        return new RateLimitResult(true, null, dailyLimit, dailyCount);
    }

    public void RecordExport(Guid userId)
    {
        var dailyKey = DailyKey(userId);
        var dailyCount = _cache.TryGetValue<int>(dailyKey, out var dc) ? dc : 0;
        _cache.Set(dailyKey, dailyCount + 1, EndOfDay());

        var hourlyKey = HourlyKey(userId);
        var hourlyCount = _cache.TryGetValue<int>(hourlyKey, out var hc) ? hc : 0;
        _cache.Set(hourlyKey, hourlyCount + 1, EndOfHour());
    }

    public int GetDailyCount(Guid userId)
    {
        var dailyKey = DailyKey(userId);
        return _cache.TryGetValue<int>(dailyKey, out var dc) ? dc : 0;
    }

    private static string DailyKey(Guid userId) => $"report-export-daily:{userId:N}:{DateTime.UtcNow:yyyyMMdd}";
    private static string HourlyKey(Guid userId) => $"report-export-hourly:{userId:N}:{DateTime.UtcNow:yyyyMMddHH}";

    private static DateTimeOffset EndOfDay()
    {
        var now = DateTime.UtcNow;
        return new DateTimeOffset(now.Year, now.Month, now.Day, 23, 59, 59, TimeSpan.Zero);
    }

    private static DateTimeOffset EndOfHour()
    {
        var now = DateTime.UtcNow;
        return new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 59, 59, TimeSpan.Zero);
    }
}
