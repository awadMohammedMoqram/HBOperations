namespace HBMail.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string? UserName { get; }
    string? FullName { get; }
    Guid? BranchId { get; }
    Guid? DepartmentId { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}
