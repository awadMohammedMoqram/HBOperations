using HBOperations.Domain.Common;
using HBOperations.Domain.Enums;

namespace HBOperations.Domain.Entities;

public class Branch : BaseEntity, IHasTimestamps
{
    public string NameAr { get; set; } = default!;
    public string Code { get; set; } = default!;
    public BranchType BranchType { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? ParentBranchId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Branch? ParentBranch { get; set; }
    public ICollection<Branch> ChildBranches { get; set; } = [];
}
