using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.ViewModel.MarksEntry;

namespace PWCEPortal.Controllers;

[Authorize]
public class MarksEntryController : Controller
{
    private readonly IMarksEntryService _service;
    private readonly IAssessmentStructureService _structureService;
    private readonly IAssessmentApprovalService _approvalService;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MarksEntryController(
        IMarksEntryService service,
        IAssessmentStructureService structureService,
        IAssessmentApprovalService approvalService,
        PortalDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _structureService = structureService;
        _approvalService = approvalService;
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Policy = Permissions.MarksEntry.ViewOwn)]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var activeSemester = await _context.AcademicSemesters
            .Include(s => s.AcademicYear)
            .FirstOrDefaultAsync(s => s.IsRegistrationActive);

        if (activeSemester is null)
        {
            ViewBag.Message = "No active semester found.";
            return View(new List<CourseLecturerAssignment>());
        }

        ViewBag.Semester = activeSemester;
        var assignments = await _service.GetAssignmentsForLecturerAsync(user!.Id, activeSemester.Id);
        return View(assignments);
    }

    [Authorize(Policy = Permissions.MarksEntry.EnterMarks)]
    public async Task<IActionResult> EnterMarks(Guid assignmentId)
    {
        var assignment = await _context.CourseLecturerAssignments
            .Include(a => a.Course).ThenInclude(c => c!.CollegeProgram)
            .Include(a => a.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.IsDeleted != true);

        if (assignment is null) return NotFound();

        // Get assessment components for this course
        var structures = await _context.AssessmentStructures
            .Include(s => s.Components)
            .Where(s => (s.CollegeProgramId == assignment.Course!.CollegeProgramId || s.CollegeProgramId == null)
                     && (s.ApplicableLevel == null || s.ApplicableLevel == assignment.Course!.Level)
                     && s.IsDeleted != true)
            .FirstOrDefaultAsync();

        var components = structures?.Components?.Where(c => c.IsDeleted != true)
            .OrderBy(c => c.DisplayOrder).ToList() ?? new List<AssessmentComponent>();

        // Get registered students
        var registrations = await _context.StudentCourseRegistrations
            .Include(r => r.Student)
            .Where(r => r.CourseId == assignment.CourseId
                     && r.SemesterId == assignment.AcademicSemesterId
                     && r.IsRegistered)
            .ToListAsync();

        var marks = await _service.GetMarksForAssignmentAsync(assignmentId);
        var submission = await _service.GetSubmissionForAssignmentAsync(assignmentId);

        var vm = new MarkSheetViewModel
        {
            Assignment = assignment,
            Components = components,
            Submission = submission,
            StudentRows = registrations.Select(reg => new StudentMarkRowViewModel
            {
                Student = reg.Student!,
                ComponentScores = components.ToDictionary(
                    c => c.Id,
                    c => marks.FirstOrDefault(m => m.StudentId == reg.StudentId && m.AssessmentComponentId == c.Id)?.Score)
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MarksEntry.EnterMarks)]
    public async Task<IActionResult> SaveMarks(SaveMarksViewModel model)
    {
        var markList = model.Marks.Select(m => new StudentMark
        {
            CourseLecturerAssignmentId = model.AssignmentId,
            StudentId = m.StudentId,
            AssessmentComponentId = m.ComponentId,
            Score = m.Score,
            Remarks = m.Remarks
        }).ToList();

        await _service.SaveMarksAsync(markList);
        TempData["SuccessMessage"] = "Marks saved successfully.";
        return RedirectToAction(nameof(EnterMarks), new { assignmentId = model.AssignmentId });
    }

    [Authorize(Policy = Permissions.MarksEntry.UploadBulk)]
    public async Task<IActionResult> BulkUpload(Guid assignmentId)
    {
        var assignment = await _context.CourseLecturerAssignments
            .Include(a => a.Course)
            .Include(a => a.AcademicSemester)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment is null) return NotFound();
        ViewBag.Assignment = assignment;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MarksEntry.UploadBulk)]
    public async Task<IActionResult> BulkUpload(Guid assignmentId, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid Excel file.";
            return RedirectToAction(nameof(BulkUpload), new { assignmentId });
        }

        using var stream = file.OpenReadStream();
        var (saved, errors, errorMessages) = await _service.BulkUploadMarksFromExcelAsync(assignmentId, stream);

        TempData["SuccessMessage"] = $"Bulk upload complete: {saved} marks saved, {errors} errors.";
        if (errorMessages.Any())
            TempData["ErrorMessage"] = string.Join("; ", errorMessages.Take(10));

        return RedirectToAction(nameof(EnterMarks), new { assignmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MarksEntry.SubmitForReview)]
    public async Task<IActionResult> SubmitForReview(Guid assignmentId)
    {
        var user = await _userManager.GetUserAsync(User);
        var result = await _approvalService.SubmitForReviewAsync(assignmentId, user!.Id);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Assessment submitted for HOD review."
            : "Failed to submit. Please ensure all marks are entered.";
        return RedirectToAction(nameof(EnterMarks), new { assignmentId });
    }
}
