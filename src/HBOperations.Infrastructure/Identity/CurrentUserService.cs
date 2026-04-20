using System.Security.Claims;
using HBOperations.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace HBOperations.Infrastructure.Identity;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var id = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return id is not null ? Guid.Parse(id) : Guid.Empty;
        }
    }

    public string? UserName => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

    public string? FullName => httpContextAccessor.HttpContext?.User.FindFirst("FullNameAr")?.Value;

    public Guid? BranchId
    {
        get
        {
            var val = httpContextAccessor.HttpContext?.User.FindFirst("BranchId")?.Value;
            return val is not null ? Guid.Parse(val) : null;
        }
    }

    public Guid? DepartmentId
    {
        get
        {
            var val = httpContextAccessor.HttpContext?.User.FindFirst("DepartmentId")?.Value;
            return val is not null ? Guid.Parse(val) : null;
        }
    }

    public IEnumerable<string> Roles =>
        httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
