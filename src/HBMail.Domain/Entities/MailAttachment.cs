using HBMail.Domain.Common;
using HBMail.Domain.Enums;

namespace HBMail.Domain.Entities;

public class MailAttachment : BaseEntity, IHasTimestamps
{
    public Guid MailId { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string Checksum { get; set; } = default!;
    public string ContentType { get; set; } = "application/pdf";
    public AttachmentType AttachmentType { get; set; }
    public int Version { get; set; } = 1;
    public bool IsRequired { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Mail Mail { get; set; } = default!;
}
