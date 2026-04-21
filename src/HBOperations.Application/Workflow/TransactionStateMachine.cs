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
            [TransactionStatus.PendingReview] = [TransactionStatus.Approved, TransactionStatus.PendingSecondApproval, TransactionStatus.Returned, TransactionStatus.Cancelled],
            [TransactionStatus.PendingSecondApproval] = [TransactionStatus.Approved, TransactionStatus.Returned, TransactionStatus.Cancelled],
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

    /// <summary>
    /// Get filtered transitions based on transaction context (e.g., hide PendingSecondApproval for non-critical)
    /// </summary>
    public IReadOnlyCollection<TransactionStatus> GetContextualTransitions(Transaction transaction)
    {
        var transitions = GetAllowedTransitions(transaction.Status).ToList();

        // For PendingReview: if Critical, show PendingSecondApproval instead of Approved
        if (transaction.Status == TransactionStatus.PendingReview)
        {
            if (transaction.Priority == TransactionPriority.Critical)
            {
                transitions.Remove(TransactionStatus.Approved);
            }
            else
            {
                transitions.Remove(TransactionStatus.PendingSecondApproval);
            }
        }

        return transitions.AsReadOnly();
    }

    public Result TransitionTo(Transaction transaction, TransactionStatus newStatus,
        Guid userId, string? notes = null, string? ipAddress = null)
    {
        if (!CanTransition(transaction.Status, newStatus))
            return Result.Failure($"لا يمكن الانتقال من {transaction.Status} إلى {newStatus}");

        // Dual approval for Critical: PendingReview → PendingSecondApproval (first approval)
        if (transaction.Status == TransactionStatus.PendingReview &&
            newStatus == TransactionStatus.PendingSecondApproval)
        {
            transaction.FirstApprovedByUserId = userId;
            transaction.FirstApprovedAt = DateTime.UtcNow;
        }

        // Second approval: PendingSecondApproval → Approved
        if (transaction.Status == TransactionStatus.PendingSecondApproval &&
            newStatus == TransactionStatus.Approved)
        {
            if (userId == transaction.FirstApprovedByUserId)
                return Result.Failure("لا يمكن للمعتمد الأول أن يكون نفسه المعتمد الثاني");

            transaction.SecondApprovedByUserId = userId;
            transaction.SecondApprovedAt = DateTime.UtcNow;
        }

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
        TransactionStatus.PendingSecondApproval => "تم الاعتماد الأول (بانتظار الاعتماد الثاني)",
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
