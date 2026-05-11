namespace HBMail.Domain.Enums;

/// <summary>
/// مسار سير العمل (نظام البريد الداخلي):
/// Sent ──► Received (المستلم يستلم بنفسه — مدير أو موظف)
///  │   ──► AssignedToStaff (المدير يوجّه لموظف لديه)
///  │   ──► Rejected (المستلم يرفض/يُرجع البريد)
///
/// AssignedToStaff ──► Received (الموظف يستلم)
///                ──► Rejected (الموظف يرفض)
///
/// Received/Rejected ──► Archived
/// </summary>
public enum MailStatus
{
    Sent = 0,              // مُرسل — الشؤون أرسلت البريد
    AssignedToStaff = 1,   // موجَّه لموظف — المدير وجَّه الاستلام لموظف لديه
    Received = 2,          // مُستلم — المستلم النهائي أكّد الاستلام
    Rejected = 3,          // مرفوض/مُرجَع — المستلم أرجع البريد
    Archived = 4           // مؤرشف تلقائياً أو يدوياً
}

public enum MailType
{
    DocumentDelivery = 0,   // تسليم مرفقات
    CashTransfer = 1,       // تحويل نقدي
    InternalDepartment = 2, // داخلي بين الإدارات
    BranchToBranch = 3,     // بين الفروع
    Other = 4               // أخرى
}

public enum MailPriority
{
    Normal = 0,
    Important = 1,
    Urgent = 2
}

public enum AttachmentType
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
    MailCreated = 0,
    MailStatusChanged = 1,
    MailAssigned = 2,
    DocumentUploaded = 3,
    MailOverdue = 4,
    SystemAlert = 5
}
