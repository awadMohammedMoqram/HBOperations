using HBOperations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBOperations.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.ReferenceNumber).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.SenderNote).HasMaxLength(1000);
        builder.Property(t => t.ReceiverNote).HasMaxLength(1000);
        builder.Property(t => t.RejectionNote).HasMaxLength(1000);

        builder.HasIndex(t => t.ReferenceNumber).IsUnique();
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => new { t.SenderBranchId, t.Status });
        builder.HasIndex(t => new { t.ReceiverBranchId, t.Status });
        builder.HasIndex(t => new { t.SenderUserId, t.Status });
        builder.HasIndex(t => new { t.ReceiverUserId, t.Status });
        builder.HasIndex(t => new { t.ReceiverDepartmentId, t.Status });
        builder.HasIndex(t => new { t.Status, t.CreatedAt });

        builder.HasOne(t => t.SenderBranch)
            .WithMany()
            .HasForeignKey(t => t.SenderBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ReceiverBranch)
            .WithMany()
            .HasForeignKey(t => t.ReceiverBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.SenderDepartment)
            .WithMany()
            .HasForeignKey(t => t.SenderDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ReceiverDepartment)
            .WithMany()
            .HasForeignKey(t => t.ReceiverDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Documents)
            .WithOne(d => d.Transaction)
            .HasForeignKey(d => d.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.History)
            .WithOne(h => h.Transaction)
            .HasForeignKey(h => h.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Notes)
            .WithOne(n => n.Transaction)
            .HasForeignKey(n => n.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TransactionDocumentConfiguration : IEntityTypeConfiguration<TransactionDocument>
{
    public void Configure(EntityTypeBuilder<TransactionDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Checksum).HasMaxLength(128).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100);

        builder.HasIndex(d => d.TransactionId);
        builder.HasIndex(d => d.UploadedAt);
        builder.HasIndex(d => d.IsArchived);
    }
}

public class TransactionHistoryConfiguration : IEntityTypeConfiguration<TransactionHistory>
{
    public void Configure(EntityTypeBuilder<TransactionHistory> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Action).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Notes).HasMaxLength(1000);
        builder.Property(h => h.IpAddress).HasMaxLength(45);

        builder.HasIndex(h => h.TransactionId);
        builder.HasIndex(h => h.PerformedAt);
    }
}

public class TransactionNoteConfiguration : IEntityTypeConfiguration<TransactionNote>
{
    public void Configure(EntityTypeBuilder<TransactionNote> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Content).HasMaxLength(2000).IsRequired();

        builder.HasIndex(n => n.TransactionId);
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.Phone).HasMaxLength(20);

        builder.HasIndex(b => b.Code).IsUnique();

        builder.HasOne(b => b.ParentBranch)
            .WithMany(b => b.ChildBranches)
            .HasForeignKey(b => b.ParentBranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();

        builder.HasIndex(d => d.Code).IsUnique();

        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.TitleAr).HasMaxLength(200).IsRequired();
        builder.Property(n => n.MessageAr).HasMaxLength(1000).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        builder.HasOne(n => n.Transaction)
            .WithMany()
            .HasForeignKey(n => n.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(200).IsRequired();
        builder.Property(a => a.UserName).HasMaxLength(200);
        builder.Property(a => a.IpAddress).HasMaxLength(45);

        builder.HasIndex(a => a.EntityName);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.Action, a.Timestamp });
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(2000).IsRequired();
        builder.Property(s => s.DescriptionAr).HasMaxLength(500);
        builder.Property(s => s.Category).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ValueType).HasMaxLength(20).IsRequired();

        builder.HasIndex(s => s.Key).IsUnique();
        builder.HasIndex(s => s.Category);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<HBOperations.Infrastructure.Identity.ApplicationUser>
{
    public void Configure(EntityTypeBuilder<HBOperations.Infrastructure.Identity.ApplicationUser> builder)
    {
        builder.Property(u => u.FullNameAr).HasMaxLength(200).IsRequired();
        builder.Property(u => u.EmployeeNumber).HasMaxLength(20);
        builder.Property(u => u.JobTitle).HasMaxLength(200);
    }
}
