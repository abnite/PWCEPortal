using Microsoft.AspNetCore.Authorization;

namespace PWCEPortal.ApplicationClass;

/// <summary>
/// Evaluates <see cref="PermissionRequirement"/> by checking whether the authenticated user's
/// claims include a claim of type <c>"Permission"</c> whose value matches the required permission.
/// Role claims are included automatically by ASP.NET Core Identity when the role has been
/// assigned that permission claim via <c>RoleManager.AddClaimAsync</c>.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims
            .Any(c => c.Type == Permissions.ClaimType && c.Value == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
