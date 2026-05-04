namespace HBOperations.Domain.Common;

/// <summary>
/// مرجع موحد لكل عمليات الوقت في التطبيق.
/// قاعدة ذهبية:
///   - DB يخزّن UTC دائماً
///   - إدخال المستخدم (datetime-local) = توقيت اليمن
///   - عرض التواريخ = توقيت اليمن
/// </summary>
public static class AppTime
{
    /// <summary>
    /// المنطقة الزمنية لليمن (UTC+3، بدون توقيت صيفي).
    /// تستخدم Asia/Aden على Linux و Arab Standard Time على Windows.
    /// </summary>
    public static readonly TimeZoneInfo YemenTimeZone = ResolveYemenTimeZone();

    /// <summary>الوقت الحالي UTC (للتخزين في DB).</summary>
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>الوقت الحالي بتوقيت اليمن (للعرض / القيم الافتراضية في النماذج).</summary>
    public static DateTime YemenNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, YemenTimeZone);

    /// <summary>
    /// تحويل وقت UTC المخزّن في DB إلى توقيت اليمن للعرض.
    /// </summary>
    public static DateTime ToYemen(DateTime utc)
    {
        var asUtc = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(asUtc, DateTimeKind.Utc), YemenTimeZone);
    }

    /// <summary>تحويل nullable.</summary>
    public static DateTime? ToYemen(DateTime? utc) => utc.HasValue ? ToYemen(utc.Value) : null;

    /// <summary>
    /// تحويل وقت أدخله المستخدم (datetime-local، يعتبر توقيت يمن) إلى UTC للتخزين.
    /// </summary>
    public static DateTime FromYemenToUtc(DateTime yemenLocal)
    {
        // نُعلِّمه Unspecified لأن TimeZoneInfo.ConvertTimeToUtc يرفض Kind=Utc
        var unspecified = DateTime.SpecifyKind(yemenLocal, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, YemenTimeZone);
    }

    /// <summary>تنسيق UTC → "yyyy/MM/dd HH:mm" بتوقيت اليمن.</summary>
    public static string FormatYemen(DateTime utc, string format = "yyyy/MM/dd HH:mm")
        => ToYemen(utc).ToString(format);

    public static string FormatYemen(DateTime? utc, string format = "yyyy/MM/dd HH:mm")
        => utc.HasValue ? FormatYemen(utc.Value, format) : "—";

    private static TimeZoneInfo ResolveYemenTimeZone()
    {
        // .NET 8+ يدعم IANA على ويندوز، لكن نُجرّب الاثنين احتياطاً
        string[] ids = { "Asia/Aden", "Arab Standard Time", "Arabia Standard Time" };
        foreach (var id in ids)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* جرّب التالي */ }
        }
        // fallback: UTC+3 ثابت
        return TimeZoneInfo.CreateCustomTimeZone("Yemen", TimeSpan.FromHours(3), "Yemen Time", "Yemen Time");
    }
}
