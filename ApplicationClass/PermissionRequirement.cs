using Microsoft.AspNetCore.Authorization;

namespace PWCEPortal.ApplicationClass;

/// <summary>
/// Represents an authorization requirement that a user must hold a specific permission claim.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
