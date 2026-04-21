using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HBOperations.Infrastructure.Identity;

public class CustomClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim("FullNameAr", user.FullNameAr ?? ""));

        if (user.BranchId.HasValue)
            identity.AddClaim(new Claim("BranchId", user.BranchId.Value.ToString()));

        if (user.DepartmentId.HasValue)
            identity.AddClaim(new Claim("DepartmentId", user.DepartmentId.Value.ToString()));

        if (!string.IsNullOrEmpty(user.JobTitle))
            identity.AddClaim(new Claim("JobTitle", user.JobTitle));

        return identity;
    }
}
