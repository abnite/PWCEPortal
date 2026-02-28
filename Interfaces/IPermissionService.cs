namespace PWCEPortal.Interfaces;

/// <summary>
/// Provides operations for managing fine-grained permissions assigned to roles and users.
/// Permissions are stored as ASP.NET Identity role claims of type <c>"Permission"</c>.
/// </summary>
public interface IPermissionService
{
    /// <summary>Returns all permission values currently assigned to the given role.</summary>
    Task<List<string>> GetRolePermissionsAsync(string roleName);

    /// <summary>
    /// Returns the effective permissions for a user — the union of their direct user claims
    /// and the claims of every role they belong to.
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(string userId);

    /// <summary>Grants a single permission to a role (idempotent — safe to call multiple times).</summary>
    Task AssignPermissionToRoleAsync(string roleName, string permission);

    /// <summary>Removes a single permission from a role.</summary>
    Task RemovePermissionFromRoleAsync(string roleName, string permission);

    /// <summary>Grants a collection of permissions to a role.</summary>
    Task AssignPermissionsToRoleAsync(string roleName, IEnumerable<string> permissions);

    /// <summary>
    /// Replaces the full set of permissions on a role with the supplied collection.
    /// Any existing permissions not in the new list are removed.
    /// </summary>
    Task SyncRolePermissionsAsync(string roleName, IEnumerable<string> permissions);

    /// <summary>Returns true if the user's effective permissions include the specified permission.</summary>
    Task<bool> UserHasPermissionAsync(string userId, string permission);

    /// <summary>Returns a dictionary of role name → list of assigned permissions for all roles.</summary>
    Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync();
}
