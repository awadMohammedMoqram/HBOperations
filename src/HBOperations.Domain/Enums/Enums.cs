namespace HBOperations.Domain.Enums;

/// <summary>
/// مسار سير العمل المبسّط:
/// Sent  ──►  Received   (المستلم يؤكد) ──► Archived (تلقائياً)
///   │   ──►  Rejected   (المستلم يرفض) — حالة نهائية لا تقبل أي تغيير
///   │   ──►  (Cancel)   (المرسل يلغي قبل الاستلام) ──► حذف نهائي للمعاملة
/// </summary>
public enum TransactionStatus
{
    Sent = 0,         // أُرسلت من المرسل، بانتظار تأكيد الاستلام
    Received = 1,     // المستلم أكّد الاستلام (نهائية)
    Rejected = 2,     // المستلم رفض المعاملة مع سبب (نهائية)
    Cancelled = 3,    // (متروكة للتوافق فقط — الإلغاء الجديد يحذف المعاملة)
    Archived = 4      // مؤرشفة تلقائياً بعد فترة من Received/Rejected
}

public enum TransactionType
{
    DocumentDelivery = 0,   // تسليم مستندات
    CashTransfer = 1,       // تحويل نقدي
    InternalDepartment = 2, // داخلي بين الإدارات
    BranchToBranch = 3,     // بين الفروع
    Other = 4               // أخرى
}

public enum TransactionPriority
{
    Normal = 0,
    Important = 1,
    Urgent = 2
}

public enum DocumentType
{
    Attachment = 0,       // مرفق عام يُضيفه المرسل
    ProofOfReceipt = 1    // إثبات استلام يرفعه المستلم عند التأكيد
}

public enum BranchType
{
    HeadOffice = 0,
    MainBranch = 1,
    Branch = 2,
    Office = 3
}

public enum NotificationType
{
    TransactionCreated = 0,
    TransactionStatusChanged = 1,
    TransactionAssigned = 2,
    DocumentUploaded = 3,
    TransactionOverdue = 4,
    SystemAlert = 5
}
