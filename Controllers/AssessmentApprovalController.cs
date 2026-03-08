using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;

namespace PWCEPortal.Controllers;

[Authorize]
public class AssessmentApprovalController : Controller
{
    private readonly IAssessmentApprovalService _service;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AssessmentApprovalController(IAssessmentApprovalService service, PortalDbContext context, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Policy = Permissions.AssessmentApproval.ViewPending)]
    public async Task<IActionResult> Pending()
    {
        var user = await _userManager.GetUserAsync(User);
        var submissions = await _service.GetPendingSubmissionsAsync(user!.Id);
        return View(submissions);
    }

    [Authorize(Policy = Permissions.AssessmentApproval.ViewPending)]
    public async Task<IActionResult> Details(Guid id)
    {
        var submission = await _service.GetSubmissionByIdAsync(id);
        if (submission is null) return NotFound();
        ViewBag.Logs = await _service.GetApprovalLogsAsync(id);

        // Load assessment components for this assignment's course
        var assignmentId = submission.CourseLecturerAssignmentId;
        var course = submission.CourseLecturerAssignment?.Course;

        var allStructures = await _context.AssessmentStructures
            .Include(s => s.Components)
            .Where(s => s.IsDeleted != true)
            .ToListAsync();

        var structure =
            allStructures.FirstOrDefault(s => s.CollegeProgramId == course!.CollegeProgramId && s.ApplicableLevel == course.Level)
            ?? allStructures.FirstOrDefault(s => s.CollegeProgramId == course!.CollegeProgramId && s.ApplicableLevel == null)
            ?? allStructures.FirstOrDefault(s => s.CollegeProgramId == null && s.ApplicableLevel == course!.Level)
            ?? allStructures.FirstOrDefault(s => s.IsDefault)
            ?? allStructures.FirstOrDefault(s => s.CollegeProgramId == null && s.ApplicableLevel == null);

        var components = structure?.Components?
            .Where(c => c.IsDeleted != true)
            .OrderBy(c => c.DisplayOrder)
            .ToList() ?? new();

        // Load all student marks for this assignment
        var marks = await _context.StudentMarks
            .Include(m => m.Student)
            .Include(m => m.AssessmentComponent)
            .Where(m => m.CourseLecturerAssignmentId == assignmentId && m.IsDeleted != true)
            .ToListAsync();

        // Load registered students
        var registrations = await _context.StudentCourseRegistrations
            .Include(r => r.Student)
            .Where(r => r.CourseId == submission.CourseLecturerAssignment!.CourseId
                     && r.SemesterId == submission.CourseLecturerAssignment!.AcademicSemesterId
                     && r.IsRegistered)
            .OrderBy(r => r.Student!.Surname)
            .ToListAsync();

        ViewBag.Components = components;
        ViewBag.Marks = marks;
        ViewBag.Registrations = registrations;

        return View(submission);
    }

    // HOD Actions
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentApproval.HODApprove)]
    public async Task<IActionResult> HODApprove(Guid id, string comments)
    {
        var user = await _userManager.GetUserAsync(User);
        var result = await _service.HODReviewAsync(id, user!.Id, approve: true, comments);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Assessment approved by HOD."
            : "Action failed.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentApproval.HODReject)]
    public async Task<IActionResult> HODReject(Guid id, string comments)
    {
        var user = await _userManager.GetUserAsync(User);
        var result = await _service.HODReviewAsync(id, user!.Id, approve: false, comments);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Assessment rejected. Lecturer notified."
            : "Action failed.";
        return RedirectToAction(nameof(Pending));
    }

    // QA Actions
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentApproval.QAApprove)]
    public async Task<IActionResult> QAApprove(Guid id, string comments)
    {
        var user = await _userManager.GetUserAsync(User);
        var result = await _service.QAReviewAsync(id, user!.Id, approve: true, comments);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Assessment approved by QA."
            : "Action failed.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentApproval.QAFlag)]
    public async Task<IActionResult> QAFlag(Guid id, string comments)
    {
        var user = await _userManager.GetUserAsync(User);
        var result = await _service.QAReviewAsync(id, user!.Id, approve: false, comments);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Assessment flagged for revision."
            : "Action failed.";
        return RedirectToAction(nameof(Pending));
    }

    // Principal Actions
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentApproval.PrincipalApprove)]
    public async Task<IActionResult> PrincipalApprove(Guid id, string comments)
    {
        var user = await _userManager.GetUserAsync(User);
        var result = await _service.PrincipalApproveAsync(id, user!.Id, comments);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Assessment approved by Principal. Results can now be published."
            : "Action failed.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentApproval.UnlockAssessment)]
    public async Task<IActionResult> Unlock(Guid id, string reason)
    {
        var user = await _userManager.GetUserAsync(User);
        var result = await _service.UnlockAssessmentAsync(id, user!.Id, reason);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Assessment unlocked for correction."
            : "Action failed.";
        return RedirectToAction(nameof(Pending));
    }
}
