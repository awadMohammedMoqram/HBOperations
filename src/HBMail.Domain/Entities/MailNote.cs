using HBMail.Domain.Common;

namespace HBMail.Domain.Entities;

public class MailNote : BaseEntity
{
    public Guid MailId { get; set; }
    public string Content { get; set; } = default!;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Mail Mail { get; set; } = default!;
}
