using HBMail.Domain.Common;
using HBMail.Domain.Enums;

namespace HBMail.Domain.Entities;

public class MailHistory : BaseEntity
{
    public Guid MailId { get; set; }
    public MailStatus FromStatus { get; set; }
    public MailStatus ToStatus { get; set; }
    public string Action { get; set; } = default!;
    public string? Notes { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime PerformedAt { get; set; }

    // Navigation
    public Mail Mail { get; set; } = default!;
}
