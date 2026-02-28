using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Staff;
using PWCEPortal.ViewModel.Appraisal;

namespace PWCEPortal.Controllers;

[Authorize]
public class TeachingAppraisalController : Controller
{
    private readonly ITeachingAppraisalService _service;
    private readonly ICourseLecturerService _lecturerService;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeachingAppraisalController(
        ITeachingAppraisalService service,
        ICourseLecturerService lecturerService,
        PortalDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _lecturerService = lecturerService;
        _context = context;
        _userManager = userManager;
    }

    // ── Templates ────────────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.TeachingAppraisal.ConfigureTemplate)]
    public async Task<IActionResult> Templates()
    {
        var templates = await _service.GetTeachingTemplatesAsync();
        return View(templates);
    }

    [Authorize(Policy = Permissions.TeachingAppraisal.ConfigureTemplate)]
    public IActionResult CreateTemplate()
    {
        return View(new AppraisalTemplateViewModel
        {
            Criteria = GetDefaultCriteria()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.TeachingAppraisal.ConfigureTemplate)]
    public async Task<IActionResult> CreateTemplate(AppraisalTemplateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var template = new AppraisalTemplate
        {
            TemplateName = model.TemplateName,
            Description = model.Description,
            TemplateType = AppraisalTemplateType.Teaching,
            IsActive = model.IsActive
        };

        var criteria = model.Criteria.Select((c, idx) => new AppraisalCriterion
        {
            CriterionName = c.CriterionName,
            Description = c.Description,
            MaxScore = c.MaxScore,
            DisplayOrder = idx + 1
        }).ToList();

        await _service.CreateTemplateAsync(template, criteria);
        TempData["SuccessMessage"] = "Appraisal template created.";
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Policy = Permissions.TeachingAppraisal.ConfigureTemplate)]
    public async Task<IActionResult> EditTemplate(Guid id)
    {
        var template = await _service.GetTemplateByIdAsync(id);
        if (template is null) return NotFound();

        var vm = new AppraisalTemplateViewModel
        {
            Id = template.Id,
            TemplateName = template.TemplateName,
            Description = template.Description,
            IsActive = template.IsActive,
            Criteria = template.Criteria?.Select(c => new CriterionInputViewModel
            {
                CriterionName = c.CriterionName,
                Description = c.Description,
                MaxScore = c.MaxScore,
                DisplayOrder = c.DisplayOrder
            }).OrderBy(c => c.DisplayOrder).ToList() ?? new()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.TeachingAppraisal.ConfigureTemplate)]
    public async Task<IActionResult> EditTemplate(Guid id, AppraisalTemplateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var template = await _service.GetTemplateByIdAsync(id);
        if (template is null) return NotFound();

        template.TemplateName = model.TemplateName;
        template.Description = model.Description;
        template.IsActive = model.IsActive;

        var criteria = model.Criteria.Select((c, idx) => new AppraisalCriterion
        {
            CriterionName = c.CriterionName,
            Description = c.Description,
            MaxScore = c.MaxScore,
            DisplayOrder = idx + 1
        }).ToList();

        await _service.UpdateTemplateAsync(template, criteria);
        TempData["SuccessMessage"] = "Template updated.";
        return RedirectToAction(nameof(Templates));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.TeachingAppraisal.ConfigureTemplate)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        await _service.DeleteTemplateAsync(id);
        TempData["SuccessMessage"] = "Template deleted.";
        return RedirectToAction(nameof(Templates));
    }

    // ── Appraisals ───────────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.TeachingAppraisal.ViewAll)]
    public async Task<IActionResult> Index(Guid? academicYearId)
    {
        var years = await _context.AcademicYears.Where(y => y.IsDeleted != true).OrderByDescending(y => y.Year).ToListAsync();
        ViewBag.AcademicYears = years;
        ViewBag.SelectedYearId = academicYearId;
        var appraisals = await _service.GetAppraisalsAsync(academicYearId: academicYearId);
        return View(appraisals);
    }

    [Authorize(Policy = Permissions.TeachingAppraisal.ViewOwn)]
    public async Task<IActionResult> MyAppraisals()
    {
        var user = await _userManager.GetUserAsync(User);
        var lecturer = await _lecturerService.GetLecturerByUserIdAsync(user!.Id);
        if (lecturer is null)
        {
            TempData["ErrorMessage"] = "Lecturer profile not found.";
            return RedirectToAction("Index", "Home");
        }
        var appraisals = await _service.GetAppraisalsAsync(lecturerId: lecturer.Id);
        return View(appraisals);
    }

    [Authorize(Policy = Permissions.TeachingAppraisal.ConductAsHOD)]
    public async Task<IActionResult> StartAppraisal()
    {
        var years = await _context.AcademicYears.Where(y => y.IsDeleted != true).OrderByDescending(y => y.Year).ToListAsync();
        var vm = new StartAppraisalViewModel
        {
            Templates = await _service.GetTeachingTemplatesAsync(),
            Lecturers = await _lecturerService.GetAllLecturersAsync(),
            AcademicYears = years
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.TeachingAppraisal.ConductAsHOD)]
    public async Task<IActionResult> StartAppraisal(StartAppraisalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Templates = await _service.GetTeachingTemplatesAsync();
            model.Lecturers = await _lecturerService.GetAllLecturersAsync();
            model.AcademicYears = await _context.AcademicYears.Where(y => y.IsDeleted != true).ToListAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        var appraisal = await _service.StartAppraisalAsync(model.TemplateId, model.LecturerId, user!.Id, model.AcademicYearId);
        TempData["SuccessMessage"] = "Appraisal started. You can now fill in the scores.";
        return RedirectToAction(nameof(ConductAppraisal), new { id = appraisal.Id });
    }

    [Authorize(Policy = Permissions.TeachingAppraisal.ConductAsHOD)]
    public async Task<IActionResult> ConductAppraisal(Guid id)
    {
        var appraisal = await _service.GetAppraisalByIdAsync(id);
        if (appraisal is null) return NotFound();

        var criteria = appraisal.AppraisalTemplate?.Criteria?
            .Where(c => c.IsDeleted != true).OrderBy(c => c.DisplayOrder).ToList() ?? new();

        var existingScores = appraisal.Scores?.ToDictionary(s => s.AppraisalCriterionId, s => s.Score)
                           ?? new Dictionary<Guid, decimal>();

        var vm = new ConductAppraisalViewModel
        {
            Appraisal = appraisal,
            Criteria = criteria,
            Scores = existingScores,
            Remarks = appraisal.OverallRemarks ?? string.Empty
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.TeachingAppraisal.ConductAsHOD)]
    public async Task<IActionResult> SaveScores(Guid appraisalId, Dictionary<Guid, decimal> scores, string remarks, bool complete)
    {
        await _service.SaveAppraisalScoresAsync(appraisalId, scores, remarks);
        if (complete)
        {
            await _service.CompleteAppraisalAsync(appraisalId);
            TempData["SuccessMessage"] = "Appraisal completed.";
            return RedirectToAction(nameof(Index));
        }
        TempData["SuccessMessage"] = "Scores saved as draft.";
        return RedirectToAction(nameof(ConductAppraisal), new { id = appraisalId });
    }

    private static List<CriterionInputViewModel> GetDefaultCriteria() => new()
    {
        new() { CriterionName = "Lesson Preparation & Delivery",     MaxScore = 20, DisplayOrder = 1 },
        new() { CriterionName = "Subject Knowledge",                  MaxScore = 20, DisplayOrder = 2 },
        new() { CriterionName = "Classroom Management",               MaxScore = 15, DisplayOrder = 3 },
        new() { CriterionName = "Student Engagement",                 MaxScore = 15, DisplayOrder = 4 },
        new() { CriterionName = "Assessment & Feedback",              MaxScore = 15, DisplayOrder = 5 },
        new() { CriterionName = "Punctuality & Professionalism",      MaxScore = 15, DisplayOrder = 6 },
    };
}
