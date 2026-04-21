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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            await SeedRolesAsync(roleManager);
            var branches = await SeedBranchesAsync(context);
            var departments = await SeedDepartmentsAsync(context);
            await SeedUsersAsync(userManager, branches, departments);
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

    private static async Task<Dictionary<string, Guid>> SeedBranchesAsync(AppDbContext context)
    {
        if (await context.Branches.AnyAsync())
            return await context.Branches.ToDictionaryAsync(b => b.Code, b => b.Id);

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
        return branches.ToDictionary(b => b.Code, b => b.Id);
    }

    private static async Task<Dictionary<string, Guid>> SeedDepartmentsAsync(AppDbContext context)
    {
        if (await context.Departments.AnyAsync())
            return await context.Departments.ToDictionaryAsync(d => d.Code, d => d.Id);

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
        return departments.ToDictionary(d => d.Code, d => d.Id);
    }

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid> branches,
        Dictionary<string, Guid> departments)
    {
        // Seed the system admin first
        if (await userManager.FindByEmailAsync("admin@hadhramoutbank.com") is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@hadhramoutbank.com",
                Email = "admin@hadhramoutbank.com",
                FullNameAr = "مدير النظام",
                JobTitle = "مدير تقنية المعلومات",
                EmailConfirmed = true,
                BranchId = branches.GetValueOrDefault("HQ"),
                DepartmentId = departments.GetValueOrDefault("DEP-IT"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(admin, "Admin@123456");
            await userManager.AddToRolesAsync(admin, ["SuperAdmin", "ITAdmin"]);
        }

        // Seed all bank staff
        var users = new (string Email, string FullName, string JobTitle, string Role, string? BranchCode, string? DeptCode)[]
        {
            ("abdulnasser.alhaj@hb.com", "عبدالناصر نعمان محمد الحاج", "الرئيس التنفيذي", "CEO", "HQ", null),
            ("yousuf.almuzeqir@hb.com", "يوسف يحيى حسين المزيقر", "مساعد الرئيس التنفيذي للخزينة والاستثمار", "AssistantCEO", "HQ", "DEP-TRS"),
            ("alnuman.bakir@hb.com", "النعمان عبدالله عبدالرحمن بكير", "مساعد الرئيس التنفيذي للعمليات وتقنية المعلومات", "AssistantCEO", "HQ", "DEP-OPS"),
            ("bassam.ahmed@hb.com", "بسام عبدالله مرعي احمد", "مدير إدارة العمليات المركزية", "DepartmentManager", "HQ", "DEP-OPS"),
            ("jamal.alwahbi@hb.com", "جمال امين عبدالله محمد الوهبي", "مدير إدارة التدقيق الداخلي", "Auditor", "HQ", "DEP-AUD"),
            ("amin.alsaghir@hb.com", "أمين محمد أمين الصغير", "مدير إدارة الامتثال", "ComplianceOfficer", "HQ", "DEP-CMP"),
            ("mohammed.bakir@hb.com", "محمد عبدالله عبد الرحمن بكير", "مدير إدارة الخزينة", "DepartmentManager", "HQ", "DEP-TRS"),
            ("khalid.barakat@hb.com", "خالد محمد عوض بركات", "مدير إدارة الرقابة على الائتمان", "DepartmentManager", "HQ", "DEP-CRD"),
            ("mohammed.alkubsi@hb.com", "محمد يحيى محمد الكبسي", "مدير إدارة الرقابة الشرعية", "ShariahAuditor", "HQ", "DEP-SHR"),
            ("waseem.bashr@hb.com", "وسيم محمد سعيد بشر", "مدير إدارة الدفع الالكتروني", "DepartmentManager", "HQ", "DEP-EPY"),
            ("ali.alraimi@hb.com", "علي حسن محمد الريمي", "مدير الإدارة المالية", "DepartmentManager", "HQ", "DEP-FIN"),
            ("mohammed.alhabashi@hb.com", "محمد احمد علي الحبشي", "مدير إدارة التمويل", "DepartmentManager", "HQ", "DEP-FND"),
            ("mohammed.alsalahm@hb.com", "محمد علي محمد الصلاهم", "مدير إدارة الموارد البشرية", "DepartmentManager", "HQ", "DEP-HRM"),
            ("intisar.alsabahi@hb.com", "انتصار علي محمد الصباحي", "مديرة إدارة المخاطر", "DepartmentManager", "HQ", "DEP-RSK"),
            ("majid.salim@hb.com", "ماجد علوي علي سالم", "مدير إدارة الرقابة على العمليات", "DepartmentManager", "HQ", "DEP-OPC"),
            ("hamdi.saeed@hb.com", "حمدي خالد أحمد محمد سعيد", "مدير إدارة التنظيم والجودة", "DepartmentManager", "HQ", "DEP-QAL"),
            ("samih.alkhamer@hb.com", "سامح عبدالله سالم الخامر", "مدير إدارة الشئون الإدارية", "DepartmentManager", "HQ", "DEP-ADM"),
            ("mohammed.binsuroor@hb.com", "محمد مبارك أحمد بن سرور", "مدير إدارة تقنية المعلومات", "ITAdmin", "HQ", "DEP-IT"),
            ("khalid.mutafi@hb.com", "خالد مبارك محمد متعافي", "مدير الفرع الرئيسي", "BranchManager", "BR-MAIN", null),
            ("ayman.alhamwi@hb.com", "ايمن سالم العبد الحموي", "ق.ب مدير فرع فوه", "BranchManager", "BR-FUW", null),
            ("abdullah.aljafri@hb.com", "عبدالله جيلاني عبدالله الجفري", "مدير فرع تريم", "BranchManager", "BR-TRM", null),
            ("abdulkarim.binshahbal@hb.com", "عبدالكريم قاسم بن شحبل", "مدير فرع عتق", "BranchManager", "BR-ATQ", null),
            ("anisa.mahdi@hb.com", "انيسة علي عبدالسلام مهدي", "مديرة فرع عدن", "BranchManager", "BR-ADN", null),
            ("amer.alsaqaf@hb.com", "عامر عبدالولي عبدالعزيز السقاف", "مدير فرع ال90 - عدن", "BranchManager", "BR-AD90", null),
            ("ali.alnaqeeb@hb.com", "علي محمد صالح النقيب", "مدير فرع سيئون", "BranchManager", "BR-SYN", null),
            ("jihad.alsaqaf@hb.com", "جهاد إبراهيم محمد السقاف", "مدير فرع بويش", "BranchManager", "BR-BWS", null),
            ("hafeez.bahdila@hb.com", "حفيظ سالم محفوظ باهديلة", "مدير فرع الشحر", "BranchManager", "BR-SHR", null),
            ("aziz.hamda@hb.com", "عزيز صالح عبود حمدة", "ق.ب مدير فرع الغيضة", "BranchManager", "BR-GYD", null),
            ("ali.alawlaqi@hb.com", "علي عبدالله صالح العولقي", "مدير مكتب المطار", "OfficeManager", "OF-APT", null),
            ("mahrous.barsheed@hb.com", "محروس عبدالله بارشيد", "مدير مكتب بن عزون", "OfficeManager", "OF-BNZ", null),
            ("ahmed.binsalm@hb.com", "أحمد عبدالرحيم بن سلم", "مدير مكتب عدن مول", "OfficeManager", "OF-ADM", null),
        };

        foreach (var (email, fullName, jobTitle, role, branchCode, deptCode) in users)
        {
            if (await userManager.FindByEmailAsync(email) is not null) continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullNameAr = fullName,
                JobTitle = jobTitle,
                EmailConfirmed = true,
                BranchId = branchCode is not null ? branches.GetValueOrDefault(branchCode) : null,
                DepartmentId = deptCode is not null ? departments.GetValueOrDefault(deptCode) : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(user, "Hb@2026pass");
            await userManager.AddToRoleAsync(user, role);
        }
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
