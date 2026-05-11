using HBMail.Domain.Common;

namespace HBMail.Domain.Entities;

public class Department : BaseEntity, IHasTimestamps
{
    public string NameAr { get; set; } = default!;
    public string Code { get; set; } = default!;
    public Guid? ManagerId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> ChildDepartments { get; set; } = [];
}
