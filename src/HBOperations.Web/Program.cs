using HBOperations.Application;
using HBOperations.Application.Common.Interfaces;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;
using HBOperations.Infrastructure;
using HBOperations.Infrastructure.Data.Seed;
using HBOperations.Web.Components;
using HBOperations.Web.Hubs;
using HBOperations.Web.Services;
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
    builder.Services.AddScoped<IRealTimeNotifier, SignalRNotifier>();
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

        // Save document record
        var document = new TransactionDocument
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoragePath = result.StoragePath,
            FileSizeBytes = result.FileSizeBytes,
            Checksum = result.Checksum,
            ContentType = file.ContentType,
            DocumentType = DocumentType.Attachment,
            Version = docCount + 1,
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
