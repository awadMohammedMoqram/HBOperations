namespace HBOperations.Domain.Enums;

/// <summary>
/// مسار سير العمل:
/// Sent ──► InTransit (الشؤون الإدارية تستلم) ──► Received (المستلم يؤكد) ──► Archived
///  │            │
///  │            └──► Rejected (المستلم يرفض)
///  └──► Rejected (الشؤون ترفض — مستندات ناقصة/تالفة)
///  └──► (حذف فعلي — المرسل يلغي قبل استلام الشؤون)
/// </summary>
public enum TransactionStatus
{
    Sent = 0,         // مُرسلة — بانتظار الشؤون الإدارية
    InTransit = 1,    // قيد التوصيل — الشؤون استلمت وأرسلت للمستلم
    Received = 2,     // مُستلمة — المستلم أكّد الاستلام
    Rejected = 3,     // مرفوضة — من الشؤون أو المستلم (نعرف المصدر من History.FromStatus)
    Archived = 4      // مؤرشفة تلقائياً أو يدوياً
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
