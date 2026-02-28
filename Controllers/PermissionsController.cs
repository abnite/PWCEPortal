using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.ViewModel.Permissions;

namespace PWCEPortal.Controllers;

/// <summary>
/// Allows System Administrators to view and manage the permissions assigned to each role.
/// </summary>
[Authorize(Roles = RoleNames.SystemAdmin)]
public class PermissionsController : Controller
{
    private readonly IPermissionService _permissionService;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public PermissionsController(
        IPermissionService permissionService,
        RoleManager<ApplicationRole> roleManager)
    {
        _permissionService = permissionService;
        _roleManager = roleManager;
    }

    // GET /Permissions
    public async Task<IActionResult> Index()
    {
        var rolePermissions = await _permissionService.GetAllRolePermissionsAsync();

        var phase2Roles = new HashSet<string>
        {
            RoleNames.Lecturer,
            RoleNames.HOD,
            RoleNames.QAOfficer,
            RoleNames.AcademicAffairsOfficer,
            RoleNames.Principal,
            RoleNames.VicePrincipal,
            RoleNames.HROfficer,
        };

        var summaries = new List<RolePermissionsSummaryViewModel>();

        foreach (var role in await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync())
        {
            rolePermissions.TryGetValue(role.Name!, out var perms);
            summaries.Add(new RolePermissionsSummaryViewModel
            {
                RoleName = role.Name!,
                Description = role.Description ?? string.Empty,
                PermissionCount = perms?.Count ?? 0,
                IsPhase2Role = phase2Roles.Contains(role.Name!),
            });
        }

        return View(summaries);
    }

    // GET /Permissions/Edit/{roleName}
    [HttpGet]
    public async Task<IActionResult> Edit(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            TempData["ErrorMessage"] = "Role not found.";
            return RedirectToAction(nameof(Index));
        }

        var grantedPermissions = await _permissionService.GetRolePermissionsAsync(roleName);
        var allPermissions = Permissions.GetAll();

        var groups = BuildPermissionGroups(allPermissions, grantedPermissions);

        var vm = new RolePermissionsViewModel
        {
            RoleName = role.Name!,
            Description = role.Description ?? string.Empty,
            PermissionGroups = groups,
            SelectedPermissions = grantedPermissions,
        };

        return View(vm);
    }

    // POST /Permissions/Edit/{roleName}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string roleName, RolePermissionsViewModel model)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            TempData["ErrorMessage"] = "Role not found.";
            return RedirectToAction(nameof(Index));
        }

        // Sync — replace existing permissions with the submitted selection
        var selected = model.SelectedPermissions ?? new List<string>();
        await _permissionService.SyncRolePermissionsAsync(roleName, selected);

        TempData["SuccessMessage"] = $"Permissions for '{roleName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Permissions/ResetToDefault/{roleName}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetToDefault(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            TempData["ErrorMessage"] = "Role not found.";
            return RedirectToAction(nameof(Index));
        }

        var defaults = Permissions.ForRole(roleName);
        await _permissionService.SyncRolePermissionsAsync(roleName, defaults);

        TempData["SuccessMessage"] = $"Permissions for '{roleName}' reset to defaults.";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<PermissionGroupViewModel> BuildPermissionGroups(
        IReadOnlyList<string> allPermissions,
        List<string> grantedPermissions)
    {
        var granted = new HashSet<string>(grantedPermissions);

        return allPermissions
            .GroupBy(p =>
            {
                // e.g. "Permissions.CourseLecturer.View" → "Course Lecturer"
                var parts = p.Split('.');
                return parts.Length >= 2
                    ? SplitCamelCase(parts[1])
                    : "General";
            })
            .OrderBy(g => g.Key)
            .Select(g => new PermissionGroupViewModel
            {
                GroupName = g.Key,
                Permissions = g.Select(p => new PermissionItemViewModel
                {
                    Value = p,
                    DisplayName = Permissions.DisplayName(p),
                    IsGranted = granted.Contains(p),
                }).ToList(),
            })
            .ToList();
    }

    private static string SplitCamelCase(string input) =>
        System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1").Trim();
}
