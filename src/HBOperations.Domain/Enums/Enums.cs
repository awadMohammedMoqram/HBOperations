namespace HBOperations.Domain.Enums;

public enum TransactionStatus
{
    Draft = 0,
    PendingReview = 1,
    Approved = 2,
    InTransit = 3,
    Received = 4,
    Confirmed = 5,
    Returned = 6,
    Disputed = 7,
    Cancelled = 8,
    Archived = 9
}

public enum TransactionType
{
    OutgoingFromHQ = 0,
    IncomingToHQ = 1,
    BranchToBranch = 2,
    InternalDepartment = 3,
    DocumentDelivery = 4,
    CashTransfer = 5,
    ReturnedItem = 6,
    AuditRequest = 7
}

public enum TransactionPriority
{
    Normal = 0,
    Important = 1,
    Urgent = 2,
    Critical = 3
}

public enum DocumentType
{
    Original = 0,
    Receipt = 1,
    Approval = 2,
    ReturnReason = 3,
    DisputeEvidence = 4,
    Confirmation = 5,
    Attachment = 6
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
