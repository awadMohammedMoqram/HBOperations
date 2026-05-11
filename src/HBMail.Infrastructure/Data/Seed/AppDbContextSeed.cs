using HBMail.Domain.Entities;
using HBMail.Domain.Enums;
using HBMail.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HBMail.Infrastructure.Data.Seed;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            await SeedRolesAsync(roleManager);
            await SeedBranchesAsync(context);
            await SeedDepartmentsAsync(context);
            await SeedSystemSettingsAsync(context);
            await SeedAdminAffairsStaffAsync(context, userManager, logger);
            await SeedSuperAdminAsync(context, userManager, logger);
            await SeedManagementUsersAsync(context, userManager, logger);
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
            new() { Id = Guid.NewGuid(), Key = "Mail.MaxDocuments", Value = "10", DescriptionAr = "الحد الأقصى لعدد المرفقات لكل بريد", Category = "Mail", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "Mail.MaxFileSizeMB", Value = "50", DescriptionAr = "الحد الأقصى لحجم الملف (ميجابايت)", Category = "Mail", ValueType = "int" },
            new() { Id = Guid.NewGuid(), Key = "Mail.DefaultPriority", Value = "0", DescriptionAr = "الأولوية الافتراضية للبريد الجديدة (0=عادية)", Category = "Mail", ValueType = "int" },
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

    private static async Task SeedAdminAffairsStaffAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Only run once: if سعيد قمري already exists, skip
        if (await userManager.FindByNameAsync("saeedqamari") != null)
            return;

        // Get إدارة الشئون الإدارية department and الإدارة العامة branch
        var adminDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "DEP-ADM");
        var hqBranch = await context.Branches.FirstOrDefaultAsync(b => b.Code == "HQ");

        if (adminDept == null || hqBranch == null)
        {
            logger.LogWarning("Cannot seed سعيد قمري: DEP-ADM or HQ not found");
            return;
        }

        // Create سعيد قمري
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullNameAr = "سعيد قمري",
            UserName = "saeedqamari",
            BranchId = hqBranch.Id,
            DepartmentId = adminDept.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "DepartmentStaff");
            logger.LogInformation("Created user سعيد قمري ({UserName}) in إدارة الشئون الإدارية", user.UserName);
        }
        else
        {
            logger.LogError("Failed to create سعيد قمري: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedSuperAdminAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        const string username = "awadmqram";
        if (await userManager.FindByNameAsync(username) != null)
            return;

        var hqBranch = await context.Branches.FirstOrDefaultAsync(b => b.Code == "HQ");
        var itDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "DEP-IT");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullNameAr = "عوض مقرم",
            UserName = username,
            BranchId = hqBranch?.Id,
            DepartmentId = itDept?.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "SuperAdmin");
            logger.LogInformation("Created SuperAdmin user عوض مقرم ({UserName})", username);
        }
        else
        {
            logger.LogError("Failed to create SuperAdmin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedManagementUsersAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Check if already seeded
        if (await userManager.FindByNameAsync("modirnizam") != null)
            return;

        var branches = await context.Branches.ToDictionaryAsync(b => b.Code, b => b.Id);
        var departments = await context.Departments.ToDictionaryAsync(d => d.Code, d => d.Id);

        // (FullNameAr, UserName, Roles[], BranchCode, DeptCode?)
        var users = new (string FullNameAr, string UserName, string[] Roles, string BranchCode, string? DeptCode)[]
        {
            // === القيادة والإدارة العليا ===
            ("مدير النظام", "modirnizam", ["SuperAdmin", "ITAdmin"], "HQ", "DEP-IT"),
            ("محمد مبارك بن سرور", "mohamedmubarak", ["ITAdmin"], "HQ", "DEP-IT"),
            ("عبدالناصر نعمان الحاج", "abdalnasiralhaj", ["CEO"], "HQ", null),
            ("النعمان عبدالله بكير", "alnomanbukair", ["AssistantCEO"], "HQ", "DEP-OPS"),
            ("يوسف يحيى المزيقر", "yousifmaziqr", ["AssistantCEO"], "HQ", "DEP-TRS"),
            ("جمال الوهبي", "jamalwahbi", ["Auditor"], "HQ", "DEP-AUD"),
            ("أمين الصغير", "aminsaghir", ["ComplianceOfficer"], "HQ", "DEP-CMP"),
            ("محمد الكبسي", "mohamedkabsi", ["ShariahAuditor"], "HQ", "DEP-SHR"),

            // === مدراء الإدارات (DepartmentManager) ===
            ("بسام أحمد", "bassamahmed", ["DepartmentManager"], "HQ", "DEP-OPS"),
            ("محمد بكير", "mohamedbukair", ["DepartmentManager"], "HQ", "DEP-TRS"),
            ("وسيم بشر", "wasimbishr", ["DepartmentManager"], "HQ", "DEP-EPY"),
            ("محمد الصلاهم", "mohamedsalahm", ["DepartmentManager"], "HQ", "DEP-HRM"),
            ("علي الريمي", "aliraimi", ["DepartmentManager"], "HQ", "DEP-FIN"),
            ("سامح الخامر", "samehkhamir", ["DepartmentManager"], "HQ", "DEP-ADM"),
            ("انتصار الصباحي", "intisarsbahi", ["DepartmentManager"], "HQ", "DEP-RSK"),
            ("ماجد سالم", "majidsalim", ["DepartmentManager"], "HQ", "DEP-OPC"),
            ("محمد الحبشي", "mohamedhabshi", ["DepartmentManager"], "HQ", "DEP-FND"),
            ("خالد بركات", "khalidbarkaat", ["DepartmentManager"], "HQ", "DEP-CRD"),
            ("حمدي سعيد", "hamdisaeed", ["DepartmentManager"], "HQ", "DEP-QAL"),

            // === مدراء الفروع (BranchManager) ===
            ("خالد متعافي", "khalidmutaafi", ["BranchManager"], "BR-MAIN", null),
            ("عامر السقاف", "amersakaf", ["BranchManager"], "BR-AD90", null),
            ("حفيظ باهديلة", "hafizbahdeelah", ["BranchManager"], "BR-SHR", null),
            ("عزيز حمدة", "azizhamdah", ["BranchManager"], "BR-GYD", null),
            ("جهاد السقاف", "jihadsakaf", ["BranchManager"], "BR-BWS", null),
            ("عبدالله الجفري", "abdullahjafri", ["BranchManager"], "BR-TRM", null),
            ("علي النقيب", "alinaqeeb", ["BranchManager"], "BR-SYN", null),
            ("عبدالكريم بن شحبل", "abdalkareemshahbal", ["BranchManager"], "BR-ATQ", null),
            ("انيسة مهدي", "aneesamahdi", ["BranchManager"], "BR-ADN", null),
            ("ايمن الحموي", "aymanhamwi", ["BranchManager"], "BR-FUW", null),

            // === مدراء المكاتب (OfficeManager) ===
            ("علي العولقي", "aliawlaqi", ["OfficeManager"], "OF-APT", null),
            ("محروس بارشيد", "mahroosbarsheed", ["OfficeManager"], "OF-BNZ", null),
            ("أحمد بن سلم", "ahmedbnsalm", ["OfficeManager"], "OF-ADM", null),
        };

        var created = 0;
        foreach (var (fullName, userName, roles, branchCode, deptCode) in users)
        {
            if (await userManager.FindByNameAsync(userName) != null)
                continue;

            // Password: 1st letter uppercase + 2nd letter lowercase + @123321
            var password = $"{char.ToUpper(userName[0])}{char.ToLower(userName[1])}@123321";

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullNameAr = fullName,
                UserName = userName,
                BranchId = branches.GetValueOrDefault(branchCode),
                DepartmentId = deptCode != null ? departments.GetValueOrDefault(deptCode) : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(user, roles);
                created++;
                logger.LogInformation("Created user {FullName} ({UserName})", fullName, userName);
            }
            else
            {
                logger.LogError("Failed to create {FullName} ({UserName}): {Errors}",
                    fullName, userName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        logger.LogInformation("Seeded {Count} management users", created);
    }
}
