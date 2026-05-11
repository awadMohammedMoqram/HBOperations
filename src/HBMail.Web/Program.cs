using HBMail.Application;
using HBMail.Application.Common.DTOs;
using HBMail.Application.Common.Interfaces;
using HBMail.Application.Common.Reports;
using HBMail.Domain.Common;
using HBMail.Domain.Entities;
using HBMail.Domain.Enums;
using HBMail.Infrastructure;
using HBMail.Infrastructure.Data;
using HBMail.Infrastructure.Data.Seed;
using HBMail.Web.Components;
using HBMail.Web.Hubs;
using HBMail.Web.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Add layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents(options =>
        {
            // Allow larger circuit messages so InputFile can stream PDFs (default is tiny ~32KB).
            options.DisconnectedCircuitMaxRetained = 100;
        })
        .AddHubOptions(options =>
        {
            // Up to 25 MB per SignalR message — covers PDF uploads (we cap original at 20 MB).
            options.MaximumReceiveMessageSize = 25 * 1024 * 1024;
            options.EnableDetailedErrors = true;
        });

    builder.Services.AddSignalR(options =>
    {
        // Notification hub — keep this generous too.
        options.MaximumReceiveMessageSize = 25 * 1024 * 1024;
    });
    builder.Services.AddSingleton<NotificationEventService>();
    builder.Services.AddScoped<ToastService>();
    builder.Services.AddScoped<PdfReportService>();
    builder.Services.AddScoped<IRealTimeNotifier, SignalRNotifier>();
    builder.Services.AddHostedService<AutoArchiveService>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCascadingAuthenticationState();

    var app = builder.Build();

    // Seed database
    await AppDbContextSeed.SeedAsync(app.Services);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    // Security Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        // SAMEORIGIN allows embedding in iframes from the same origin (needed for PDF preview)
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
            "img-src 'self' data:; connect-src 'self' ws: wss:; frame-src 'self'; object-src 'self';";
        await next();
    });

    app.UseSerilogRequestLogging();
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    // Logout endpoint
    app.MapPost("/logout", async (HttpContext ctx, Microsoft.AspNetCore.Identity.SignInManager<HBMail.Infrastructure.Identity.ApplicationUser> signInManager) =>
    {
        await signInManager.SignOutAsync();
        ctx.Response.Redirect("/login");
    }).DisableAntiforgery();

    // ── Document API Endpoints ──────────────────────────────────────────

    // Helper: check if user has access to a Mail
    static bool HasTransactionAccess(ICurrentUserService user, Mail mail)
    {
        var globalRoles = new[] { "SuperAdmin", "CEO", "ITAdmin", "Auditor", "ComplianceOfficer" };
        if (globalRoles.Any(r => user.IsInRole(r))) return true;

        var uid = user.UserId;

        // المرسل (المُنشئ أو الموظف المُعيَّن كمرسل)
        if (mail.CreatedByUserId == uid || mail.SenderUserId == uid)
            return true;

        // الشخص المستلم المُعيَّن
        if (mail.ReceiverUserId == uid)
            return true;

        // أي عضو في فرع المرسل (لأغراض المتابعة داخل الفرع)
        if (mail.SenderBranchId.HasValue && user.BranchId == mail.SenderBranchId)
            return true;

        return false;
    }

    // Upload document for a Mail
    app.MapPost("/api/documents/upload/{MailId:guid}", async (
        Guid MailId,
        IFormFile file,
        IAppDbContext db,
        IFileStorageService storage,
        IFileValidationService validation,
        IPdfCompressionService pdfCompression,
        ICurrentUserService currentUser,
        HttpContext ctx,
        int? docType) =>
    {
        var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Results.Unauthorized();

        // Check Mail exists
        var Mail = await db.Mails.FindAsync(MailId);
        if (Mail is null) return Results.NotFound("البريد غير موجودة");

        // Verify user has access to this Mail
        if (!HasTransactionAccess(currentUser, Mail))
            return Results.Forbid();

        // Don't allow uploads on terminal statuses
        if (Mail.Status == MailStatus.Archived)
            return Results.BadRequest("لا يمكن إضافة مرفقات لبريد مؤرشفة");

        // One file per user per Mail (sender or receiver each get exactly one slot).
        var uploaderId = Guid.Parse(userId);
        var existingForUser = await db.MailAttachments
            .AnyAsync(d => d.MailId == MailId && d.UploadedByUserId == uploaderId);
        if (existingForUser)
            return Results.BadRequest("لقد قمت برفع ملف لهذا البريد بالفعل. يُسمح بملف واحد فقط لكل مستخدم.");

        // Validate file
        using var stream = file.OpenReadStream();
        var validationResult = validation.Validate(stream, file.FileName, file.ContentType, file.Length);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.ErrorMessage);

        // Reset stream position after validation
        stream.Position = 0;

        // Compress PDF (PdfSharpCore — MIT). Falls back to original on failure.
        var compression = await pdfCompression.CompressAsync(stream);

        // Hard cap: post-compression size must not exceed 5 MB.
        const long MaxCompressedBytes = 5L * 1024 * 1024;
        if (compression.CompressedSizeBytes > MaxCompressedBytes)
        {
            compression.OutputStream.Dispose();
            var sizeMb = compression.CompressedSizeBytes / (1024.0 * 1024.0);
            return Results.BadRequest(
                $"حجم الملف بعد الضغط ({sizeMb:F2} ميجا) يتجاوز الحد المسموح (5 ميجا). يرجى تقليل حجم الملف الأصلي.");
        }

        await using var uploadStream = compression.OutputStream;

        // Upload file
        var result = await storage.UploadAsync(uploadStream, file.FileName, file.ContentType);

        // Save document record (one per user — version is always 1)
        var fileName = Path.GetFileName(file.FileName);
        var document = new MailAttachment
        {
            Id = Guid.NewGuid(),
            MailId = MailId,
            OriginalFileName = fileName,
            StoragePath = result.StoragePath,
            FileSizeBytes = result.FileSizeBytes,
            Checksum = result.Checksum,
            ContentType = file.ContentType,
            AttachmentType = docType.HasValue && Enum.IsDefined(typeof(AttachmentType), docType.Value)
                ? (AttachmentType)docType.Value
                : AttachmentType.Attachment,
            Version = 1,
            UploadedByUserId = uploaderId,
            UploadedAt = DateTime.UtcNow
        };

        db.MailAttachments.Add(document);
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            document.Id,
            document.OriginalFileName,
            document.FileSizeBytes,
            originalSizeBytes = compression.OriginalSizeBytes,
            compressedSizeBytes = compression.CompressedSizeBytes,
            wasCompressed = compression.WasCompressed,
            savedPercent = compression.OriginalSizeBytes > 0
                ? Math.Round((1 - (double)compression.CompressedSizeBytes / compression.OriginalSizeBytes) * 100, 1)
                : 0
        });
    }).RequireAuthorization().DisableAntiforgery();

    // Download document
    app.MapGet("/api/documents/{id:guid}/download", async (
        Guid id,
        IAppDbContext db,
        IFileStorageService storage,
        ICurrentUserService currentUser) =>
    {
        var doc = await db.MailAttachments
            .Include(d => d.Mail)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        if (!HasTransactionAccess(currentUser, doc.Mail))
            return Results.Forbid();

        var stream = await storage.DownloadAsync(doc.StoragePath);
        return Results.File(stream, doc.ContentType, doc.OriginalFileName);
    }).RequireAuthorization();

    // Preview document (inline PDF)
    app.MapGet("/api/documents/{id:guid}/preview", async (
        Guid id,
        IAppDbContext db,
        IFileStorageService storage,
        ICurrentUserService currentUser) =>
    {
        var doc = await db.MailAttachments
            .Include(d => d.Mail)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        if (!HasTransactionAccess(currentUser, doc.Mail))
            return Results.Forbid();

        var stream = await storage.DownloadAsync(doc.StoragePath);
        return Results.File(stream, "application/pdf");
    }).RequireAuthorization();

    // Delete document
    app.MapDelete("/api/documents/{id:guid}", async (
        Guid id,
        IAppDbContext db,
        IFileStorageService storage,
        ICurrentUserService currentUser) =>
    {
        var doc = await db.MailAttachments
            .Include(d => d.Mail)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        if (!HasTransactionAccess(currentUser, doc.Mail))
            return Results.Forbid();

        // Don't allow deletion on terminal statuses
        if (doc.Mail.Status == MailStatus.Archived)
            return Results.BadRequest("لا يمكن حذف مرفقات من بريد مؤرشفة");

        await storage.DeleteAsync(doc.StoragePath);
        db.MailAttachments.Remove(doc);
        await db.SaveChangesAsync();

        return Results.Ok();
    }).RequireAuthorization();

    // Export report to PDF
    app.MapGet("/api/reports/export-pdf", async (
        IAppDbContext db,
        ICurrentUserService currentUser,
        PdfReportService pdfService,
        IReportAccessPolicy accessPolicy,
        IReportRateLimiter rateLimiter,
        IReportSanitizer sanitizer,
        IAuditService audit,
        HttpContext http,
        string? period, string? type, string? branchId, string? reportType) =>
    {
        // 1) Rate limit
        var userId = currentUser.UserId;
        var roles = (currentUser.Roles ?? Array.Empty<string>()).ToList();
        var rl = rateLimiter.CheckLimit(userId, roles);
        if (!rl.IsAllowed)
            return Results.Json(new { error = rl.Reason }, statusCode: 429);

        // 2) Resolve report type — explicit (?reportType=) or default per scope
        var scope = await accessPolicy.GetUserScopeAsync(currentUser, db);
        ReportType selectedType;
        if (!string.IsNullOrWhiteSpace(reportType) && Enum.TryParse<ReportType>(reportType, true, out var parsed))
            selectedType = parsed;
        else
            selectedType = accessPolicy.GetDefaultReport(scope);

        if (!accessPolicy.CanAccessReport(scope, selectedType))
            return Results.Forbid();

        var reportDate = AppTime.YemenNow;
        var generatedBy = currentUser.FullName ?? "مستخدم النظام";
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "—";

        byte[] pdfBytes;
        string fileName;

        switch (selectedType)
        {
            case ReportType.AuditTrail:
            {
                var auditRows = await BuildAuditTrailReport(db, currentUser, period, type, branchId);
                auditRows = sanitizer.Sanitize(auditRows, scope); // Defense in depth
                var auditSummary = SummarizeAuditTrail(auditRows);
                pdfBytes = pdfService.GenerateAuditTrailReport(auditRows, auditSummary, generatedBy, reportDate);
                fileName = $"تقرير_التدقيق_{reportDate:yyyy-MM-dd}.pdf";
                break;
            }
            case ReportType.Personal:
            {
                var personalRows = await BuildPersonalReport(db, currentUser, period, type);
                var personalSummary = SummarizePersonal(personalRows);
                pdfBytes = pdfService.GeneratePersonalReport(personalRows, personalSummary, generatedBy, reportDate);
                fileName = $"تقريري_الشخصي_{reportDate:yyyy-MM-dd}.pdf";
                break;
            }
            case ReportType.ShariahCompliance:
            {
                // فلترة البريد النقدية فقط
                var cashRows = await BuildAuditTrailReport(db, currentUser, period, ((int)MailType.CashTransfer).ToString(), branchId);
                cashRows = sanitizer.Sanitize(cashRows, scope);
                var cashSummary = SummarizeAuditTrail(cashRows);
                pdfBytes = pdfService.GenerateShariahReport(cashRows, cashSummary, generatedBy, reportDate);
                fileName = $"تقرير_الرقابة_الشرعية_{reportDate:yyyy-MM-dd}.pdf";
                break;
            }
            case ReportType.RejectionAnalysis:
            {
                var (rejected, byTypeGrp, byBranchGrp, rejSummary) = await BuildRejectionAnalysis(db, currentUser, period, type, branchId);
                pdfBytes = pdfService.GenerateRejectionAnalysisReport(rejected, byTypeGrp, byBranchGrp, rejSummary, generatedBy, reportDate);
                fileName = $"تحليل_الرفض_{reportDate:yyyy-MM-dd}.pdf";
                break;
            }
            case ReportType.Executive:
            {
                var execData = await BuildExecutiveReport(db, currentUser, period, type, branchId);
                pdfBytes = pdfService.GenerateExecutiveReport(execData, generatedBy, reportDate);
                fileName = $"التقرير_التنفيذي_{reportDate:yyyy-MM-dd}.pdf";
                break;
            }
            default:
            {
                var (data, summary) = await BuildReportData(db, currentUser, period, type, branchId);
                var branchReport = await BuildBranchReport(data, db);
                pdfBytes = pdfService.GenerateBranchReport(branchReport, summary, generatedBy, reportDate);
                fileName = scope.HasGlobalAccess
                    ? $"تقرير_الفروع_{reportDate:yyyy-MM-dd}.pdf"
                    : $"تقرير_البريد_{reportDate:yyyy-MM-dd}.pdf";
                break;
            }
        }

        // 3) Record + audit
        rateLimiter.RecordExport(userId);
        await audit.LogAsync("Report", Guid.Empty, "Exported", null, new
        {
            ReportType = selectedType.ToString(),
            Format = "Pdf",
            Period = period,
            Type = type,
            BranchId = branchId,
            UserName = currentUser.UserName,
            FullName = generatedBy,
            Ip = ip,
            DailyCount = rl.CurrentDaily + 1,
            DailyLimit = rl.DailyLimit
        });

        return Results.File(pdfBytes, "application/pdf", fileName);
    }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute
    {
        Roles = "SuperAdmin,CEO,AssistantCEO,DepartmentManager,BranchManager,OfficeManager,Auditor,ComplianceOfficer,ShariahAuditor,ITAdmin,DepartmentStaff,BranchStaff"
    });

    // Export report to Excel
    app.MapGet("/api/reports/export-excel", async (
        IAppDbContext db,
        ICurrentUserService currentUser,
        IReportAccessPolicy accessPolicy,
        IReportRateLimiter rateLimiter,
        IReportSanitizer sanitizer,
        IAuditService audit,
        HttpContext http,
        string? period, string? type, string? branchId, string? reportType) =>
    {
        var userId = currentUser.UserId;
        var roles = (currentUser.Roles ?? Array.Empty<string>()).ToList();
        var rl = rateLimiter.CheckLimit(userId, roles);
        if (!rl.IsAllowed)
            return Results.Json(new { error = rl.Reason }, statusCode: 429);

        var scope = await accessPolicy.GetUserScopeAsync(currentUser, db);
        ReportType selectedType;
        if (!string.IsNullOrWhiteSpace(reportType) && Enum.TryParse<ReportType>(reportType, true, out var parsed))
            selectedType = parsed;
        else
            selectedType = accessPolicy.GetDefaultReport(scope);

        if (!accessPolicy.CanAccessReport(scope, selectedType))
            return Results.Forbid();

        var reportDate = AppTime.YemenNow;
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "—";
        var generatedBy = currentUser.FullName ?? "مستخدم النظام";

        using var workbook = new XLWorkbook();
        string fileName;

        switch (selectedType)
        {
            case ReportType.AuditTrail:
            {
                var rows = await BuildAuditTrailReport(db, currentUser, period, type, branchId);
                rows = sanitizer.Sanitize(rows, scope);
                var sum = SummarizeAuditTrail(rows);
                ExcelHelpers.AddSummarySheet(workbook, "تقرير المراجعة", sum, generatedBy, reportDate);
                ExcelHelpers.AddAuditTrailSheet(workbook, rows);
                fileName = $"تقرير_التدقيق_{reportDate:yyyy-MM-dd}.xlsx";
                break;
            }
            case ReportType.Personal:
            {
                var rows = await BuildPersonalReport(db, currentUser, period, type);
                var sum = SummarizePersonal(rows);
                ExcelHelpers.AddSummarySheet(workbook, "تقريري الشخصي", sum, generatedBy, reportDate);
                ExcelHelpers.AddPersonalSheet(workbook, rows);
                fileName = $"تقريري_الشخصي_{reportDate:yyyy-MM-dd}.xlsx";
                break;
            }
            case ReportType.ShariahCompliance:
            {
                var rows = await BuildAuditTrailReport(db, currentUser, period, ((int)MailType.CashTransfer).ToString(), branchId);
                rows = sanitizer.Sanitize(rows, scope);
                var sum = SummarizeAuditTrail(rows);
                ExcelHelpers.AddSummarySheet(workbook, "الرقابة الشرعية", sum, generatedBy, reportDate);
                ExcelHelpers.AddAuditTrailSheet(workbook, rows, "البريد النقدية");
                fileName = $"تقرير_الرقابة_الشرعية_{reportDate:yyyy-MM-dd}.xlsx";
                break;
            }
            case ReportType.RejectionAnalysis:
            {
                var (rejected, byTypeGrp, byBranchGrp, sum) = await BuildRejectionAnalysis(db, currentUser, period, type, branchId);
                ExcelHelpers.AddSummarySheet(workbook, "تحليل الرفض", sum, generatedBy, reportDate);
                ExcelHelpers.AddRejectionGroupSheet(workbook, "حسب النوع", "نوع البريد", byTypeGrp);
                ExcelHelpers.AddRejectionGroupSheet(workbook, "حسب الفرع", "الفرع", byBranchGrp);
                ExcelHelpers.AddRejectionDetailsSheet(workbook, rejected);
                fileName = $"تحليل_الرفض_{reportDate:yyyy-MM-dd}.xlsx";
                break;
            }
            case ReportType.Executive:
            {
                var execData = await BuildExecutiveReport(db, currentUser, period, type, branchId);
                ExcelHelpers.AddSummarySheet(workbook, "التقرير التنفيذي", execData.Summary, generatedBy, reportDate);
                if (execData.Branches.Count > 0) ExcelHelpers.AddBranchesSheet(workbook, execData.Branches);
                if (execData.Departments.Count > 0) ExcelHelpers.AddDepartmentsSheet(workbook, execData.Departments);
                // تفاصيل البريد حسب نطاق المستخدم
                var execTxRows = await BuildAuditTrailReport(db, currentUser, period, type, branchId);
                execTxRows = sanitizer.Sanitize(execTxRows, scope);
                if (execTxRows.Count > 0) ExcelHelpers.AddAuditTrailSheet(workbook, execTxRows, "البريد");
                fileName = $"التقرير_التنفيذي_{reportDate:yyyy-MM-dd}.xlsx";
                break;
            }
            default:
            {
                var (data, summary) = await BuildReportData(db, currentUser, period, type, branchId);
                ExcelHelpers.AddSummarySheet(workbook, "تقرير البريد", summary, generatedBy, reportDate);
                if (scope.HasGlobalAccess)
                {
                    var branchReport = await BuildBranchReport(data, db);
                    ExcelHelpers.AddBranchesSheet(workbook, branchReport);
                }
                // تفاصيل البريد حسب نطاق المستخدم
                var txRows = await BuildAuditTrailReport(db, currentUser, period, type, branchId);
                txRows = sanitizer.Sanitize(txRows, scope);
                if (txRows.Count > 0) ExcelHelpers.AddAuditTrailSheet(workbook, txRows, "البريد");
                fileName = $"تقرير_البريد_{reportDate:yyyy-MM-dd}.xlsx";
                break;
            }
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        rateLimiter.RecordExport(userId);
        await audit.LogAsync("Report", Guid.Empty, "Exported", null, new
        {
            ReportType = selectedType.ToString(),
            Format = "Excel",
            Period = period,
            Type = type,
            BranchId = branchId,
            UserName = currentUser.UserName,
            FullName = currentUser.FullName,
            Ip = ip,
            DailyCount = rl.CurrentDaily + 1,
            DailyLimit = rl.DailyLimit
        });

        return Results.File(ms,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute
    {
        Roles = "SuperAdmin,CEO,AssistantCEO,DepartmentManager,BranchManager,OfficeManager,Auditor,ComplianceOfficer,ShariahAuditor,ITAdmin,DepartmentStaff,BranchStaff"
    });

    app.MapStaticAssets();
    app.MapHub<NotificationHub>("/notificationhub");
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ═══════════════════════════════════════════════════════════
// Report helper methods
// ═══════════════════════════════════════════════════════════

static async Task<(List<ReportTxData> data, ReportSummary summary)> BuildReportData(
    IAppDbContext db, ICurrentUserService currentUser, string? period, string? type, string? branchId)
{
    IQueryable<Mail> query = db.Mails.AsNoTracking();

    // Period filter
    var now = DateTime.UtcNow;
    DateTime? from = period switch
    {
        "week" => now.AddDays(-7),
        "month" => new DateTime(now.Year, now.Month, 1),
        "quarter" => now.AddMonths(-3),
        "year" => new DateTime(now.Year, 1, 1),
        _ => null
    };
    if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);

    // Type filter
    if (!string.IsNullOrEmpty(type) && Enum.TryParse<MailType>(type, out var typeFilter))
        query = query.Where(t => t.Type == typeFilter);

    // Role-based filtering
    var globalRoles = new[] { "SuperAdmin", "CEO", "ITAdmin", "AssistantCEO", "Auditor", "ComplianceOfficer", "ShariahAuditor" };
    var hasGlobalAccess = globalRoles.Any(r => currentUser.IsInRole(r));

    if (!hasGlobalAccess)
    {
        var userId = currentUser.UserId;
        var userBranchId = currentUser.BranchId;
        var deptId = currentUser.DepartmentId;

        var isAdminAffairs = deptId.HasValue && await db.Departments.AsNoTracking()
            .AnyAsync(d => d.Id == deptId.Value && d.Code == "DEP-ADM");

        if (isAdminAffairs)
        {
            query = query.Where(t =>
                t.Status == MailStatus.Sent ||
                t.Status == MailStatus.AssignedToStaff ||
                t.CreatedByUserId == userId ||
                t.SenderUserId == userId ||
                t.ReceiverUserId == userId);
        }
        else if ((currentUser.IsInRole("DepartmentManager") || currentUser.IsInRole("DepartmentStaff")) && deptId.HasValue)
        {
            query = query.Where(t =>
                t.CreatedByUserId == userId || t.SenderUserId == userId ||
                t.ReceiverUserId == userId ||
                t.SenderDepartmentId == deptId || t.ReceiverDepartmentId == deptId);
        }
        else
        {
            query = query.Where(t =>
                t.CreatedByUserId == userId || t.SenderUserId == userId ||
                t.ReceiverUserId == userId ||
                (userBranchId.HasValue && (t.SenderBranchId == userBranchId || t.ReceiverBranchId == userBranchId)));
        }
    }
    else if (!string.IsNullOrEmpty(branchId) && Guid.TryParse(branchId, out var brFilter))
    {
        query = query.Where(t => t.SenderBranchId == brFilter || t.ReceiverBranchId == brFilter);
    }

    var data = await query.Select(t => new ReportTxData
    {
        Status = t.Status,
        Type = t.Type,
        SenderBranchId = t.SenderBranchId,
        ReceiverBranchId = t.ReceiverBranchId,
        SenderDepartmentId = t.SenderDepartmentId,
        ReceiverDepartmentId = t.ReceiverDepartmentId,
        SentAt = t.SentAt,
        ReceivedAt = t.ReceivedAt
    }).ToListAsync();

    var summary = new ReportSummary
    {
        Total = data.Count,
        Completed = data.Count(t => t.Status == MailStatus.Received || t.Status == MailStatus.Archived),
        Pending = data.Count(t => t.Status == MailStatus.Sent || t.Status == MailStatus.AssignedToStaff),
        Rejected = data.Count(t => t.Status == MailStatus.Rejected),
    };

    var completedWithTime = data.Where(t => t.ReceivedAt.HasValue).ToList();
    if (completedWithTime.Count > 0)
        summary.AvgProcessingHours = completedWithTime.Average(t => (t.ReceivedAt!.Value - t.SentAt).TotalHours);

    return (data, summary);
}

static async Task<List<BranchReportRow>> BuildBranchReport(List<ReportTxData> data, IAppDbContext db)
{
    var branches = await db.Branches.AsNoTracking()
        .Where(b => b.IsActive).OrderBy(b => b.NameAr)
        .Select(b => new { b.Id, b.NameAr }).ToListAsync();

    return branches.Select(b =>
    {
        var outgoing = data.Count(t => t.SenderBranchId == b.Id);
        var incoming = data.Count(t => t.ReceiverBranchId == b.Id);
        var total = outgoing + incoming;
        if (total == 0) return null;
        return new BranchReportRow
        {
            BranchName = b.NameAr,
            Outgoing = outgoing,
            Incoming = incoming,
            Total = total,
            Pending = data.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && (t.Status == MailStatus.Sent || t.Status == MailStatus.AssignedToStaff)),
            Completed = data.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && (t.Status == MailStatus.Received || t.Status == MailStatus.Archived)),
            Rejected = data.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && t.Status == MailStatus.Rejected)
        };
    }).Where(x => x != null).Cast<BranchReportRow>().ToList();
}

// ───────── Audit Trail Report (للمدققين) ─────────
static async Task<List<AuditTrailReportRow>> BuildAuditTrailReport(
    IAppDbContext db, ICurrentUserService currentUser, string? period, string? type, string? branchId)
{
    IQueryable<Mail> query = db.Mails.AsNoTracking();

    var now = DateTime.UtcNow;
    DateTime? from = period switch
    {
        "week" => now.AddDays(-7),
        "month" => new DateTime(now.Year, now.Month, 1),
        "quarter" => now.AddMonths(-3),
        "year" => new DateTime(now.Year, 1, 1),
        _ => null
    };
    if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
    if (!string.IsNullOrEmpty(type) && Enum.TryParse<MailType>(type, out var tFilter))
        query = query.Where(t => t.Type == tFilter);
    if (!string.IsNullOrEmpty(branchId) && Guid.TryParse(branchId, out var brFilter))
        query = query.Where(t => t.SenderBranchId == brFilter || t.ReceiverBranchId == brFilter);

    var rows = await query
        .OrderByDescending(t => t.CreatedAt)
        .Take(500) // Cap to avoid huge PDFs
        .Select(t => new AuditTrailReportRow
        {
            ReferenceNumber = t.ReferenceNumber,
            Subject = t.Subject,
            Type = t.Type,
            Status = t.Status,
            Priority = t.Priority,
            CreatedAt = t.CreatedAt,
            SentAt = t.SentAt,
            ReceivedAt = t.ReceivedAt,
            RejectionNote = t.RejectionNote,
            AdminNote = t.AdminNote,
            SenderNote = t.SenderNote,
            SenderBranch = t.SenderBranchId.HasValue
                ? db.Branches.Where(b => b.Id == t.SenderBranchId).Select(b => b.NameAr).FirstOrDefault()
                : null,
            ReceiverBranch = t.ReceiverBranchId.HasValue
                ? db.Branches.Where(b => b.Id == t.ReceiverBranchId).Select(b => b.NameAr).FirstOrDefault()
                : null,
        })
        .ToListAsync();

    return rows;
}

static ReportSummary SummarizeAuditTrail(List<AuditTrailReportRow> rows) => new()
{
    Total = rows.Count,
    Completed = rows.Count(r => r.Status == MailStatus.Received || r.Status == MailStatus.Archived),
    Pending = rows.Count(r => r.Status == MailStatus.Sent || r.Status == MailStatus.AssignedToStaff),
    Rejected = rows.Count(r => r.Status == MailStatus.Rejected),
};

// ───────── Personal Report (للموظف) ─────────
static async Task<List<PersonalReportRow>> BuildPersonalReport(
    IAppDbContext db, ICurrentUserService currentUser, string? period, string? type)
{
    var userId = currentUser.UserId;
    IQueryable<Mail> query = db.Mails.AsNoTracking()
        .Where(t => t.CreatedByUserId == userId || t.SenderUserId == userId || t.ReceiverUserId == userId);

    var now = DateTime.UtcNow;
    DateTime? from = period switch
    {
        "week" => now.AddDays(-7),
        "month" => new DateTime(now.Year, now.Month, 1),
        "quarter" => now.AddMonths(-3),
        "year" => new DateTime(now.Year, 1, 1),
        _ => null
    };
    if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
    if (!string.IsNullOrEmpty(type) && Enum.TryParse<MailType>(type, out var tFilter))
        query = query.Where(t => t.Type == tFilter);

    var rows = await query.OrderByDescending(t => t.CreatedAt).Take(500)
        .Select(t => new
        {
            t.ReferenceNumber,
            t.Subject,
            t.Type,
            t.Status,
            t.CreatedAt,
            t.ReceivedAt,
            IsOutgoing = t.SenderUserId == userId || t.CreatedByUserId == userId,
            CounterBranch = (t.SenderUserId == userId || t.CreatedByUserId == userId)
                ? (t.ReceiverBranchId.HasValue ? db.Branches.Where(b => b.Id == t.ReceiverBranchId).Select(b => b.NameAr).FirstOrDefault() : null)
                : (t.SenderBranchId.HasValue ? db.Branches.Where(b => b.Id == t.SenderBranchId).Select(b => b.NameAr).FirstOrDefault() : null)
        }).ToListAsync();

    return rows.Select(r => new PersonalReportRow
    {
        ReferenceNumber = r.ReferenceNumber,
        Subject = r.Subject,
        Type = r.Type,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        CompletedAt = r.ReceivedAt,
        Direction = r.IsOutgoing ? "صادرة" : "واردة",
        CounterpartName = r.CounterBranch
    }).ToList();
}

static ReportSummary SummarizePersonal(List<PersonalReportRow> rows) => new()
{
    Total = rows.Count,
    Completed = rows.Count(r => r.Status == MailStatus.Received || r.Status == MailStatus.Archived),
    Pending = rows.Count(r => r.Status == MailStatus.Sent || r.Status == MailStatus.AssignedToStaff),
    Rejected = rows.Count(r => r.Status == MailStatus.Rejected),
};

// ───────── Rejection Analysis (تحليل أسباب الرفض) ─────────
static async Task<(List<RejectionAnalysisRow> rows, List<RejectionGroup> byType, List<RejectionGroup> byBranch, ReportSummary summary)>
    BuildRejectionAnalysis(IAppDbContext db, ICurrentUserService currentUser, string? period, string? type, string? branchId)
{
    IQueryable<Mail> query = db.Mails.AsNoTracking()
        .Where(t => t.Status == MailStatus.Rejected);

    var now = DateTime.UtcNow;
    DateTime? from = period switch
    {
        "week" => now.AddDays(-7),
        "month" => new DateTime(now.Year, now.Month, 1),
        "quarter" => now.AddMonths(-3),
        "year" => new DateTime(now.Year, 1, 1),
        _ => null
    };
    if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
    if (!string.IsNullOrEmpty(type) && Enum.TryParse<MailType>(type, out var tFilter))
        query = query.Where(t => t.Type == tFilter);
    if (!string.IsNullOrEmpty(branchId) && Guid.TryParse(branchId, out var brFilter))
        query = query.Where(t => t.SenderBranchId == brFilter || t.ReceiverBranchId == brFilter);

    var rows = await query
        .OrderByDescending(t => t.CreatedAt)
        .Take(500)
        .Select(t => new RejectionAnalysisRow
        {
            ReferenceNumber = t.ReferenceNumber,
            Subject = t.Subject,
            Type = t.Type,
            RejectedAt = t.CreatedAt,
            RejectionNote = t.RejectionNote,
            RejectedBy = "—",
            SenderBranch = t.SenderBranchId.HasValue
                ? db.Branches.Where(b => b.Id == t.SenderBranchId).Select(b => b.NameAr).FirstOrDefault()
                : null,
            ReceiverBranch = t.ReceiverBranchId.HasValue
                ? db.Branches.Where(b => b.Id == t.ReceiverBranchId).Select(b => b.NameAr).FirstOrDefault()
                : null,
        })
        .ToListAsync();

    var totalCount = rows.Count;

    var byType = rows.GroupBy(r => r.Type)
        .Select(g => new RejectionGroup
        {
            Label = g.Key switch
            {
                MailType.DocumentDelivery => "تسليم مرفقات",
                MailType.CashTransfer => "تحويل نقدي",
                MailType.InternalDepartment => "داخلي بين الإدارات",
                MailType.BranchToBranch => "بين الفروع",
                _ => "أخرى"
            },
            Count = g.Count(),
            Percentage = totalCount > 0 ? (double)g.Count() / totalCount * 100 : 0
        })
        .OrderByDescending(g => g.Count)
        .ToList();

    var byBranch = rows.Where(r => !string.IsNullOrEmpty(r.SenderBranch))
        .GroupBy(r => r.SenderBranch!)
        .Select(g => new RejectionGroup
        {
            Label = g.Key,
            Count = g.Count(),
            Percentage = totalCount > 0 ? (double)g.Count() / totalCount * 100 : 0
        })
        .OrderByDescending(g => g.Count)
        .Take(10)
        .ToList();

    var summary = new ReportSummary
    {
        Total = totalCount,
        Rejected = totalCount,
        Pending = 0,
        Completed = 0
    };

    return (rows, byType, byBranch, summary);
}

// ───────── Executive Report (للإدارة العليا) ─────────
static async Task<ExecutiveReportData> BuildExecutiveReport(
    IAppDbContext db, ICurrentUserService currentUser, string? period, string? type, string? branchId)
{
    var (data, summary) = await BuildReportData(db, currentUser, period, type, branchId);
    var branches = await BuildBranchReport(data, db);

    var deptIds = await db.Departments.AsNoTracking()
        .Where(d => d.IsActive).OrderBy(d => d.NameAr)
        .Select(d => new { d.Id, d.NameAr }).ToListAsync();

    var departments = deptIds.Select(d =>
    {
        var outgoing = data.Count(t => t.SenderDepartmentId == d.Id);
        var incoming = data.Count(t => t.ReceiverDepartmentId == d.Id);
        var total = outgoing + incoming;
        if (total == 0) return null;
        return new DepartmentReportRow
        {
            DepartmentName = d.NameAr,
            Outgoing = outgoing,
            Incoming = incoming,
            Total = total,
            Pending = data.Count(t => (t.SenderDepartmentId == d.Id || t.ReceiverDepartmentId == d.Id) && (t.Status == MailStatus.Sent || t.Status == MailStatus.AssignedToStaff)),
            Completed = data.Count(t => (t.SenderDepartmentId == d.Id || t.ReceiverDepartmentId == d.Id) && (t.Status == MailStatus.Received || t.Status == MailStatus.Archived)),
            Rejected = data.Count(t => (t.SenderDepartmentId == d.Id || t.ReceiverDepartmentId == d.Id) && t.Status == MailStatus.Rejected)
        };
    }).Where(x => x != null).Cast<DepartmentReportRow>().OrderByDescending(d => d.Total).ToList();

    var typeBreakdown = data.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.Count());

    var periodLabel = period switch
    {
        "week" => "هذا الأسبوع",
        "month" => "هذا الشهر",
        "quarter" => "آخر 3 أشهر",
        "year" => "هذه السنة",
        _ => "كل الفترات"
    };

    return new ExecutiveReportData
    {
        Summary = summary,
        Branches = branches.OrderByDescending(b => b.Total).ToList(),
        Departments = departments,
        TypeBreakdown = typeBreakdown,
        TotalRejected = summary.Rejected,
        TotalArchived = data.Count(t => t.Status == MailStatus.Archived),
        PeriodLabel = periodLabel
    };
}

// Lightweight DTO for report data in endpoints
record ReportTxData
{
    public MailStatus Status { get; init; }
    public MailType Type { get; init; }
    public Guid? SenderBranchId { get; init; }
    public Guid? ReceiverBranchId { get; init; }
    public Guid? SenderDepartmentId { get; init; }
    public Guid? ReceiverDepartmentId { get; init; }
    public DateTime SentAt { get; init; }
    public DateTime? ReceivedAt { get; init; }
}