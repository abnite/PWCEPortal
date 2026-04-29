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

        // Fee gate: student must have paid ≥ 70% of overall fees
        var hasPaid = await _gpaService.HasSufficientFeePaymentAsync(student.Id);
        if (!hasPaid)
            return View("ResultsFeeLocked");

        var semesterResults = await _gpaService.GetStudentSemesterResultsAsync(student.Id);

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.IsActive);
        CumulativeResult? cumulative = null;
        if (activeYear != null)
            cumulative = await _gpaService.GetCumulativeResultAsync(student.Id, activeYear.Id);

        var courseResults = await _gpaService.GetCourseResultsBySemesterAsync(student.Id);
        var interimResults = await _gpaService.GetInterimCourseResultsAsync(student.Id);

        var vm = new StudentResultsViewModel
        {
            Student = student,
            SemesterResults = semesterResults,
            CumulativeResult = cumulative,
            CourseResultsBySemester = courseResults,
            InterimResults = interimResults
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

        var pending = await _gpaService.GetPendingSubmissionsForSemesterAsync(selectedId.Value);

        var vm = new ResultsPublishViewModel
        {
            Semester = semester!,
            Results = results,
            PublishedCount = results.Count(r => r.IsPublished),
            WithheldCount = results.Count(r => r.IsWithheld),
            PendingSubmissions = pending
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

    // ── Interim publish (per course assignment) ──────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.StudentResults.Publish)]
    public async Task<IActionResult> PublishInterim(Guid assignmentId, Guid semesterId)
    {
        var result = await _gpaService.PublishInterimAsync(assignmentId);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Provisional results published. Fee-paying students can now view these marks."
            : "Could not publish. Ensure the submission is Principal-Approved.";
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

    // ── Un-withhold individual result ────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.StudentResults.Withhold)]
    public async Task<IActionResult> UnWithhold(Guid studentId, Guid semesterId)
    {
        await _gpaService.UnWithholdResultAsync(studentId, semesterId);
        TempData["SuccessMessage"] = "Result un-withheld and re-published.";
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

        var courseResults = await _gpaService.GetCourseResultsBySemesterAsync(studentId);

        var vm = new StudentResultsViewModel
        {
            Student = student,
            SemesterResults = semesterResults,
            CumulativeResult = cumulative,
            CourseResultsBySemester = courseResults
        };
        return View("MyResults", vm);
    }
}
