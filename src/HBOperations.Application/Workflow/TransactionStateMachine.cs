using HBOperations.Domain.Common;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;

namespace HBOperations.Application.Workflow;

public class TransactionStateMachine
{
    private static readonly Dictionary<TransactionStatus, HashSet<TransactionStatus>>
        AllowedTransitions = new()
        {
            [TransactionStatus.Sent]      = [TransactionStatus.Received, TransactionStatus.Rejected],
            [TransactionStatus.Received]  = [TransactionStatus.Archived],
            [TransactionStatus.Rejected]  = [TransactionStatus.Archived],
            [TransactionStatus.Cancelled] = [],
            [TransactionStatus.Archived]  = []
        };

    private static readonly HashSet<string> GlobalViewRoles =
        ["SuperAdmin", "CEO", "ITAdmin", "Auditor", "ComplianceOfficer"];

    private static readonly HashSet<string> ArchiveRoles =
        ["SuperAdmin", "ITAdmin"];

    public bool CanTransition(TransactionStatus from, TransactionStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public IReadOnlyCollection<TransactionStatus> GetAllowedTransitions(TransactionStatus current)
        => AllowedTransitions.TryGetValue(current, out var allowed)
            ? allowed.ToList().AsReadOnly()
            : Array.Empty<TransactionStatus>().AsReadOnly();

    public static bool IsAuthorizedReceiver(Transaction tx, Guid userId, Guid? userBranchId, Guid? userDepartmentId)
    {
        // المُرسِل لا يمكن أن يكون مستلماً لمعاملته
        if (IsSender(tx, userId)) return false;

        // إذا كان هناك مستلم محدد بالاسم، فهو وحده المستلم المعتمد
        if (tx.ReceiverUserId.HasValue)
            return tx.ReceiverUserId == userId;

        // وإلا فأي عضو في الفرع/الإدارة المستلِمة (ما عدا المرسل أعلاه)
        if (tx.ReceiverBranchId.HasValue && userBranchId == tx.ReceiverBranchId) return true;
        if (tx.ReceiverDepartmentId.HasValue && userDepartmentId == tx.ReceiverDepartmentId) return true;
        return false;
    }

    public static bool IsSender(Transaction tx, Guid userId)
        => tx.SenderUserId == userId || tx.CreatedByUserId == userId;

    public static bool HasGlobalAccess(IEnumerable<string> userRoles)
        => userRoles.Any(GlobalViewRoles.Contains);

    public IReadOnlyCollection<TransactionStatus> GetContextualTransitions(
        Transaction tx,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        IEnumerable<string> userRoles)
    {
        var result = new List<TransactionStatus>();
        var roles = userRoles as string[] ?? userRoles.ToArray();

        var isReceiver = IsAuthorizedReceiver(tx, userId, userBranchId, userDepartmentId);
        var isAdmin = roles.Any(ArchiveRoles.Contains);

        if (tx.Status == TransactionStatus.Sent && isReceiver)
        {
            result.Add(TransactionStatus.Received);
            result.Add(TransactionStatus.Rejected);
        }

        if (isAdmin && tx.Status is TransactionStatus.Received or TransactionStatus.Rejected)
            result.Add(TransactionStatus.Archived);

        return result.AsReadOnly();
    }

    public static bool CanCancel(Transaction tx, Guid userId)
        => tx.Status == TransactionStatus.Sent && IsSender(tx, userId);

    public Result TransitionTo(
        Transaction tx,
        TransactionStatus target,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        IEnumerable<string> userRoles,
        string? notes = null,
        string? ipAddress = null,
        DateTime? actualReceivedAt = null)
    {
        if (!CanTransition(tx.Status, target))
            return Result.Failure("لا يمكن الانتقال من الحالة الحالية إلى الحالة المطلوبة");

        var allowed = GetContextualTransitions(tx, userId, userBranchId, userDepartmentId, userRoles);
        if (!allowed.Contains(target))
            return Result.Failure("ليس لديك صلاحية لتنفيذ هذا الإجراء على هذه المعاملة");

        if (target == TransactionStatus.Rejected && string.IsNullOrWhiteSpace(notes))
            return Result.Failure("الرفض يتطلب كتابة سبب");

        // تحقق من تاريخ الاستلام الفعلي عند التأكيد
        DateTime receivedAtUtc = DateTime.UtcNow;
        if (target == TransactionStatus.Received && actualReceivedAt.HasValue)
        {
            var ts = actualReceivedAt.Value;
            // إذا كان Local نحوّله UTC، إذا Unspecified نعتبره Local
            if (ts.Kind == DateTimeKind.Unspecified)
                ts = DateTime.SpecifyKind(ts, DateTimeKind.Local);
            var utc = ts.ToUniversalTime();
            if (utc < tx.SentAt)
                return Result.Failure("تاريخ الاستلام لا يمكن أن يكون قبل تاريخ الإرسال");
            if (utc > DateTime.UtcNow.AddMinutes(5))
                return Result.Failure("تاريخ الاستلام لا يمكن أن يكون في المستقبل");
            receivedAtUtc = utc;
        }

        var oldStatus = tx.Status;
        tx.Status = target;

        switch (target)
        {
            case TransactionStatus.Received:
                tx.ReceivedAt = receivedAtUtc;
                tx.ReceivedByUserId = userId;
                tx.CompletedAt = receivedAtUtc;
                if (!string.IsNullOrWhiteSpace(notes))
                    tx.ReceiverNote = notes.Trim();
                break;
            case TransactionStatus.Rejected:
                tx.ReceivedByUserId = userId;
                tx.CompletedAt = DateTime.UtcNow;
                tx.RejectionNote = notes?.Trim();
                break;
            case TransactionStatus.Archived:
                tx.CompletedAt ??= DateTime.UtcNow;
                break;
        }

        tx.History.Add(new TransactionHistory
        {
            TransactionId = tx.Id,
            FromStatus = oldStatus,
            ToStatus = target,
            Action = GetActionName(target),
            Notes = notes,
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = DateTime.UtcNow
        });

        return Result.Success();
    }

    private static string GetActionName(TransactionStatus to) => to switch
    {
        TransactionStatus.Received  => "تم تأكيد الاستلام",
        TransactionStatus.Rejected  => "تم رفض المعاملة",
        TransactionStatus.Cancelled => "تم إلغاء المعاملة",
        TransactionStatus.Archived  => "تمت الأرشفة",
        _ => to.ToString()
    };
}
