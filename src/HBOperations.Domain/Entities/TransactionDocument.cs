using HBOperations.Domain.Common;
using HBOperations.Domain.Enums;

namespace HBOperations.Domain.Entities;

public class TransactionDocument : BaseEntity, IHasTimestamps
{
    public Guid TransactionId { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string Checksum { get; set; } = default!;
    public string ContentType { get; set; } = "application/pdf";
    public DocumentType DocumentType { get; set; }
    public int Version { get; set; } = 1;
    public bool IsRequired { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Transaction Transaction { get; set; } = default!;
}
