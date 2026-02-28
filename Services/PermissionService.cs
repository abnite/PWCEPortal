using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using System.Security.Claims;

namespace PWCEPortal.Services;

public class PermissionService : IPermissionService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<List<string>> GetRolePermissionsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null) return new List<string>();

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return new List<string>();

        // Direct user-level permission claims
        var userClaims = await _userManager.GetClaimsAsync(user);
        var permissions = userClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        // Add permissions inherited from the user's roles
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            var rolePerms = await GetRolePermissionsAsync(role);
            permissions.AddRange(rolePerms);
        }

        return permissions.Distinct().ToList();
    }

    public async Task AssignPermissionToRoleAsync(string roleName, string permission)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null) return;

        var existing = await _roleManager.GetClaimsAsync(role);
        if (!existing.Any(c => c.Type == Permissions.ClaimType && c.Value == permission))
        {
            await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
        }
    }

    public async Task RemovePermissionFromRoleAsync(string roleName, string permission)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null) return;

        var claims = await _roleManager.GetClaimsAsync(role);
        var target = claims.FirstOrDefault(c => c.Type == Permissions.ClaimType && c.Value == permission);
        if (target is not null)
        {
            await _roleManager.RemoveClaimAsync(role, target);
        }
    }

    public async Task AssignPermissionsToRoleAsync(string roleName, IEnumerable<string> permissions)
    {
        foreach (var permission in permissions)
        {
            await AssignPermissionToRoleAsync(roleName, permission);
        }
    }

    public async Task SyncRolePermissionsAsync(string roleName, IEnumerable<string> permissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null) return;

        var existing = await _roleManager.GetClaimsAsync(role);
        var permissionClaims = existing.Where(c => c.Type == Permissions.ClaimType).ToList();

        // Remove all current permission claims
        foreach (var claim in permissionClaims)
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        // Add the new set
        foreach (var permission in permissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
        }
    }

    public async Task<bool> UserHasPermissionAsync(string userId, string permission)
    {
        var perms = await GetUserPermissionsAsync(userId);
        return perms.Contains(permission);
    }

    public async Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync()
    {
        var result = new Dictionary<string, List<string>>();
        var roles = await _roleManager.Roles.ToListAsync();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            result[role.Name!] = claims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToList();
        }

        return result;
    }
}
