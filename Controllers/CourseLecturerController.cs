using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.ViewModel.CourseLecturer;

namespace PWCEPortal.Controllers;

[Authorize]
public class CourseLecturerController : Controller
{
    private readonly ICourseLecturerService _service;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CourseLecturerController(ICourseLecturerService service, PortalDbContext context, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _context = context;
        _userManager = userManager;
    }

    // ── Departments ──────────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.CourseLecturer.View)]
    public async Task<IActionResult> Departments()
    {
        var depts = await _service.GetAllDepartmentsAsync();
        return View(depts);
    }

    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public IActionResult CreateDepartment() => View(new DepartmentViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> CreateDepartment(DepartmentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        await _service.CreateDepartmentAsync(new Department { DepartmentName = model.DepartmentName, Description = model.Description });
        TempData["SuccessMessage"] = "Department created successfully.";
        return RedirectToAction(nameof(Departments));
    }

    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> EditDepartment(Guid id)
    {
        var dept = await _service.GetDepartmentByIdAsync(id);
        if (dept is null) return NotFound();
        return View(new DepartmentViewModel { DepartmentName = dept.DepartmentName, Description = dept.Description });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> EditDepartment(Guid id, DepartmentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var dept = await _service.GetDepartmentByIdAsync(id);
        if (dept is null) return NotFound();
        dept.DepartmentName = model.DepartmentName;
        dept.Description = model.Description;
        await _service.UpdateDepartmentAsync(dept);
        TempData["SuccessMessage"] = "Department updated.";
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Remove)]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        await _service.DeleteDepartmentAsync(id);
        TempData["SuccessMessage"] = "Department deleted.";
        return RedirectToAction(nameof(Departments));
    }

    // ── Lecturers ────────────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.CourseLecturer.View)]
    public async Task<IActionResult> Lecturers()
    {
        var lecturers = await _service.GetAllLecturersAsync();
        return View(lecturers);
    }

    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> CreateLecturer()
    {
        ViewBag.Departments = await _service.GetAllDepartmentsAsync();
        ViewBag.Users = await _userManager.Users
            .Where(u => !_context.Lecturers.Any(l => l.UserId == u.Id && l.IsDeleted != true))
            .ToListAsync();
        return View(new LecturerViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> CreateLecturer(LecturerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _service.GetAllDepartmentsAsync();
            ViewBag.Users = await _userManager.Users.ToListAsync();
            return View(model);
        }

        await _service.CreateLecturerAsync(new Lecturer
        {
            UserId = model.UserId,
            DepartmentId = model.DepartmentId,
            StaffId = model.StaffId,
            Qualification = model.Qualification,
            Specialisation = model.Specialisation
        });

        TempData["SuccessMessage"] = "Lecturer profile created.";
        return RedirectToAction(nameof(Lecturers));
    }

    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> EditLecturer(Guid id)
    {
        var lecturer = await _service.GetLecturerByIdAsync(id);
        if (lecturer is null) return NotFound();
        ViewBag.Departments = await _service.GetAllDepartmentsAsync();
        return View(new LecturerViewModel
        {
            UserId = lecturer.UserId,
            DepartmentId = lecturer.DepartmentId,
            StaffId = lecturer.StaffId,
            Qualification = lecturer.Qualification,
            Specialisation = lecturer.Specialisation
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> EditLecturer(Guid id, LecturerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _service.GetAllDepartmentsAsync();
            return View(model);
        }
        var lecturer = await _service.GetLecturerByIdAsync(id);
        if (lecturer is null) return NotFound();
        lecturer.DepartmentId = model.DepartmentId;
        lecturer.StaffId = model.StaffId;
        lecturer.Qualification = model.Qualification;
        lecturer.Specialisation = model.Specialisation;
        await _service.UpdateLecturerAsync(lecturer);
        TempData["SuccessMessage"] = "Lecturer profile updated.";
        return RedirectToAction(nameof(Lecturers));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Remove)]
    public async Task<IActionResult> DeleteLecturer(Guid id)
    {
        await _service.DeleteLecturerAsync(id);
        TempData["SuccessMessage"] = "Lecturer profile removed.";
        return RedirectToAction(nameof(Lecturers));
    }

    // ── Assignments ──────────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.CourseLecturer.View)]
    public async Task<IActionResult> Index(Guid? semesterId, Guid? departmentId)
    {
        var semesters = await _context.AcademicSemesters
            .Include(s => s.AcademicYear)
            .Where(s => s.IsDeleted != true)
            .OrderByDescending(s => s.AcademicYear!.Year)
            .ToListAsync();

        var activeSemester = semesters.FirstOrDefault(s => s.IsRegistrationActive)
                          ?? semesters.FirstOrDefault();

        var selectedSemesterId = semesterId ?? activeSemester?.Id;

        ViewBag.Semesters = semesters;
        ViewBag.SelectedSemesterId = selectedSemesterId;
        ViewBag.Departments = await _service.GetAllDepartmentsAsync();
        ViewBag.SelectedDepartmentId = departmentId;

        var assignments = await _service.GetAssignmentsAsync(selectedSemesterId, departmentId);
        return View(assignments);
    }

    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> Assign()
    {
        var vm = new AssignLecturerViewModel
        {
            Lecturers = await _service.GetAllLecturersAsync(),
            Courses = await _context.Courses.Where(c => c.IsDeleted != true).OrderBy(c => c.CourseName).ToListAsync(),
            Semesters = await _context.AcademicSemesters
                .Include(s => s.AcademicYear)
                .Where(s => s.IsDeleted != true)
                .OrderByDescending(s => s.AcademicYear!.Year)
                .ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Assign)]
    public async Task<IActionResult> Assign(AssignLecturerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Lecturers = await _service.GetAllLecturersAsync();
            model.Courses = await _context.Courses.Where(c => c.IsDeleted != true).OrderBy(c => c.CourseName).ToListAsync();
            model.Semesters = await _context.AcademicSemesters.Include(s => s.AcademicYear).Where(s => s.IsDeleted != true).ToListAsync();
            return View(model);
        }

        if (await _service.AssignmentExistsAsync(model.CourseId, model.AcademicSemesterId))
        {
            TempData["ErrorMessage"] = "A lecturer is already assigned to this course for the selected semester.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _service.AssignLecturerAsync(model.LecturerId, model.CourseId, model.AcademicSemesterId);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Lecturer assigned successfully."
            : "Failed to assign lecturer. The course may already be assigned for this semester.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.CourseLecturer.Remove)]
    public async Task<IActionResult> Remove(Guid id)
    {
        await _service.RemoveAssignmentAsync(id);
        TempData["SuccessMessage"] = "Assignment removed.";
        return RedirectToAction(nameof(Index));
    }
}
