using HBOperations.Application;
using HBOperations.Application.Common.Interfaces;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;
using HBOperations.Infrastructure;
using HBOperations.Infrastructure.Data.Seed;
using HBOperations.Web.Components;
using HBOperations.Web.Hubs;
using HBOperations.Web.Services;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
        .AddInteractiveServerComponents();

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<NotificationEventService>();
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
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
            "img-src 'self' data:; connect-src 'self' ws: wss:; frame-src 'self';";
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

    // Upload document for a transaction
    app.MapPost("/api/documents/upload/{transactionId:guid}", async (
        Guid transactionId,
        IFormFile file,
        IAppDbContext db,
        IFileStorageService storage,
        IFileValidationService validation,
        HttpContext ctx) =>
    {
        var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Results.Unauthorized();

        // Check transaction exists
        var transaction = await db.Transactions.FindAsync(transactionId);
        if (transaction is null) return Results.NotFound("المعاملة غير موجودة");

        // Check document count limit
        var docCount = await db.TransactionDocuments
            .CountAsync(d => d.TransactionId == transactionId);
        if (docCount >= 10)
            return Results.BadRequest("تم الوصول إلى الحد الأقصى (10 مستندات لكل معاملة)");

        // Validate file
        using var stream = file.OpenReadStream();
        var validationResult = validation.Validate(stream, file.FileName, file.ContentType, file.Length);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.ErrorMessage);

        // Reset stream position after validation
        stream.Position = 0;

        // Upload file
        var result = await storage.UploadAsync(stream, file.FileName, file.ContentType);

        // Calculate version: if same filename exists, increment version
        var fileName = Path.GetFileName(file.FileName);
        var maxVersion = await db.TransactionDocuments
            .Where(d => d.TransactionId == transactionId && d.OriginalFileName == fileName)
            .MaxAsync(d => (int?)d.Version) ?? 0;

        // Save document record
        var document = new TransactionDocument
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            OriginalFileName = fileName,
            StoragePath = result.StoragePath,
            FileSizeBytes = result.FileSizeBytes,
            Checksum = result.Checksum,
            ContentType = file.ContentType,
            DocumentType = DocumentType.Attachment,
            Version = maxVersion + 1,
            UploadedByUserId = Guid.Parse(userId),
            UploadedAt = DateTime.UtcNow
        };

        db.TransactionDocuments.Add(document);
        await db.SaveChangesAsync();

        return Results.Ok(new { document.Id, document.OriginalFileName, document.FileSizeBytes });
    }).RequireAuthorization().DisableAntiforgery();

    // Download document
    app.MapGet("/api/documents/{id:guid}/download", async (
        Guid id,
        IAppDbContext db,
        IFileStorageService storage) =>
    {
        var doc = await db.TransactionDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        var stream = await storage.DownloadAsync(doc.StoragePath);
        return Results.File(stream, doc.ContentType, doc.OriginalFileName);
    }).RequireAuthorization();

    // Preview document (inline PDF)
    app.MapGet("/api/documents/{id:guid}/preview", async (
        Guid id,
        IAppDbContext db,
        IFileStorageService storage) =>
    {
        var doc = await db.TransactionDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        var stream = await storage.DownloadAsync(doc.StoragePath);
        return Results.File(stream, "application/pdf");
    }).RequireAuthorization();

    // Delete document
    app.MapDelete("/api/documents/{id:guid}", async (
        Guid id,
        IAppDbContext db,
        IFileStorageService storage) =>
    {
        var doc = await db.TransactionDocuments.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        await storage.DeleteAsync(doc.StoragePath);
        db.TransactionDocuments.Remove(doc);
        await db.SaveChangesAsync();

        return Results.Ok();
    }).RequireAuthorization();

    // Export branch report to Excel
    app.MapGet("/api/reports/branches/export", async (IAppDbContext db) =>
    {
        var branches = await db.Branches.AsNoTracking()
            .Where(b => b.IsActive).OrderBy(b => b.NameAr).ToListAsync();
        var txList = await db.Transactions.AsNoTracking()
            .Select(t => new { t.SenderBranchId, t.ReceiverBranchId, t.Status })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("تقرير الفروع");
        ws.RightToLeft = true;

        // Header
        var headers = new[] { "الفرع", "الصادرة", "الواردة", "الإجمالي", "قيد المراجعة", "مكتملة", "ملغاة" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0, 61, 122);
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 2;
        foreach (var b in branches)
        {
            var outgoing = txList.Count(t => t.SenderBranchId == b.Id);
            var incoming = txList.Count(t => t.ReceiverBranchId == b.Id);
            var pending = txList.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && t.Status == TransactionStatus.PendingReview);
            var completed = txList.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && (t.Status == TransactionStatus.Confirmed || t.Status == TransactionStatus.Archived));
            var cancelled = txList.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && t.Status == TransactionStatus.Cancelled);
            var total = outgoing + incoming;
            if (total == 0) continue;

            ws.Cell(row, 1).Value = b.NameAr;
            ws.Cell(row, 2).Value = outgoing;
            ws.Cell(row, 3).Value = incoming;
            ws.Cell(row, 4).Value = total;
            ws.Cell(row, 5).Value = pending;
            ws.Cell(row, 6).Value = completed;
            ws.Cell(row, 7).Value = cancelled;
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Range(1, 1, row - 1, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(1, 1, row - 1, 7).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        return Results.File(ms,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"تقرير_الفروع_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }).RequireAuthorization();

    // Export branch report to PDF
    app.MapGet("/api/reports/branches/export-pdf", async (IAppDbContext db) =>
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var branches = await db.Branches.AsNoTracking()
            .Where(b => b.IsActive).OrderBy(b => b.NameAr).ToListAsync();
        var txList = await db.Transactions.AsNoTracking()
            .Select(t => new { t.SenderBranchId, t.ReceiverBranchId, t.Status })
            .ToListAsync();

        var reportData = branches.Select(b =>
        {
            var outgoing = txList.Count(t => t.SenderBranchId == b.Id);
            var incoming = txList.Count(t => t.ReceiverBranchId == b.Id);
            var total = outgoing + incoming;
            if (total == 0) return null;
            return new
            {
                Branch = b.NameAr,
                Outgoing = outgoing,
                Incoming = incoming,
                Total = total,
                Pending = txList.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && t.Status == TransactionStatus.PendingReview),
                Completed = txList.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && (t.Status == TransactionStatus.Confirmed || t.Status == TransactionStatus.Archived)),
                Cancelled = txList.Count(t => (t.SenderBranchId == b.Id || t.ReceiverBranchId == b.Id) && t.Status == TransactionStatus.Cancelled)
            };
        }).Where(x => x != null).ToList();

        var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));
                page.ContentFromRightToLeft();

                page.Header().Text("تقرير الفروع — بنك حضرموت")
                    .FontSize(18).Bold().AlignCenter();

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); // الفرع
                        c.RelativeColumn(1); // صادرة
                        c.RelativeColumn(1); // واردة
                        c.RelativeColumn(1); // إجمالي
                        c.RelativeColumn(1); // قيد المراجعة
                        c.RelativeColumn(1); // مكتملة
                        c.RelativeColumn(1); // ملغاة
                    });

                    table.Header(header =>
                    {
                        foreach (var h in new[] { "الفرع", "الصادرة", "الواردة", "الإجمالي", "قيد المراجعة", "مكتملة", "ملغاة" })
                        {
                            header.Cell().Background("#003d7a").Padding(5)
                                .Text(h).FontColor("#ffffff").Bold().AlignCenter();
                        }
                    });

                    foreach (var row in reportData)
                    {
                        table.Cell().BorderBottom(1).BorderColor("#ddd").Padding(4).Text(row!.Branch);
                        table.Cell().BorderBottom(1).BorderColor("#ddd").Padding(4).Text(row.Outgoing.ToString()).AlignCenter();
                        table.Cell().BorderBottom(1).BorderColor("#ddd").Padding(4).Text(row.Incoming.ToString()).AlignCenter();
                        table.Cell().BorderBottom(1).BorderColor("#ddd").Padding(4).Text(row.Total.ToString()).AlignCenter().Bold();
                        table.Cell().BorderBottom(1).BorderColor("#ddd").Padding(4).Text(row.Pending.ToString()).AlignCenter();
                        table.Cell().BorderBottom(1).BorderColor("#ddd").Padding(4).Text(row.Completed.ToString()).AlignCenter();
                        table.Cell().BorderBottom(1).BorderColor("#ddd").Padding(4).Text(row.Cancelled.ToString()).AlignCenter();
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9);
                });
            });
        }).GeneratePdf();

        return Results.File(pdfBytes, "application/pdf", $"تقرير_الفروع_{DateTime.Now:yyyy-MM-dd}.pdf");
    }).RequireAuthorization();

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
