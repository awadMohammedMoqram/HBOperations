using HBMail.Domain.Common;

namespace HBMail.Domain.Entities;

public class MailCC : BaseEntity
{
    public Guid MailId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AddedAt { get; set; }

    // Navigation
    public Mail Mail { get; set; } = default!;
}
