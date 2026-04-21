using Microsoft.AspNetCore.Identity;

namespace HBOperations.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullNameAr { get; set; } = default!;
    public string? EmployeeNumber { get; set; }
    public string? JobTitle { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool ForcePasswordChange { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? DescriptionAr { get; set; }

    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
