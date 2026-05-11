using HBMail.Domain.Common;
using HBMail.Domain.Entities;
using HBMail.Domain.Enums;

namespace HBMail.Application.Workflow;

public class MailStateMachine
{
    /// <summary>
    /// الانتقالات المسموحة في نظام البريد الداخلي:
    /// Sent → Received (المستلم يستلم) | AssignedToStaff (المدير يوجّه) | Rejected (المستلم يرفض)
    /// AssignedToStaff → Received (الموظف يستلم) | Rejected (الموظف يرفض)
    /// Received/Rejected → Archived
    /// </summary>
    private static readonly Dictionary<MailStatus, HashSet<MailStatus>>
        AllowedTransitions = new()
        {
            [MailStatus.Sent]            = [MailStatus.Received, MailStatus.AssignedToStaff, MailStatus.Rejected],
            [MailStatus.AssignedToStaff] = [MailStatus.Received, MailStatus.Rejected],
            [MailStatus.Received]        = [MailStatus.Archived],
            [MailStatus.Rejected]        = [MailStatus.Archived],
            [MailStatus.Archived]        = []
        };

    private static readonly HashSet<string> GlobalViewRoles =
        ["SuperAdmin", "CEO", "ITAdmin", "Auditor", "ComplianceOfficer", "ShariahAuditor"];

    private static readonly HashSet<string> ArchiveRoles =
        ["SuperAdmin", "ITAdmin"];

    private static readonly HashSet<string> ManagerRoles =
        ["DepartmentManager", "BranchManager", "OfficeManager"];

    public const string AdminAffairsDeptCode = "DEP-ADM";

    public bool CanTransition(MailStatus from, MailStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public IReadOnlyCollection<MailStatus> GetAllowedTransitions(MailStatus current)
        => AllowedTransitions.TryGetValue(current, out var allowed)
            ? allowed.ToList().AsReadOnly()
            : Array.Empty<MailStatus>().AsReadOnly();

    // ── Role helpers ──────────────────────────────────────────────────

    public static bool IsAdminAffairsStaff(Guid? userDepartmentId, Guid adminAffairsDeptId)
        => userDepartmentId.HasValue && userDepartmentId.Value == adminAffairsDeptId;

    public static bool IsManager(IEnumerable<string> userRoles)
        => userRoles.Any(ManagerRoles.Contains);

    public static bool IsSender(Mail mail, Guid userId)
        => mail.SenderUserId == userId || mail.CreatedByUserId == userId;

    public static bool HasGlobalAccess(IEnumerable<string> userRoles)
        => userRoles.Any(GlobalViewRoles.Contains);

    /// <summary>
    /// هل المستخدم هو المستلم الحالي المعتمد لهذا البريد؟
    /// </summary>
    public static bool IsAuthorizedReceiver(Mail mail, Guid userId, Guid? userBranchId, Guid? userDepartmentId)
    {
        if (IsSender(mail, userId)) return false;
        if (mail.ReceiverUserId.HasValue)
            return mail.ReceiverUserId == userId;
        return false;
    }

    /// <summary>
    /// هل المستخدم يملك حق القراءة فقط (CC أو مدير أُشعر تلقائياً أو مدير سابق)؟
    /// </summary>
    public static bool CanViewReadOnly(Mail mail, Guid userId)
    {
        if (mail.ManagerNotifiedUserId.HasValue && mail.ManagerNotifiedUserId == userId)
            return true;
        if (mail.OriginalReceiverUserId.HasValue && mail.OriginalReceiverUserId == userId)
            return true;
        return false;
    }

    // ── Contextual transitions ──────────────────────────────────────

    /// <summary>
    /// الأزرار المسموحة حسب السياق (الحالة + هوية المستخدم + دوره)
    /// </summary>
    public IReadOnlyCollection<MailStatus> GetContextualTransitions(
        Mail mail,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        IEnumerable<string> userRoles,
        Guid adminAffairsDeptId)
    {
        var result = new List<MailStatus>();
        var roles = userRoles as string[] ?? userRoles.ToArray();

        var isReceiver = IsAuthorizedReceiver(mail, userId, userBranchId, userDepartmentId);
        var isArchiveAdmin = roles.Any(ArchiveRoles.Contains);
        var isManager = IsManager(roles);

        switch (mail.Status)
        {
            case MailStatus.Sent when isReceiver:
                result.Add(MailStatus.Received);
                if (isManager)
                    result.Add(MailStatus.AssignedToStaff);
                result.Add(MailStatus.Rejected);
                break;

            case MailStatus.AssignedToStaff when isReceiver:
                result.Add(MailStatus.Received);
                result.Add(MailStatus.Rejected);
                break;
        }

        if (isArchiveAdmin && mail.Status is MailStatus.Received or MailStatus.Rejected)
            result.Add(MailStatus.Archived);

        return result.AsReadOnly();
    }

    public static bool CanDelete(Mail mail, Guid userId)
        => mail.Status == MailStatus.Sent && IsSender(mail, userId);

    // ── State transition methods ──────────────────────────────────────

    /// <summary>
    /// توجيه البريد من مدير لموظف في إدارته (Sent → AssignedToStaff)
    /// </summary>
    public Result AssignToStaff(
        Mail mail,
        Guid managerId,
        Guid staffUserId,
        string? note,
        string? ipAddress = null)
    {
        if (mail.Status != MailStatus.Sent)
            return Result.Failure("البريد ليس في حالة 'مُرسل'");

        if (mail.ReceiverUserId != managerId)
            return Result.Failure("ليس لديك صلاحية توجيه هذا البريد");

        if (staffUserId == managerId)
            return Result.Failure("لا يمكن توجيه البريد لنفسك");

        var oldStatus = mail.Status;
        mail.OriginalReceiverUserId = managerId;
        mail.ReceiverUserId = staffUserId;
        mail.AssignedByUserId = managerId;
        mail.AssignedAt = AppTime.UtcNow;
        mail.Status = MailStatus.AssignedToStaff;

        mail.History.Add(new MailHistory
        {
            MailId = mail.Id,
            FromStatus = oldStatus,
            ToStatus = MailStatus.AssignedToStaff,
            Action = "تم توجيه البريد لموظف",
            Notes = note,
            PerformedByUserId = managerId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// تأكيد الاستلام (Sent → Received أو AssignedToStaff → Received)
    /// </summary>
    public Result ConfirmReceipt(
        Mail mail,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        string? receiverNote,
        DateTime? actualReceivedAt,
        string? ipAddress = null)
    {
        if (mail.Status is not (MailStatus.Sent or MailStatus.AssignedToStaff))
            return Result.Failure("البريد ليس في حالة تسمح بالاستلام");

        if (!IsAuthorizedReceiver(mail, userId, userBranchId, userDepartmentId))
            return Result.Failure("ليس لديك صلاحية تأكيد استلام هذا البريد");

        var receivedAtUtc = actualReceivedAt.HasValue
            ? AppTime.FromYemenToUtc(actualReceivedAt.Value)
            : AppTime.UtcNow;

        if (receivedAtUtc < mail.SentAt)
            return Result.Failure("تاريخ الاستلام لا يمكن أن يكون قبل تاريخ الإرسال");

        var oldStatus = mail.Status;
        mail.Status = MailStatus.Received;
        mail.ReceivedAt = receivedAtUtc;
        mail.ReceivedByUserId = userId;
        mail.CompletedAt = receivedAtUtc;
        if (!string.IsNullOrWhiteSpace(receiverNote))
            mail.ReceiverNote = receiverNote.Trim();

        mail.History.Add(new MailHistory
        {
            MailId = mail.Id,
            FromStatus = oldStatus,
            ToStatus = MailStatus.Received,
            Action = "تم تأكيد الاستلام",
            Notes = receiverNote,
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// رفض/إرجاع البريد (Sent → Rejected أو AssignedToStaff → Rejected)
    /// </summary>
    public Result Reject(
        Mail mail,
        Guid userId,
        Guid? userBranchId,
        Guid? userDepartmentId,
        string rejectionReason,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return Result.Failure("الرفض يتطلب كتابة سبب");

        if (mail.Status is not (MailStatus.Sent or MailStatus.AssignedToStaff))
            return Result.Failure("لا يمكن رفض البريد في حالته الحالية");

        if (!IsAuthorizedReceiver(mail, userId, userBranchId, userDepartmentId))
            return Result.Failure("ليس لديك صلاحية لرفض هذا البريد");

        var oldStatus = mail.Status;
        mail.Status = MailStatus.Rejected;
        mail.RejectionNote = rejectionReason.Trim();
        mail.ReceivedByUserId = userId;
        mail.CompletedAt = AppTime.UtcNow;

        mail.History.Add(new MailHistory
        {
            MailId = mail.Id,
            FromStatus = oldStatus,
            ToStatus = MailStatus.Rejected,
            Action = "تم رفض/إرجاع البريد",
            Notes = rejectionReason.Trim(),
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// أرشفة البريد — SuperAdmin/ITAdmin أو النظام تلقائياً
    /// </summary>
    public Result Archive(
        Mail mail,
        Guid userId,
        IEnumerable<string> userRoles,
        string? ipAddress = null)
    {
        if (mail.Status is not (MailStatus.Received or MailStatus.Rejected))
            return Result.Failure("لا يمكن أرشفة البريد إلا بعد اكتماله أو رفضه");

        var roles = userRoles as string[] ?? userRoles.ToArray();
        if (!roles.Any(ArchiveRoles.Contains))
            return Result.Failure("ليس لديك صلاحية الأرشفة");

        var oldStatus = mail.Status;
        mail.Status = MailStatus.Archived;
        mail.CompletedAt ??= AppTime.UtcNow;

        mail.History.Add(new MailHistory
        {
            MailId = mail.Id,
            FromStatus = oldStatus,
            ToStatus = MailStatus.Archived,
            Action = "تمت الأرشفة",
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// إلغاء الأرشفة — SuperAdmin/ITAdmin فقط
    /// </summary>
    public Result Restore(
        Mail mail,
        MailStatus restoreTo,
        Guid userId,
        IEnumerable<string> userRoles,
        string? ipAddress = null)
    {
        if (mail.Status != MailStatus.Archived)
            return Result.Failure("البريد ليس مؤرشفاً");

        var roles = userRoles as string[] ?? userRoles.ToArray();
        if (!roles.Any(ArchiveRoles.Contains))
            return Result.Failure("ليس لديك صلاحية إلغاء الأرشفة");

        if (restoreTo is not (MailStatus.Received or MailStatus.Rejected))
            return Result.Failure("حالة الاستعادة غير صالحة");

        mail.Status = restoreTo;

        mail.History.Add(new MailHistory
        {
            MailId = mail.Id,
            FromStatus = MailStatus.Archived,
            ToStatus = restoreTo,
            Action = "تم إلغاء الأرشفة",
            PerformedByUserId = userId,
            IpAddress = ipAddress,
            PerformedAt = AppTime.UtcNow
        });

        return Result.Success();
    }
}
