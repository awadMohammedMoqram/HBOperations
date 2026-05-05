using HBOperations.Application;
using HBOperations.Application.Common.DTOs;
using HBOperations.Application.Common.Interfaces;
using HBOperations.Domain.Common;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;
using HBOperations.Infrastructure;
using HBOperations.Infrastructure.Data;
using HBOperations.Infrastructure.Data.Seed;
using HBOperations.Web.Components;
using HBOperations.Web.Hubs;
using HBOperations.Web.Services;
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
    app.MapPost("/logout", async (HttpContext ctx, Microsoft.AspNetCore.Identity.SignInManager<HBOperations.Infrastructure.Identity.ApplicationUser> signInManager) =>
    {
        await signInManager.SignOutAsync();
        ctx.Response.Redirect("/login");
    }).DisableAntiforgery();

    // ── Document API Endpoints ──────────────────────────────────────────

    // Helper: check if user has access to a transaction
    static bool HasTransactionAccess(ICurrentUserService user, Transaction tx)
    {
        var globalRoles = new[] { "SuperAdmin", "CEO", "ITAdmin", "Auditor", "ComplianceOfficer" };
        if (globalRoles.Any(r => user.IsInRole(r))) return true;

        var uid = user.UserId;

        // المرسل (المُنشئ أو الموظف المُعيَّن كمرسل)
        if (tx.CreatedByUserId == uid || tx.SenderUserId == uid)
            return true;

        // الشخص المستلم المُعيَّن
        if (tx.ReceiverUserId == uid)
            return true;

        // أي عضو في فرع المرسل (لأغراض المتابعة داخل الفرع)
        if (tx.SenderBranchId.HasValue && user.BranchId == tx.SenderBranchId)
            return true;

        return false;
    }

    // Upload document for a transaction
    app.MapPost("/api/documents/upload/{transactionId:guid}", async (
        Guid transactionId,
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

        // Check transaction exists
        var transaction = await db.Transactions.FindAsync(transactionId);
        if (transaction is null) return Results.NotFound("المعاملة غير موجودة");

        // Verify user has access to this transaction
        if (!HasTransactionAccess(currentUser, transaction))
            return Results.Forbid();

        // Don't allow uploads on terminal statuses
        if (transaction.Status == TransactionStatus.Archived)
            return Results.BadRequest("لا يمكن إضافة مستندات لمعاملة مؤرشفة");

        // One file per user per transaction (sender or receiver each get exactly one slot).
        var uploaderId = Guid.Parse(userId);
        var existingForUser = await db.TransactionDocuments
            .AnyAsync(d => d.TransactionId == transactionId && d.UploadedByUserId == uploaderId);
        if (existingForUser)
            return Results.BadRequest("لقد قمت برفع ملف لهذه المعاملة بالفعل. يُسمح بملف واحد فقط لكل مستخدم.");

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
        var document = new TransactionDocument
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            OriginalFileName = fileName,
            StoragePath = result.StoragePath,
            FileSizeBytes = result.FileSizeBytes,
            Checksum = result.Checksum,
            ContentType = file.ContentType,
            DocumentType = docType.HasValue && Enum.IsDefined(typeof(DocumentType), docType.Value)
                ? (DocumentType)docType.Value
                : DocumentType.Attachment,
            Version = 1,
            UploadedByUserId = uploaderId,
            UploadedAt = DateTime.UtcNow
        };

        db.TransactionDocuments.Add(document);
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
        var doc = await db.TransactionDocuments
            .Include(d => d.Transaction)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        if (!HasTransactionAccess(currentUser, doc.Transaction))
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
        var doc = await db.TransactionDocuments
            .Include(d => d.Transaction)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        if (!HasTransactionAccess(currentUser, doc.Transaction))
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
        var doc = await db.TransactionDocuments
            .Include(d => d.Transaction)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        if (!HasTransactionAccess(currentUser, doc.Transaction))
            return Results.Forbid();

        // Don't allow deletion on terminal statuses
        if (doc.Transaction.Status == TransactionStatus.Archived)
            return Results.BadRequest("لا يمكن حذف مستندات من معاملة مؤرشفة");

        await storage.DeleteAsync(doc.StoragePath);
        db.TransactionDocuments.Remove(doc);
        await db.SaveChangesAsync();

        return Results.Ok();
    }).RequireAuthorization();

    // Export report to PDF
    app.MapGet("/api/reports/export-pdf", async (IAppDbContext db, ICurrentUserService currentUser, PdfReportService pdfService, string? period, string? type, string? branchId) =>
    {
        var (data, summary) = await BuildReportData(db, currentUser, period, type, branchId);
        var reportDate = AppTime.YemenNow;
        var generatedBy = currentUser.FullName ?? "مستخدم النظام";

        byte[] pdfBytes;
        string fileName;

        var globalRoles = new[] { "SuperAdmin", "CEO", "ITAdmin", "AssistantCEO", "Auditor", "ComplianceOfficer", "ShariahAuditor" };
        var hasGlobalAccess = globalRoles.Any(r => currentUser.IsInRole(r));

        if (hasGlobalAccess)
        {
            var branchReport = await BuildBranchReport(data, db);
            pdfBytes = await pdfService.GenerateBranchReportAsync(branchReport, summary, generatedBy, reportDate);
            fileName = $"تقرير_الفروع_{reportDate:yyyy-MM-dd}.pdf";
        }
        else
        {
            // Personal/department report as branch format
            var branchReport = await BuildBranchReport(data, db);
            pdfBytes = await pdfService.GenerateBranchReportAsync(branchReport, summary, generatedBy, reportDate);
            fileName = $"تقرير_المعاملات_{reportDate:yyyy-MM-dd}.pdf";
        }

        return Results.File(pdfBytes, "application/pdf", fileName);
    }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute
    {
        Roles = "SuperAdmin,CEO,AssistantCEO,DepartmentManager,BranchManager,OfficeManager,Auditor,ComplianceOfficer,ShariahAuditor,ITAdmin,DepartmentStaff,BranchStaff"
    });

    // Export report to Excel
    app.MapGet("/api/reports/export-excel", async (IAppDbContext db, ICurrentUserService currentUser, string? period, string? type, string? branchId) =>
    {
        var (data, summary) = await BuildReportData(db, currentUser, period, type, branchId);
        var reportDate = AppTime.YemenNow;

        var globalRoles = new[] { "SuperAdmin", "CEO", "ITAdmin", "AssistantCEO", "Auditor", "ComplianceOfficer", "ShariahAuditor" };
        var hasGlobalAccess = globalRoles.Any(r => currentUser.IsInRole(r));

        using var workbook = new XLWorkbook();

        // Sheet 1: Summary
        var wsSummary = workbook.Worksheets.Add("الملخص");
        wsSummary.RightToLeft = true;
        wsSummary.Cell(1, 1).Value = "تقرير المعاملات — بنك حضرموت";
        wsSummary.Cell(1, 1).Style.Font.Bold = true;
        wsSummary.Cell(1, 1).Style.Font.FontSize = 14;
        wsSummary.Cell(2, 1).Value = $"تاريخ التقرير: {reportDate:yyyy/MM/dd}";
        wsSummary.Cell(3, 1).Value = $"أُعد بواسطة: {currentUser.FullName ?? "—"}";

        wsSummary.Cell(5, 1).Value = "إجمالي المعاملات";
        wsSummary.Cell(5, 2).Value = summary.Total;
        wsSummary.Cell(6, 1).Value = "مستلمة";
        wsSummary.Cell(6, 2).Value = summary.Completed;
        wsSummary.Cell(7, 1).Value = "معلّقة";
        wsSummary.Cell(7, 2).Value = summary.Pending;
        wsSummary.Cell(8, 1).Value = "مرفوضة";
        wsSummary.Cell(8, 2).Value = summary.Rejected;
        wsSummary.Cell(9, 1).Value = "نسبة الإنجاز";
        wsSummary.Cell(9, 2).Value = $"{summary.CompletionRate:F1}%";
        wsSummary.Columns().AdjustToContents();

        // Sheet 2: Branch Data
        if (hasGlobalAccess)
        {
            var branchReport = await BuildBranchReport(data, db);
            var ws = workbook.Worksheets.Add("تقرير الفروع");
            ws.RightToLeft = true;

            var headers = new[] { "الفرع", "الصادرة", "الواردة", "الإجمالي", "معلّقة", "مستلمة", "مرفوضة" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0, 61, 122);
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var r in branchReport)
            {
                ws.Cell(row, 1).Value = r.BranchName;
                ws.Cell(row, 2).Value = r.Outgoing;
                ws.Cell(row, 3).Value = r.Incoming;
                ws.Cell(row, 4).Value = r.Total;
                ws.Cell(row, 5).Value = r.Pending;
                ws.Cell(row, 6).Value = r.Completed;
                ws.Cell(row, 7).Value = r.Rejected;
                row++;
            }

            // Total row
            ws.Cell(row, 1).Value = "المجموع";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = branchReport.Sum(r => r.Outgoing);
            ws.Cell(row, 3).Value = branchReport.Sum(r => r.Incoming);
            ws.Cell(row, 4).Value = branchReport.Sum(r => r.Total);
            ws.Cell(row, 5).Value = branchReport.Sum(r => r.Pending);
            ws.Cell(row, 6).Value = branchReport.Sum(r => r.Completed);
            ws.Cell(row, 7).Value = branchReport.Sum(r => r.Rejected);
            ws.Row(row).Style.Font.Bold = true;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(232, 244, 253);

            ws.Columns().AdjustToContents();
            ws.Range(1, 1, row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, row, 7).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.SheetView.FreezeRows(1);
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        return Results.File(ms,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"تقرير_المعاملات_{reportDate:yyyy-MM-dd}.xlsx");
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
    IQueryable<Transaction> query = db.Transactions.AsNoTracking();

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
    if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, out var typeFilter))
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
                t.Status == TransactionStatus.Sent ||
                t.Status == TransactionStatus.InTransit ||
                t.CreatedByUserId == userId ||
                t.SenderUserId == userId ||
                t.PickedUpByUserId == userId);
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
        Completed = data.Count(t => t.Status == TransactionStatus.Received || t.Status == TransactionStatus.Archived),
        Pending = data.Count(t => t.Status == TransactionStatus.Sent || t.Status == TransactionStatus.InTransit),
        Rejected = data.Count(t => t.Status == TransactionStatus.Rejected),
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
            Pending = data.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && (t.Status == TransactionStatus.Sent || t.Status == TransactionStatus.InTransit)),
            Completed = data.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && (t.Status == TransactionStatus.Received || t.Status == TransactionStatus.Archived)),
            Rejected = data.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && t.Status == TransactionStatus.Rejected)
        };
    }).Where(x => x != null).Cast<BranchReportRow>().ToList();
}

// Lightweight DTO for report data in endpoints
record ReportTxData
{
    public TransactionStatus Status { get; init; }
    public TransactionType Type { get; init; }
    public Guid? SenderBranchId { get; init; }
    public Guid? ReceiverBranchId { get; init; }
    public Guid? SenderDepartmentId { get; init; }
    public Guid? ReceiverDepartmentId { get; init; }
    public DateTime SentAt { get; init; }
    public DateTime? ReceivedAt { get; init; }
}