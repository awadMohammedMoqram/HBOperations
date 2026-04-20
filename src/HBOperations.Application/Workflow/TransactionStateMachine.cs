using HBOperations.Domain.Common;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;

namespace HBOperations.Application.Workflow;

public class TransactionStateMachine
{
    private static readonly Dictionary<TransactionStatus, HashSet<TransactionStatus>>
        AllowedTransitions = new()
        {
            [TransactionStatus.Draft] = [TransactionStatus.PendingReview, TransactionStatus.Cancelled],
            [TransactionStatus.PendingReview] = [TransactionStatus.Approved, TransactionStatus.Returned, TransactionStatus.Cancelled],
            [TransactionStatus.Approved] = [TransactionStatus.InTransit],
            [TransactionStatus.InTransit] = [TransactionStatus.Received, TransactionStatus.Returned],
            [TransactionStatus.Received] = [TransactionStatus.Confirmed, TransactionStatus.Disputed, TransactionStatus.Returned],
            [TransactionStatus.Confirmed] = [TransactionStatus.Archived],
            [TransactionStatus.Returned] = [TransactionStatus.Draft, TransactionStatus.Cancelled],
            [TransactionStatus.Disputed] = [TransactionStatus.Confirmed, TransactionStatus.Returned],
            [TransactionStatus.Cancelled] = [],
            [TransactionStatus.Archived] = []
        };

    public bool CanTransition(TransactionStatus from, TransactionStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public IReadOnlyCollection<TransactionStatus> GetAllowedTransitions(TransactionStatus currentStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed)
            ? allowed.ToList().AsReadOnly()
            : Array.Empty<TransactionStatus>().AsReadOnly();
    }

    public Result TransitionTo(Transaction transaction, TransactionStatus newStatus,
        Guid userId, string? notes = null, string? ipAddress = null)
    {
        if (!CanTransition(transaction.Status, newStatus))
            return Result.Failure($"لا يمكن الانتقال من {transaction.Status} إلى {newStatus}");

        var oldStatus = transaction.Status;
        transaction.Status = newStatus;

        switch (newStatus)
        {
            case TransactionStatus.InTransit:
                transaction.SentAt = DateTime.UtcNow;
                break;
            case TransactionStatus.Received:
                transaction.ReceivedAt = DateTime.UtcNow;
                break;
            case TransactionStatus.Confirmed:
            case TransactionStatus.Archived:
                transaction.CompletedAt = DateTime.UtcNow;
                break;
        }

        transaction.History.Add(new TransactionHistory
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Action = GetActionName(newStatus),
            Notes = notes,
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = DateTime.UtcNow
        });

        return Result.Success();
    }

    private static string GetActionName(TransactionStatus to) => to switch
    {
        TransactionStatus.PendingReview => "تم الإرسال للمراجعة",
        TransactionStatus.Approved => "تم الاعتماد",
        TransactionStatus.InTransit => "تم الإرسال",
        TransactionStatus.Received => "تم الاستلام",
        TransactionStatus.Confirmed => "تم التأكيد",
        TransactionStatus.Returned => "تم الإرجاع",
        TransactionStatus.Disputed => "نزاع",
        TransactionStatus.Cancelled => "تم الإلغاء",
        TransactionStatus.Archived => "تم الأرشفة",
        _ => to.ToString()
    };
}
