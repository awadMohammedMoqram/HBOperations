using HBOperations.Application.Common.Interfaces;
using HBOperations.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HBOperations.Web.Services;

public class AutoArchiveService(IServiceProvider serviceProvider, ILogger<AutoArchiveService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Auto-archive service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunArchiveAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error in auto-archive service");
            }

            // Run every 6 hours
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    private async Task RunArchiveAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var settingService = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var enabled = await settingService.GetBoolAsync("AutoArchive.Enabled", true);
        if (!enabled)
        {
            logger.LogDebug("Auto-archive is disabled");
            return;
        }

        var daysAfter = await settingService.GetIntAsync("AutoArchive.DaysAfterCompletion", 90);
        var cutoffDate = DateTime.UtcNow.AddDays(-daysAfter);

        // Archive completed transactions older than cutoff
        var toArchive = await db.Transactions
            .Where(t => (t.Status == TransactionStatus.Received || t.Status == TransactionStatus.Rejected)
                        && t.CompletedAt.HasValue
                        && t.CompletedAt.Value < cutoffDate)
            .ToListAsync(ct);

        if (toArchive.Count == 0)
        {
            logger.LogDebug("No transactions to archive");
            return;
        }

        foreach (var tx in toArchive)
        {
            tx.Status = TransactionStatus.Archived;
        }

        // Also archive related documents
        var txIds = toArchive.Select(t => t.Id).ToHashSet();
        var docs = await db.TransactionDocuments
            .Where(d => txIds.Contains(d.TransactionId) && !d.IsArchived)
            .ToListAsync(ct);

        foreach (var doc in docs)
        {
            doc.IsArchived = true;
        }

        var count = await db.SaveChangesAsync(ct);
        logger.LogInformation("Auto-archived {Count} transactions ({Docs} documents), cutoff: {Days} days",
            toArchive.Count, docs.Count, daysAfter);
    }
}
