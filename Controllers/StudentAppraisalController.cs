using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;

namespace PWCEPortal.Controllers;

/// <summary>
/// Allows students to view and fill in appraisal sessions opened by their HOD
/// for courses they are registered in.
/// </summary>
[Authorize(Policy = Permissions.TeachingAppraisal.StudentAppraise)]
public class StudentAppraisalController : Controller
{
    private readonly IStudentAppraisalService _service;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentAppraisalController(
        IStudentAppraisalService service,
        PortalDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _context = context;
        _userManager = userManager;
    }

    // ── Student: List open sessions for their courses ────────────────────────

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == user!.Email);

        if (student is null)
        {
            TempData["ErrorMessage"] = "Student profile not found.";
            return RedirectToAction("Index", "StudentDashboard");
        }

        var sessions = await _service.GetOpenSessionsForStudentAsync(student.Id);
        ViewBag.StudentId = student.Id;
        return View(sessions);
    }

    // ── Student: Fill in an appraisal session ────────────────────────────────

    public async Task<IActionResult> FillIn(Guid sessionId)
    {
        var user = await _userManager.GetUserAsync(User);
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == user!.Email);
        if (student is null) return NotFound();

        var session = await _service.GetSessionByIdAsync(sessionId);
        if (session is null || !session.IsOpen) return NotFound();

        // Verify student is registered to this course
        var isRegistered = await _context.StudentCourseRegistrations
            .AnyAsync(r => r.StudentId == student.Id
                        && r.CourseId == session.Assignment!.CourseId
                        && r.IsRegistered);
        if (!isRegistered)
        {
            TempData["ErrorMessage"] = "You are not registered to this course.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _service.GetSubmissionAsync(sessionId, student.Id);
        ViewBag.Session = session;
        ViewBag.StudentId = student.Id;
        ViewBag.ExistingScores = existing?.Scores?.ToDictionary(s => s.AppraisalCriterionId, s => s.Score)
                                 ?? new Dictionary<Guid, decimal>();
        ViewBag.ExistingRemarks = existing?.Remarks ?? string.Empty;
        ViewBag.AlreadySubmitted = existing?.IsSubmitted ?? false;

        var criteria = session.AppraisalTemplate?.Criteria?
            .Where(c => c.IsDeleted != true)
            .OrderBy(c => c.DisplayOrder)
            .ToList() ?? new();

        return View(criteria);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        Guid sessionId,
        Dictionary<Guid, decimal> scores,
        string? remarks,
        bool submitFinal)
    {
        var user = await _userManager.GetUserAsync(User);
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == user!.Email);
        if (student is null) return NotFound();

        var session = await _service.GetSessionByIdAsync(sessionId);
        if (session is null || !session.IsOpen)
        {
            TempData["ErrorMessage"] = "This appraisal session is no longer open.";
            return RedirectToAction(nameof(Index));
        }

        await _service.SaveSubmissionAsync(sessionId, student.Id, scores, remarks, submitFinal);

        TempData["SuccessMessage"] = submitFinal
            ? "Appraisal submitted successfully. Thank you for your feedback."
            : "Draft saved. You can return to complete it before the session closes.";

        return RedirectToAction(nameof(Index));
    }
}
