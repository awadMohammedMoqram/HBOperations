using HBMail.Domain.Common;
using HBMail.Domain.Enums;

namespace HBMail.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string TitleAr { get; set; } = default!;
    public string MessageAr { get; set; } = default!;
    public Guid? MailId { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Mail? Mail { get; set; }
}
