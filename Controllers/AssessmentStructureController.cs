using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.ViewModel.Assessment;

namespace PWCEPortal.Controllers;

[Authorize]
public class AssessmentStructureController : Controller
{
    private readonly IAssessmentStructureService _service;
    private readonly PortalDbContext _context;

    public AssessmentStructureController(IAssessmentStructureService service, PortalDbContext context)
    {
        _service = service;
        _context = context;
    }

    // ── Assessment Structures ────────────────────────────────────────────────

    [Authorize(Policy = Permissions.AssessmentStructure.View)]
    public async Task<IActionResult> Index()
    {
        var structures = await _service.GetAllStructuresAsync();
        return View(structures);
    }

    [Authorize(Policy = Permissions.AssessmentStructure.Configure)]
    public async Task<IActionResult> Create()
    {
        var vm = new AssessmentStructureViewModel
        {
            Programs = await _context.CollegePrograms.Where(p => p.IsDeleted != true).OrderBy(p => p.ProgramName).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentStructure.Configure)]
    public async Task<IActionResult> Create(AssessmentStructureViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Programs = await _context.CollegePrograms.Where(p => p.IsDeleted != true).ToListAsync();
            return View(model);
        }

        // If setting as default, clear the flag from any existing default
        if (model.IsDefault)
        {
            var existingDefaults = await _context.AssessmentStructures
                .Where(s => s.IsDefault && s.IsDeleted != true)
                .ToListAsync();
            foreach (var d in existingDefaults) d.IsDefault = false;
            await _context.SaveChangesAsync();
        }

        var structure = new AssessmentStructure
        {
            Name = model.Name,
            Description = model.Description,
            CollegeProgramId = model.CollegeProgramId,
            ApplicableLevel = model.ApplicableLevel,
            IsDefault = model.IsDefault
        };
        await _service.CreateStructureAsync(structure);

        foreach (var (comp, idx) in model.Components.Select((c, i) => (c, i)))
        {
            await _service.CreateComponentAsync(new AssessmentComponent
            {
                AssessmentStructureId = structure.Id,
                ComponentName = comp.ComponentName,
                WeightPercent = comp.WeightPercent,
                MaxScore = comp.MaxScore,
                DisplayOrder = idx + 1
            });
        }

        TempData["SuccessMessage"] = "Assessment structure created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.AssessmentStructure.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var structure = await _service.GetStructureByIdAsync(id);
        if (structure is null) return NotFound();
        return View(structure);
    }

    [Authorize(Policy = Permissions.AssessmentStructure.Configure)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var structure = await _service.GetStructureByIdAsync(id);
        if (structure is null) return NotFound();

        var vm = new AssessmentStructureViewModel
        {
            Id = structure.Id,
            Name = structure.Name,
            Description = structure.Description,
            CollegeProgramId = structure.CollegeProgramId,
            ApplicableLevel = structure.ApplicableLevel,
            IsDefault = structure.IsDefault,
            Programs = await _context.CollegePrograms.Where(p => p.IsDeleted != true).ToListAsync(),
            Components = structure.Components?.Select(c => new ComponentInputViewModel
            {
                Id = c.Id,
                ComponentName = c.ComponentName,
                WeightPercent = c.WeightPercent,
                MaxScore = c.MaxScore,
                DisplayOrder = c.DisplayOrder
            }).ToList() ?? new()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentStructure.Configure)]
    public async Task<IActionResult> Edit(Guid id, AssessmentStructureViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Programs = await _context.CollegePrograms.Where(p => p.IsDeleted != true).ToListAsync();
            return View(model);
        }

        var structure = await _service.GetStructureByIdAsync(id);
        if (structure is null) return NotFound();

        // If setting as default, clear the flag from other structures first
        if (model.IsDefault && !structure.IsDefault)
        {
            var existingDefaults = await _context.AssessmentStructures
                .Where(s => s.IsDefault && s.Id != id && s.IsDeleted != true)
                .ToListAsync();
            foreach (var d in existingDefaults) d.IsDefault = false;
            await _context.SaveChangesAsync();
        }

        structure.Name = model.Name;
        structure.Description = model.Description;
        structure.CollegeProgramId = model.CollegeProgramId;
        structure.ApplicableLevel = model.ApplicableLevel;
        structure.IsDefault = model.IsDefault;
        await _service.UpdateStructureAsync(structure);

        // Refresh components: remove old, re-add new
        var old = await _service.GetComponentsByStructureIdAsync(id);
        foreach (var c in old) await _service.DeleteComponentAsync(c.Id);
        foreach (var (comp, idx) in model.Components.Select((c, i) => (c, i)))
        {
            await _service.CreateComponentAsync(new AssessmentComponent
            {
                AssessmentStructureId = id,
                ComponentName = comp.ComponentName,
                WeightPercent = comp.WeightPercent,
                MaxScore = comp.MaxScore,
                DisplayOrder = idx + 1
            });
        }

        TempData["SuccessMessage"] = "Assessment structure updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentStructure.Configure)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteStructureAsync(id);
        TempData["SuccessMessage"] = "Assessment structure deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Mark one structure as the default fallback for all courses.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.AssessmentStructure.Configure)]
    public async Task<IActionResult> SetDefaultStructure(Guid id)
    {
        // Clear existing defaults
        var allStructures = await _context.AssessmentStructures
            .Where(s => s.IsDeleted != true)
            .ToListAsync();
        foreach (var s in allStructures) s.IsDefault = false;
        var target = allStructures.FirstOrDefault(s => s.Id == id);
        if (target is not null) target.IsDefault = true;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Default assessment structure updated.";
        return RedirectToAction(nameof(Index));
    }

    // ── Grading Scales ───────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.GradingScale.View)]
    public async Task<IActionResult> GradingScales()
    {
        var scales = await _service.GetAllGradingScalesAsync();
        return View(scales);
    }

    [Authorize(Policy = Permissions.GradingScale.Configure)]
    public IActionResult CreateGradingScale()
    {
        var vm = new GradingScaleViewModel
        {
            Grades = GetDefaultGrades()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.GradingScale.Configure)]
    public async Task<IActionResult> CreateGradingScale(GradingScaleViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var scale = new GradingScale
        {
            ScaleName = model.ScaleName,
            Description = model.Description,
            IsDefault = model.IsDefault
        };

        var grades = model.Grades.Select(g => new GradeDefinition
        {
            GradeLetter = g.GradeLetter,
            MinScore = g.MinScore,
            MaxScore = g.MaxScore,
            GradePoint = g.GradePoint,
            Remark = g.Remark
        }).ToList();

        await _service.CreateGradingScaleAsync(scale, grades);
        TempData["SuccessMessage"] = "Grading scale created.";
        return RedirectToAction(nameof(GradingScales));
    }

    [Authorize(Policy = Permissions.GradingScale.Configure)]
    public async Task<IActionResult> EditGradingScale(Guid id)
    {
        var scale = await _service.GetGradingScaleByIdAsync(id);
        if (scale is null) return NotFound();

        var vm = new GradingScaleViewModel
        {
            Id = scale.Id,
            ScaleName = scale.ScaleName,
            Description = scale.Description,
            IsDefault = scale.IsDefault,
            Grades = scale.Grades?.Select(g => new GradeDefinitionInputViewModel
            {
                Id = g.Id,
                GradeLetter = g.GradeLetter,
                MinScore = g.MinScore,
                MaxScore = g.MaxScore,
                GradePoint = g.GradePoint,
                Remark = g.Remark
            }).ToList() ?? GetDefaultGrades()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.GradingScale.Configure)]
    public async Task<IActionResult> EditGradingScale(Guid id, GradingScaleViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var scale = await _service.GetGradingScaleByIdAsync(id);
        if (scale is null) return NotFound();

        scale.ScaleName = model.ScaleName;
        scale.Description = model.Description;
        scale.IsDefault = model.IsDefault;

        var grades = model.Grades.Select(g => new GradeDefinition
        {
            GradeLetter = g.GradeLetter,
            MinScore = g.MinScore,
            MaxScore = g.MaxScore,
            GradePoint = g.GradePoint,
            Remark = g.Remark
        }).ToList();

        await _service.UpdateGradingScaleAsync(scale, grades);
        TempData["SuccessMessage"] = "Grading scale updated.";
        return RedirectToAction(nameof(GradingScales));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.GradingScale.Configure)]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        await _service.SetDefaultGradingScaleAsync(id);
        TempData["SuccessMessage"] = "Default grading scale updated.";
        return RedirectToAction(nameof(GradingScales));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.GradingScale.Configure)]
    public async Task<IActionResult> DeleteGradingScale(Guid id)
    {
        await _service.DeleteGradingScaleAsync(id);
        TempData["SuccessMessage"] = "Grading scale deleted.";
        return RedirectToAction(nameof(GradingScales));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<GradeDefinitionInputViewModel> GetDefaultGrades() => new()
    {
        new() { GradeLetter = "A",   MinScore = 80, MaxScore = 100, GradePoint = 4.0m, Remark = "Distinction" },
        new() { GradeLetter = "B+",  MinScore = 75, MaxScore = 79,  GradePoint = 3.5m, Remark = "Very Good" },
        new() { GradeLetter = "B",   MinScore = 70, MaxScore = 74,  GradePoint = 3.0m, Remark = "Good" },
        new() { GradeLetter = "C+",  MinScore = 65, MaxScore = 69,  GradePoint = 2.5m, Remark = "Upper Credit" },
        new() { GradeLetter = "C",   MinScore = 60, MaxScore = 64,  GradePoint = 2.0m, Remark = "Lower Credit" },
        new() { GradeLetter = "D+",  MinScore = 55, MaxScore = 59,  GradePoint = 1.5m, Remark = "Pass" },
        new() { GradeLetter = "D",   MinScore = 50, MaxScore = 54,  GradePoint = 1.0m, Remark = "Bare Pass" },
        new() { GradeLetter = "F",   MinScore = 0,  MaxScore = 49,  GradePoint = 0.0m, Remark = "Fail" },
    };
}
