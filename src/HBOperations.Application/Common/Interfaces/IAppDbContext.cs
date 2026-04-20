using HBOperations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HBOperations.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Transaction> Transactions { get; }
    DbSet<TransactionDocument> TransactionDocuments { get; }
    DbSet<TransactionHistory> TransactionHistories { get; }
    DbSet<TransactionNote> TransactionNotes { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Department> Departments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
