using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;
using HBOperations.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HBOperations.Infrastructure.Data.Seed;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            await SeedRolesAsync(roleManager);
            await SeedBranchesAsync(context);
            await SeedDepartmentsAsync(context);
            await SeedSystemSettingsAsync(context);
            logger.LogInformation("Seed data completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new (string Name, string DescAr)[]
        {
            ("SuperAdmin", "مدير النظام الأعلى"),
            ("CEO", "الرئيس التنفيذي"),
            ("AssistantCEO", "مساعد الرئيس التنفيذي"),
            ("DepartmentManager", "مدير إدارة"),
            ("BranchManager", "مدير فرع"),
            ("OfficeManager", "مدير مكتب"),
            ("DepartmentStaff", "موظف إدارة"),
            ("BranchStaff", "موظف فرع"),
            ("Auditor", "مدقق داخلي"),
            ("ComplianceOfficer", "مسؤول الامتثال"),
            ("ShariahAuditor", "مراقب شرعي"),
            ("ITAdmin", "مدير تقنية المعلومات"),
        };

        foreach (var (name, descAr) in roles)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new ApplicationRole(name) { DescriptionAr = descAr });
            }
        }
    }

    private static async Task SeedBranchesAsync(AppDbContext context)
    {
        if (await context.Branches.AnyAsync()) return;

        var hqId = Guid.NewGuid();
        var branches = new List<Branch>
        {
            new() { Id = hqId, NameAr = "الإدارة العامة", Code = "HQ", BranchType = BranchType.HeadOffice },
            new() { Id = Guid.NewGuid(), NameAr = "الفرع الرئيسي", Code = "BR-MAIN", BranchType = BranchType.MainBranch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع فوه", Code = "BR-FUW", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع تريم", Code = "BR-TRM", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع عتق", Code = "BR-ATQ", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع عدن", Code = "BR-ADN", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع ال90 - عدن", Code = "BR-AD90", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع سيئون", Code = "BR-SYN", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع بويش", Code = "BR-BWS", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع الشحر", Code = "BR-SHR", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "فرع الغيضة", Code = "BR-GYD", BranchType = BranchType.Branch, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "مكتب المطار", Code = "OF-APT", BranchType = BranchType.Office, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "مكتب بن عزون", Code = "OF-BNZ", BranchType = BranchType.Office, ParentBranchId = hqId },
            new() { Id = Guid.NewGuid(), NameAr = "مكتب عدن مول", Code = "OF-ADM", BranchType = BranchType.Office, ParentBranchId = hqId },
        };

        context.Branches.AddRange(branches);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDepartmentsAsync(AppDbContext context)
    {
        if (await context.Departments.AnyAsync()) return;

        var departments = new List<Department>
        {
            new() { Id = Guid.NewGuid(), NameAr = "إدارة العمليات المركزية", Code = "DEP-OPS" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة التدقيق الداخلي", Code = "DEP-AUD" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الامتثال", Code = "DEP-CMP" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الخزينة", Code = "DEP-TRS" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الرقابة على الائتمان", Code = "DEP-CRD" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الرقابة الشرعية", Code = "DEP-SHR" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الدفع الالكتروني", Code = "DEP-EPY" },
            new() { Id = Guid.NewGuid(), NameAr = "الإدارة المالية", Code = "DEP-FIN" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة التمويل", Code = "DEP-FND" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الموارد البشرية", Code = "DEP-HRM" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة المخاطر", Code = "DEP-RSK" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الرقابة على العمليات", Code = "DEP-OPC" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة التنظيم والجودة", Code = "DEP-QAL" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة الشئون الإدارية", Code = "DEP-ADM" },
            new() { Id = Guid.NewGuid(), NameAr = "إدارة تقنية المعلومات", Code = "DEP-IT" },
        };

        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSystemSettingsAsync(AppDbContext context)
    {
        if (await context.SystemSettings.AnyAsync()) return;

        var settings = new SystemSetting[]
        {
            new() { Id = Guid.NewGuid(), Key = "AutoArchive.Enabled", Value = "true", DescriptionAr = "تفعيل الأرشفة التلقائية", Category = "Archive", ValueType = "bool" },
            new() { Id = Guid.NewGuid(), Key = "AutoArchive.DaysAfterCompletion", Value = "90", DescriptionAr = "عدد الأيام بعد الإكمال للأرشفة التلقائية", Category = "Archive", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "AutoArchive.RunTimeUtc", Value = "02:00", DescriptionAr = "وقت تشغيل الأرشفة التلقائية (UTC)", Category = "Archive", ValueType = "string" },
            new() { Id = Guid.NewGuid(), Key = "Transaction.MaxDocuments", Value = "10", DescriptionAr = "الحد الأقصى لعدد المستندات لكل معاملة", Category = "Transaction", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "Transaction.MaxFileSizeMB", Value = "50", DescriptionAr = "الحد الأقصى لحجم الملف (ميجابايت)", Category = "Transaction", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "Transaction.DefaultPriority", Value = "0", DescriptionAr = "الأولوية الافتراضية للمعاملات الجديدة (0=عادية)", Category = "Transaction", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "Notification.OverdueDays", Value = "3", DescriptionAr = "عدد أيام التأخير لتنبيه المتأخرات", Category = "Notification", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "Notification.RetentionDays", Value = "180", DescriptionAr = "عدد أيام الاحتفاظ بالإشعارات", Category = "Notification", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "System.BankNameAr", Value = "بنك حضرموت", DescriptionAr = "اسم البنك بالعربية", Category = "General", ValueType = "string", IsEditable = false },
            new() { Id = Guid.NewGuid(), Key = "System.SessionTimeoutMinutes", Value = "480", DescriptionAr = "مدة الجلسة بالدقائق", Category = "Security", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "System.MaxLoginAttempts", Value = "5", DescriptionAr = "الحد الأقصى لمحاولات تسجيل الدخول الفاشلة", Category = "Security", ValueType = "int", IsEditable = false },
            new() { Id = Guid.NewGuid(), Key = "AuditLog.RetentionDays", Value = "365", DescriptionAr = "عدد أيام الاحتفاظ بسجل التدقيق", Category = "Audit", ValueType = "int" },
        };

        context.SystemSettings.AddRange(settings);
        await context.SaveChangesAsync();
    }
}
