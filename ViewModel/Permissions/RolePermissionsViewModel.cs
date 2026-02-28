namespace PWCEPortal.ViewModel.Permissions;

/// <summary>
/// Represents the full permissions configuration for a single role,
/// organised into logical permission groups for display in the admin UI.
/// </summary>
public class RolePermissionsViewModel
{
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Permissions selected by the administrator (bound from form POST).</summary>
    public List<string> SelectedPermissions { get; set; } = new();

    /// <summary>All permissions organised by group, used to render the edit form.</summary>
    public List<PermissionGroupViewModel> PermissionGroups { get; set; } = new();
}

public class PermissionGroupViewModel
{
    public string GroupName { get; set; } = string.Empty;
    public List<PermissionItemViewModel> Permissions { get; set; } = new();
}

public class PermissionItemViewModel
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}

/// <summary>Summary row shown on the Permissions index page.</summary>
public class RolePermissionsSummaryViewModel
{
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
    public bool IsPhase2Role { get; set; }
}
