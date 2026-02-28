using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.ViewModel.Results;

namespace PWCEPortal.Controllers;

[Authorize]
public class StudentResultsController : Controller
{
    private readonly IGPAService _gpaService;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentResultsController(IGPAService gpaService, PortalDbContext context, UserManager<ApplicationUser> userManager)
    {
        _gpaService = gpaService;
        _context = context;
        _userManager = userManager;
    }

    // ── Student: View own results ────────────────────────────────────────────

    [Authorize(Policy = Permissions.StudentResults.ViewOwn)]
    public async Task<IActionResult> MyResults()
    {
        var user = await _userManager.GetUserAsync(User);
        var student = await _context.Students
            .Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Email == user!.Email);

        if (student is null)
        {
            TempData["ErrorMessage"] = "Student profile not found.";
            return RedirectToAction("Index", "StudentDashboard");
        }

        var semesterResults = await _gpaService.GetStudentSemesterResultsAsync(student.Id);

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.IsActive);
        CumulativeResult? cumulative = null;
        if (activeYear != null)
            cumulative = await _gpaService.GetCumulativeResultAsync(student.Id, activeYear.Id);

        var vm = new StudentResultsViewModel
        {
            Student = student,
            SemesterResults = semesterResults,
            CumulativeResult = cumulative
        };

        return View(vm);
    }

    // ── Admin/HOD: View all results ──────────────────────────────────────────

    [Authorize(Policy = Permissions.StudentResults.ViewAll)]
    public async Task<IActionResult> AllResults(Guid? semesterId)
    {
        var semesters = await _context.AcademicSemesters
            .Include(s => s.AcademicYear)
            .Where(s => s.IsDeleted != true)
            .OrderByDescending(s => s.AcademicYear!.Year)
            .ToListAsync();

        var activeSemester = semesters.FirstOrDefault(s => s.IsRegistrationActive) ?? semesters.FirstOrDefault();
        var selectedId = semesterId ?? activeSemester?.Id;

        ViewBag.Semesters = semesters;
        ViewBag.SelectedSemesterId = selectedId;

        if (selectedId is null)
        {
            ViewBag.Semester = null;
            return View(new ResultsPublishViewModel());
        }

        var semester = await _context.AcademicSemesters
            .Include(s => s.AcademicYear)
            .FirstOrDefaultAsync(s => s.Id == selectedId);

        var results = await _context.SemesterResults
            .Include(r => r.Student)
            .Where(r => r.AcademicSemesterId == selectedId)
            .OrderBy(r => r.Student!.Surname)
            .ToListAsync();

        var vm = new ResultsPublishViewModel
        {
            Semester = semester!,
            Results = results,
            PublishedCount = results.Count(r => r.IsPublished),
            WithheldCount = results.Count(r => r.IsWithheld)
        };

        return View(vm);
    }

    // ── Compute results ──────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.StudentResults.ViewAll)]
    public async Task<IActionResult> Compute(Guid semesterId)
    {
        var result = await _gpaService.ComputeSemesterResultsAsync(semesterId);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Semester results computed successfully."
            : "Failed to compute results. Ensure a default grading scale is configured.";
        return RedirectToAction(nameof(AllResults), new { semesterId });
    }

    // ── Publish results ──────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.StudentResults.Publish)]
    public async Task<IActionResult> Publish(Guid semesterId)
    {
        var result = await _gpaService.PublishResultsAsync(semesterId);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Results published successfully."
            : "No results found to publish.";
        return RedirectToAction(nameof(AllResults), new { semesterId });
    }

    // ── Withhold individual result ────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.StudentResults.Withhold)]
    public async Task<IActionResult> Withhold(Guid studentId, Guid semesterId, string reason)
    {
        await _gpaService.WithholdResultAsync(studentId, semesterId, reason);
        TempData["SuccessMessage"] = "Result withheld.";
        return RedirectToAction(nameof(AllResults), new { semesterId });
    }

    // ── View student's result detail ─────────────────────────────────────────

    [Authorize(Policy = Permissions.StudentResults.ViewAll)]
    public async Task<IActionResult> StudentDetail(Guid studentId)
    {
        var student = await _context.Students.Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null) return NotFound();

        var semesterResults = await _gpaService.GetStudentSemesterResultsAsync(studentId);
        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.IsActive);
        CumulativeResult? cumulative = null;
        if (activeYear != null)
            cumulative = await _gpaService.GetCumulativeResultAsync(studentId, activeYear.Id);

        var vm = new StudentResultsViewModel
        {
            Student = student,
            SemesterResults = semesterResults,
            CumulativeResult = cumulative
        };
        return View("MyResults", vm);
    }
}
