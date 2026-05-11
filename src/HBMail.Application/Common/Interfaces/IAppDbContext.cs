using HBMail.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HBMail.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Mail> Mails { get; }
    DbSet<MailAttachment> MailAttachments { get; }
    DbSet<MailHistory> MailHistories { get; }
    DbSet<MailNote> MailNotes { get; }
    DbSet<MailCC> MailCCs { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Department> Departments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
