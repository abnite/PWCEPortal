using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Staff;
using PWCEPortal.ViewModel.NonTeachingStaff;

namespace PWCEPortal.Controllers;

[Authorize]
public class NonTeachingStaffController : Controller
{
    private readonly INonTeachingStaffService _service;
    private readonly ITeachingAppraisalService _templateService;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NonTeachingStaffController(
        INonTeachingStaffService service,
        ITeachingAppraisalService templateService,
        PortalDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _templateService = templateService;
        _context = context;
        _userManager = userManager;
    }

    // ── Staff Records ────────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.NonTeachingStaff.View)]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        List<Models.Staff.NonTeachingStaff> staff;

        // Unit Head sees only their own department's staff; HR/System Admin sees all
        if (user!.DepartmentId.HasValue && !User.IsInRole(RoleNames.SystemAdmin) && !User.IsInRole(RoleNames.HROfficer))
            staff = await _service.GetStaffByDepartmentAsync(user.DepartmentId.Value);
        else
            staff = await _service.GetAllStaffAsync();

        return View(staff);
    }

    [Authorize(Policy = Permissions.NonTeachingStaff.Manage)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _context.Departments.Where(d => d.IsDeleted != true).OrderBy(d => d.DepartmentName).ToListAsync();
        return View(new NonTeachingStaffFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.NonTeachingStaff.Manage)]
    public async Task<IActionResult> Create(NonTeachingStaffFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsDeleted != true).OrderBy(d => d.DepartmentName).ToListAsync();
            return View(model);
        }

        await _service.CreateStaffAsync(new Models.Staff.NonTeachingStaff
        {
            StaffId = model.StaffId,
            FullName = model.FullName,
            Position = model.Position,
            Department = model.Department,
            DepartmentId = model.DepartmentId,
            Email = model.Email,
            PhoneNo = model.PhoneNo,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            DateEmployed = model.DateEmployed,
            Qualification = model.Qualification,
            IsActive = model.IsActive
        });

        TempData["SuccessMessage"] = "Staff member added.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.NonTeachingStaff.Manage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var staff = await _service.GetStaffByIdAsync(id);
        if (staff is null) return NotFound();

        ViewBag.Departments = await _context.Departments.Where(d => d.IsDeleted != true).OrderBy(d => d.DepartmentName).ToListAsync();
        return View(new NonTeachingStaffFormViewModel
        {
            Id = staff.Id,
            StaffId = staff.StaffId,
            FullName = staff.FullName,
            Position = staff.Position,
            Department = staff.Department,
            DepartmentId = staff.DepartmentId,
            Email = staff.Email,
            PhoneNo = staff.PhoneNo,
            Gender = staff.Gender,
            DateOfBirth = staff.DateOfBirth,
            DateEmployed = staff.DateEmployed,
            Qualification = staff.Qualification,
            IsActive = staff.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.NonTeachingStaff.Manage)]
    public async Task<IActionResult> Edit(Guid id, NonTeachingStaffFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsDeleted != true).OrderBy(d => d.DepartmentName).ToListAsync();
            return View(model);
        }

        var staff = await _service.GetStaffByIdAsync(id);
        if (staff is null) return NotFound();

        staff.StaffId = model.StaffId;
        staff.FullName = model.FullName;
        staff.Position = model.Position;
        staff.Department = model.Department;
        staff.DepartmentId = model.DepartmentId;
        staff.Email = model.Email;
        staff.PhoneNo = model.PhoneNo;
        staff.Gender = model.Gender;
        staff.DateOfBirth = model.DateOfBirth;
        staff.DateEmployed = model.DateEmployed;
        staff.Qualification = model.Qualification;
        staff.IsActive = model.IsActive;

        await _service.UpdateStaffAsync(staff);
        TempData["SuccessMessage"] = "Staff record updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.NonTeachingStaff.Manage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteStaffAsync(id);
        TempData["SuccessMessage"] = "Staff member removed.";
        return RedirectToAction(nameof(Index));
    }

    // ── Appraisals ───────────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.NonTeachingStaff.ViewReports)]
    public async Task<IActionResult> Appraisals(Guid? staffId, Guid? academicYearId)
    {
        var user = await _userManager.GetUserAsync(User);
        bool isUnitHeadScoped = user!.DepartmentId.HasValue && !User.IsInRole(RoleNames.SystemAdmin) && !User.IsInRole(RoleNames.HROfficer);

        // Unit Head sees only staff in their department
        var staffList = isUnitHeadScoped
            ? await _service.GetStaffByDepartmentAsync(user.DepartmentId!.Value)
            : await _service.GetAllStaffAsync();

        ViewBag.StaffList = staffList;
        ViewBag.AcademicYears = await _context.AcademicYears.Where(y => y.IsDeleted != true).OrderByDescending(y => y.Year).ToListAsync();
        ViewBag.SelectedStaffId = staffId;
        ViewBag.SelectedYearId = academicYearId;

        // If scoped and a staffId was given, verify it belongs to the dept
        Guid? effectiveStaffId = staffId;
        if (isUnitHeadScoped && staffId.HasValue && !staffList.Any(s => s.Id == staffId.Value))
            effectiveStaffId = null;

        var appraisals = await _service.GetAppraisalsAsync(effectiveStaffId, academicYearId);
        return View(appraisals);
    }

    [Authorize(Policy = Permissions.NonTeachingStaff.Appraise)]
    public async Task<IActionResult> StartAppraisal()
    {
        var user = await _userManager.GetUserAsync(User);
        bool isUnitHeadScoped = user!.DepartmentId.HasValue && !User.IsInRole(RoleNames.SystemAdmin) && !User.IsInRole(RoleNames.HROfficer);

        var ntTemplates = await _context.AppraisalTemplates
            .Where(t => t.TemplateType == AppraisalTemplateType.NonTeaching && t.IsActive && t.IsDeleted != true)
            .ToListAsync();

        // Unit Head only appraises staff in their department
        var staffList = isUnitHeadScoped
            ? await _service.GetStaffByDepartmentAsync(user.DepartmentId!.Value)
            : await _service.GetAllStaffAsync();

        var vm = new StartNonTeachingAppraisalViewModel
        {
            Templates = ntTemplates,
            StaffList = staffList,
            AcademicYears = await _context.AcademicYears.Where(y => y.IsDeleted != true).OrderByDescending(y => y.Year).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.NonTeachingStaff.Appraise)]
    public async Task<IActionResult> StartAppraisal(StartNonTeachingAppraisalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Templates = await _context.AppraisalTemplates
                .Where(t => t.TemplateType == AppraisalTemplateType.NonTeaching && t.IsActive && t.IsDeleted != true)
                .ToListAsync();
            model.StaffList = await _service.GetAllStaffAsync();
            model.AcademicYears = await _context.AcademicYears.Where(y => y.IsDeleted != true).ToListAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        var appraisal = await _service.StartAppraisalAsync(model.TemplateId, model.StaffId, user!.Id, model.AcademicYearId);
        TempData["SuccessMessage"] = "Appraisal started.";
        return RedirectToAction(nameof(ConductAppraisal), new { id = appraisal.Id });
    }

    [Authorize(Policy = Permissions.NonTeachingStaff.Appraise)]
    public async Task<IActionResult> ConductAppraisal(Guid id)
    {
        var appraisal = await _service.GetAppraisalByIdAsync(id);
        if (appraisal is null) return NotFound();

        var criteria = appraisal.AppraisalTemplate?.Criteria?
            .Where(c => c.IsDeleted != true).OrderBy(c => c.DisplayOrder).ToList() ?? new();

        var existingScores = appraisal.Scores?.ToDictionary(s => s.AppraisalCriterionId, s => s.Score)
                           ?? new Dictionary<Guid, decimal>();

        var vm = new ConductNonTeachingAppraisalViewModel
        {
            Appraisal = appraisal,
            Criteria = criteria,
            Scores = existingScores,
            Remarks = appraisal.OverallRemarks ?? string.Empty
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.NonTeachingStaff.Appraise)]
    public async Task<IActionResult> SaveScores(Guid appraisalId, Dictionary<Guid, decimal> scores, string remarks, bool complete)
    {
        await _service.SaveAppraisalScoresAsync(appraisalId, scores, remarks);
        if (complete)
        {
            await _service.CompleteAppraisalAsync(appraisalId);
            TempData["SuccessMessage"] = "Appraisal completed.";
            return RedirectToAction(nameof(Appraisals));
        }
        TempData["SuccessMessage"] = "Scores saved.";
        return RedirectToAction(nameof(ConductAppraisal), new { id = appraisalId });
    }
}
