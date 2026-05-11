using HBMail.Domain.Common;
using HBMail.Domain.Enums;

namespace HBMail.Domain.Entities;

public class Mail : BaseEntity, IHasTimestamps
{
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string? Description { get; set; }

    public MailType Type { get; set; }
    public MailPriority Priority { get; set; }
    public MailStatus Status { get; set; }

    // المرسل (يُحدَّد تلقائياً من فرع/إدارة المُنشئ)
    public Guid SenderUserId { get; set; }
    public Guid? SenderBranchId { get; set; }
    public Guid? SenderDepartmentId { get; set; }

    // المستلم (يُوجَّه لفرع — مع تحديد شخص مستلم بعينه)
    public Guid? ReceiverBranchId { get; set; }
    public Guid? ReceiverDepartmentId { get; set; }
    public Guid? ReceiverUserId { get; set; }

    // الموظف الذي قام فعلياً بتأكيد الاستلام/الرفض
    public Guid? ReceivedByUserId { get; set; }

    // توجيه المدير لموظف (AssignedToStaff)
    public DateTime? AssignedAt { get; set; }            // وقت التوجيه
    public Guid? AssignedByUserId { get; set; }          // المدير الذي وجَّه
    public Guid? OriginalReceiverUserId { get; set; }    // المستلم الأصلي (المدير) قبل التوجيه

    // إشعار المدير التلقائي (عند الإرسال لموظف مباشرة)
    public Guid? ManagerNotifiedUserId { get; set; }

    // ملاحظة الشؤون عند الإرسال
    public string? AdminNote { get; set; }

    // الملاحظات التوثيقية لكل دور — كل واحدة لها معنى محدد
    // ملاحظة من المرسل عند إنشاء البريد (اختيارية)
    public string? SenderNote { get; set; }
    // ملاحظة من المستلم عند تأكيد الاستلام (اختيارية)
    public string? ReceiverNote { get; set; }
    // سبب الرفض من المستلم (إجباري عند الرفض)
    public string? RejectionNote { get; set; }

    // إعدادات يحددها المرسل عند الإنشاء
    public bool RequireReceiverDocument { get; set; }

    // التواريخ
    public DateTime? DueDate { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // تعديل إداري — يُسجَّل عند تعديل البريد من قبل مدير النظام (SuperAdmin)
    public DateTime? AdminEditedAt { get; set; }
    public Guid? AdminEditedByUserId { get; set; }
    public string? AdminEditedByName { get; set; }

    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Branch? SenderBranch { get; set; }
    public Branch? ReceiverBranch { get; set; }
    public Department? SenderDepartment { get; set; }
    public Department? ReceiverDepartment { get; set; }

    public ICollection<MailAttachment> Attachments { get; set; } = [];
    public ICollection<MailHistory> History { get; set; } = [];
    public ICollection<MailNote> Notes { get; set; } = [];
    public ICollection<MailCC> CcRecipients { get; set; } = [];
}
