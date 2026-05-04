using HBOperations.Domain.Common;
using HBOperations.Domain.Entities;
using HBOperations.Domain.Enums;

namespace HBOperations.Application.Workflow;

public class TransactionStateMachine
{
    /// <summary>
    /// الانتقالات المسموحة في Workflow الجديد:
    /// Sent → InTransit (الشؤون تستلم) | Rejected (الشؤون ترفض)
    /// InTransit → Received (المستلم يؤكد) | Rejected (المستلم يرفض)
    /// Received/Rejected → Archived (Admin/System)
    /// </summary>
    private static readonly Dictionary<TransactionStatus, HashSet<TransactionStatus>>
        AllowedTransitions = new()
        {
            [TransactionStatus.Sent]      = [TransactionStatus.InTransit, TransactionStatus.Rejected],
            [TransactionStatus.InTransit] = [TransactionStatus.Received, TransactionStatus.Rejected],
            [TransactionStatus.Received]  = [TransactionStatus.Archived],
            [TransactionStatus.Rejected]  = [TransactionStatus.Archived],
            [TransactionStatus.Archived]  = []
        };

    private static readonly HashSet<string> GlobalViewRoles =
        ["SuperAdmin", "CEO", "ITAdmin", "Auditor", "ComplianceOfficer", "ShariahAuditor"];

    private static readonly HashSet<string> ArchiveRoles =
        ["SuperAdmin", "ITAdmin"];

    /// <summary>
    /// كود إدارة الشؤون الإدارية في جدول الإدارات
    /// </summary>
    public const string AdminAffairsDeptCode = "DEP-ADM";

    public bool CanTransition(TransactionStatus from, TransactionStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public IReadOnlyCollection<TransactionStatus> GetAllowedTransitions(TransactionStatus current)
        => AllowedTransitions.TryGetValue(current, out var allowed)
            ? allowed.ToList().AsReadOnly()
            : Array.Empty<TransactionStatus>().AsReadOnly();

    /// <summary>
    /// هل المستخدم ينتمي لإدارة الشؤون الإدارية؟
    /// </summary>
    public static bool IsAdminAffairsStaff(Guid? userDepartmentId, Guid adminAffairsDeptId)
        => userDepartmentId.HasValue && userDepartmentId.Value == adminAffairsDeptId;

    /// <summary>
    /// هل المستخدم هو المستلم المعتمد لهذه المعاملة؟
    /// </summary>
    public static bool IsAuthorizedReceiver(Transaction tx, Guid userId, Guid? userBranchId, Guid? userDepartmentId)
    {
        if (IsSender(tx, userId)) return false;

        if (tx.ReceiverUserId.HasValue)
            return tx.ReceiverUserId == userId;

        if (tx.ReceiverBranchId.HasValue && userBranchId == tx.ReceiverBranchId) return true;
        if (tx.ReceiverDepartmentId.HasValue && userDepartmentId == tx.ReceiverDepartmentId) return true;
        return false;
    }

    public static bool IsSender(Transaction tx, Guid userId)
        => tx.SenderUserId == userId || tx.CreatedByUserId == userId;

    public static bool HasGlobalAccess(IEnumerable<string> userRoles)
        => userRoles.Any(GlobalViewRoles.Contains);

    /// <summary>
    /// الأزرار المسموحة حسب السياق (الحالة + هوية المستخدم + دوره)
    /// </summary>
    public IReadOnlyCollection<TransactionStatus> GetContextualTransitions(
        Transaction tx,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        IEnumerable<string> userRoles,
        Guid adminAffairsDeptId)
    {
        var result = new List<TransactionStatus>();
        var roles = userRoles as string[] ?? userRoles.ToArray();

        var isAdminAffairs = IsAdminAffairsStaff(userDepartmentId, adminAffairsDeptId);
        var isReceiver = IsAuthorizedReceiver(tx, userId, userBranchId, userDepartmentId);
        var isArchiveAdmin = roles.Any(ArchiveRoles.Contains);

        switch (tx.Status)
        {
            // الشؤون الإدارية تستلم أو ترفض
            case TransactionStatus.Sent when isAdminAffairs:
                result.Add(TransactionStatus.InTransit);
                result.Add(TransactionStatus.Rejected);
                break;

            // المستلم يؤكد أو يرفض
            case TransactionStatus.InTransit when isReceiver:
                result.Add(TransactionStatus.Received);
                result.Add(TransactionStatus.Rejected);
                break;
        }

        // الأرشفة — SuperAdmin/ITAdmin فقط
        if (isArchiveAdmin && tx.Status is TransactionStatus.Received or TransactionStatus.Rejected)
            result.Add(TransactionStatus.Archived);

        return result.AsReadOnly();
    }

    /// <summary>
    /// هل يمكن للمرسل حذف/إلغاء المعاملة؟ (حذف فعلي — فقط إذا ما زالت Sent)
    /// </summary>
    public static bool CanDelete(Transaction tx, Guid userId)
        => tx.Status == TransactionStatus.Sent && IsSender(tx, userId);

    /// <summary>
    /// تنفيذ انتقال Sent → InTransit (الشؤون الإدارية)
    /// </summary>
    public Result PickUp(
        Transaction tx,
        Guid userId,
        Guid? userDepartmentId,
        Guid adminAffairsDeptId,
        bool isSelfDelivery,
        string? courierName,
        string? adminNote,
        DateTime? actualPickedUpAt,
        string? ipAddress = null)
    {
        if (tx.Status != TransactionStatus.Sent)
            return Result.Failure("المعاملة ليست في حالة 'مُرسلة'");

        if (!IsAdminAffairsStaff(userDepartmentId, adminAffairsDeptId))
            return Result.Failure("فقط موظفو الشؤون الإدارية يمكنهم استلام المعاملات");

        if (!isSelfDelivery && string.IsNullOrWhiteSpace(courierName))
            return Result.Failure("يجب إدخال اسم المُوصِّل");

        var pickedUpAtUtc = actualPickedUpAt.HasValue
            ? AppTime.FromYemenToUtc(actualPickedUpAt.Value)
            : AppTime.UtcNow;

        if (pickedUpAtUtc < tx.SentAt)
            return Result.Failure("وقت الاستلام لا يمكن أن يكون قبل وقت الإرسال");

        var oldStatus = tx.Status;
        tx.Status = TransactionStatus.InTransit;
        tx.PickedUpAt = pickedUpAtUtc;
        tx.PickedUpByUserId = userId;
        tx.IsSelfDelivery = isSelfDelivery;
        tx.CourierName = isSelfDelivery ? null : courierName?.Trim();
        tx.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();

        tx.History.Add(new TransactionHistory
        {
            TransactionId = tx.Id,
            FromStatus = oldStatus,
            ToStatus = TransactionStatus.InTransit,
            Action = "تم الاستلام من المرسل وبدء التوصيل",
            Notes = adminNote,
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// تنفيذ انتقال InTransit → Received (المستلم يؤكد)
    /// </summary>
    public Result ConfirmReceipt(
        Transaction tx,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        string? receiverNote,
        DateTime? actualReceivedAt,
        string? ipAddress = null)
    {
        if (tx.Status != TransactionStatus.InTransit)
            return Result.Failure("المعاملة ليست في حالة 'قيد التوصيل'");

        if (!IsAuthorizedReceiver(tx, userId, userBranchId, userDepartmentId))
            return Result.Failure("ليس لديك صلاحية تأكيد استلام هذه المعاملة");

        var receivedAtUtc = actualReceivedAt.HasValue
            ? AppTime.FromYemenToUtc(actualReceivedAt.Value)
            : AppTime.UtcNow;

        if (receivedAtUtc < tx.SentAt)
            return Result.Failure("تاريخ الاستلام لا يمكن أن يكون قبل تاريخ الإرسال");

        var oldStatus = tx.Status;
        tx.Status = TransactionStatus.Received;
        tx.ReceivedAt = receivedAtUtc;
        tx.ReceivedByUserId = userId;
        tx.CompletedAt = receivedAtUtc;
        if (!string.IsNullOrWhiteSpace(receiverNote))
            tx.ReceiverNote = receiverNote.Trim();

        tx.History.Add(new TransactionHistory
        {
            TransactionId = tx.Id,
            FromStatus = oldStatus,
            ToStatus = TransactionStatus.Received,
            Action = "تم تأكيد الاستلام",
            Notes = receiverNote,
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// تنفيذ الرفض — من الشؤون (Sent→Rejected) أو من المستلم (InTransit→Rejected)
    /// </summary>
    public Result Reject(
        Transaction tx,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        Guid adminAffairsDeptId,
        string rejectionReason,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return Result.Failure("الرفض يتطلب كتابة سبب");

        bool authorized = false;
        string action;

        switch (tx.Status)
        {
            case TransactionStatus.Sent:
                authorized = IsAdminAffairsStaff(userDepartmentId, adminAffairsDeptId);
                action = "رفض من الشؤون الإدارية";
                break;
            case TransactionStatus.InTransit:
                authorized = IsAuthorizedReceiver(tx, userId, userBranchId, userDepartmentId);
                action = "رفض من المستلم";
                break;
            default:
                return Result.Failure("لا يمكن رفض المعاملة في حالتها الحالية");
        }

        if (!authorized)
            return Result.Failure("ليس لديك صلاحية لرفض هذه المعاملة");

        var oldStatus = tx.Status;
        tx.Status = TransactionStatus.Rejected;
        tx.RejectionNote = rejectionReason.Trim();
        tx.ReceivedByUserId = userId;
        tx.CompletedAt = AppTime.UtcNow;

        tx.History.Add(new TransactionHistory
        {
            TransactionId = tx.Id,
            FromStatus = oldStatus,
            ToStatus = TransactionStatus.Rejected,
            Action = action,
            Notes = rejectionReason.Trim(),
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// أرشفة المعاملة — SuperAdmin/ITAdmin أو النظام تلقائياً
    /// </summary>
    public Result Archive(
        Transaction tx,
        Guid userId,
        IEnumerable<string> userRoles,
        string? ipAddress = null)
    {
        if (tx.Status is not (TransactionStatus.Received or TransactionStatus.Rejected))
            return Result.Failure("لا يمكن أرشفة المعاملة إلا بعد اكتمالها أو رفضها");

        var roles = userRoles as string[] ?? userRoles.ToArray();
        if (!roles.Any(ArchiveRoles.Contains))
            return Result.Failure("ليس لديك صلاحية الأرشفة");

        var oldStatus = tx.Status;
        tx.Status = TransactionStatus.Archived;
        tx.CompletedAt ??= AppTime.UtcNow;

        tx.History.Add(new TransactionHistory
        {
            TransactionId = tx.Id,
            FromStatus = oldStatus,
            ToStatus = TransactionStatus.Archived,
            Action = "تمت الأرشفة",
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }
}
