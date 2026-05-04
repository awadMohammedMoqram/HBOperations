using HBOperations.Domain.Common;
using HBOperations.Domain.Enums;

namespace HBOperations.Domain.Entities;

public class Transaction : BaseEntity, IHasTimestamps
{
    public string ReferenceNumber { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string? Description { get; set; }

    public TransactionType Type { get; set; }
    public TransactionPriority Priority { get; set; }
    public TransactionStatus Status { get; set; }

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

    // الشؤون الإدارية — عند انتقال Sent → InTransit
    public DateTime? PickedUpAt { get; set; }         // وقت استلام الشؤون من المرسل
    public Guid? PickedUpByUserId { get; set; }       // موظف الشؤون الذي أكّد الاستلام
    public string? AdminNote { get; set; }            // ملاحظة من الشؤون عند الاستلام
    public bool IsSelfDelivery { get; set; } = true;  // هل المُوصِّل = موظف الشؤون المُؤكِّد؟
    public string? CourierName { get; set; }           // اسم المُوصِّل (مطلوب إذا IsSelfDelivery = false)

    // الملاحظات التوثيقية لكل دور — كل واحدة لها معنى محدد
    // ملاحظة من المرسل عند إنشاء المعاملة (اختيارية)
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

    // تعديل إداري — يُسجَّل عند تعديل المعاملة من قبل مدير النظام (SuperAdmin)
    public DateTime? AdminEditedAt { get; set; }
    public Guid? AdminEditedByUserId { get; set; }
    public string? AdminEditedByName { get; set; }

    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Branch? SenderBranch { get; set; }
    public Branch? ReceiverBranch { get; set; }
    public Department? SenderDepartment { get; set; }
    public Department? ReceiverDepartment { get; set; }

    public ICollection<TransactionDocument> Documents { get; set; } = [];
    public ICollection<TransactionHistory> History { get; set; } = [];
    public ICollection<TransactionNote> Notes { get; set; } = [];
}
