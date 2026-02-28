using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;

namespace PWCEPortal.Controllers;

[Authorize]
public class AssessmentApprovalController : Controller
{
    private readonly IAssessmentApprovalService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public AssessmentApprovalController(IAssessmentApprovalService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
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
