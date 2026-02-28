using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class AssessmentStructureService : IAssessmentStructureService
{
    private readonly PortalDbContext _context;

    public AssessmentStructureService(PortalDbContext context)
    {
        _context = context;
    }

    // ── Assessment Structures ────────────────────────────────────────────────

    public async Task<List<AssessmentStructure>> GetAllStructuresAsync() =>
        await _context.AssessmentStructures
            .Include(s => s.CollegeProgram)
            .Include(s => s.Components)
            .Where(s => s.IsDeleted != true)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<AssessmentStructure?> GetStructureByIdAsync(Guid id) =>
        await _context.AssessmentStructures
            .Include(s => s.CollegeProgram)
            .Include(s => s.Components)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted != true);

    public async Task<bool> CreateStructureAsync(AssessmentStructure structure)
    {
        _context.AssessmentStructures.Add(structure);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateStructureAsync(AssessmentStructure structure)
    {
        _context.AssessmentStructures.Update(structure);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteStructureAsync(Guid id)
    {
        var structure = await GetStructureByIdAsync(id);
        if (structure is null) return false;
        structure.IsDeleted = true;
        structure.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    // ── Components ───────────────────────────────────────────────────────────

    public async Task<List<AssessmentComponent>> GetComponentsByStructureIdAsync(Guid structureId) =>
        await _context.AssessmentComponents
            .Where(c => c.AssessmentStructureId == structureId && c.IsDeleted != true)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

    public async Task<AssessmentComponent?> GetComponentByIdAsync(Guid id) =>
        await _context.AssessmentComponents
            .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted != true);

    public async Task<bool> CreateComponentAsync(AssessmentComponent component)
    {
        _context.AssessmentComponents.Add(component);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateComponentAsync(AssessmentComponent component)
    {
        _context.AssessmentComponents.Update(component);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteComponentAsync(Guid id)
    {
        var component = await GetComponentByIdAsync(id);
        if (component is null) return false;
        component.IsDeleted = true;
        component.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<decimal> GetTotalWeightForStructureAsync(Guid structureId)
    {
        var components = await GetComponentsByStructureIdAsync(structureId);
        return components.Sum(c => c.WeightPercent);
    }

    // ── Grading Scales ───────────────────────────────────────────────────────

    public async Task<List<GradingScale>> GetAllGradingScalesAsync() =>
        await _context.GradingScales
            .Include(s => s.Grades)
            .Where(s => s.IsDeleted != true)
            .OrderBy(s => s.ScaleName)
            .ToListAsync();

    public async Task<GradingScale?> GetDefaultGradingScaleAsync() =>
        await _context.GradingScales
            .Include(s => s.Grades)
            .FirstOrDefaultAsync(s => s.IsDefault && s.IsDeleted != true);

    public async Task<GradingScale?> GetGradingScaleByIdAsync(Guid id) =>
        await _context.GradingScales
            .Include(s => s.Grades)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted != true);

    public async Task<bool> CreateGradingScaleAsync(GradingScale scale, List<GradeDefinition> grades)
    {
        if (scale.IsDefault)
        {
            // unset previous default
            var prev = await GetDefaultGradingScaleAsync();
            if (prev != null) { prev.IsDefault = false; }
        }
        _context.GradingScales.Add(scale);
        foreach (var g in grades) { g.GradingScaleId = scale.Id; _context.GradeDefinitions.Add(g); }
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateGradingScaleAsync(GradingScale scale, List<GradeDefinition> grades)
    {
        if (scale.IsDefault)
        {
            var prev = await _context.GradingScales.FirstOrDefaultAsync(s => s.IsDefault && s.Id != scale.Id);
            if (prev != null) { prev.IsDefault = false; }
        }
        _context.GradingScales.Update(scale);
        // Remove old grade definitions and replace
        var old = await _context.GradeDefinitions.Where(g => g.GradingScaleId == scale.Id).ToListAsync();
        _context.GradeDefinitions.RemoveRange(old);
        foreach (var g in grades) { g.GradingScaleId = scale.Id; _context.GradeDefinitions.Add(g); }
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteGradingScaleAsync(Guid id)
    {
        var scale = await GetGradingScaleByIdAsync(id);
        if (scale is null) return false;
        scale.IsDeleted = true;
        scale.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SetDefaultGradingScaleAsync(Guid id)
    {
        var prev = await GetDefaultGradingScaleAsync();
        if (prev != null) prev.IsDefault = false;
        var scale = await GetGradingScaleByIdAsync(id);
        if (scale is null) return false;
        scale.IsDefault = true;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<GradeDefinition?> GetGradeForScoreAsync(Guid gradingScaleId, decimal score) =>
        await _context.GradeDefinitions
            .Where(g => g.GradingScaleId == gradingScaleId && score >= g.MinScore && score <= g.MaxScore)
            .FirstOrDefaultAsync();
}
