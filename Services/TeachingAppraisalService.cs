using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Staff;

namespace PWCEPortal.Services;

public class TeachingAppraisalService : ITeachingAppraisalService
{
    private readonly PortalDbContext _context;

    public TeachingAppraisalService(PortalDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppraisalTemplate>> GetTeachingTemplatesAsync() =>
        await _context.AppraisalTemplates
            .Include(t => t.Criteria)
            .Where(t => t.TemplateType == AppraisalTemplateType.Teaching && t.IsActive && t.IsDeleted != true)
            .OrderBy(t => t.TemplateName)
            .ToListAsync();

    public async Task<AppraisalTemplate?> GetTemplateByIdAsync(Guid id) =>
        await _context.AppraisalTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted != true);

    public async Task<bool> CreateTemplateAsync(AppraisalTemplate template, List<AppraisalCriterion> criteria)
    {
        _context.AppraisalTemplates.Add(template);
        foreach (var c in criteria)
        {
            c.AppraisalTemplateId = template.Id;
            _context.AppraisalCriteria.Add(c);
        }
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateTemplateAsync(AppraisalTemplate template, List<AppraisalCriterion> criteria)
    {
        _context.AppraisalTemplates.Update(template);
        var old = await _context.AppraisalCriteria.Where(c => c.AppraisalTemplateId == template.Id).ToListAsync();
        _context.AppraisalCriteria.RemoveRange(old);
        foreach (var c in criteria)
        {
            c.AppraisalTemplateId = template.Id;
            _context.AppraisalCriteria.Add(c);
        }
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var template = await GetTemplateByIdAsync(id);
        if (template is null) return false;
        template.IsDeleted = true;
        template.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<TeachingAppraisal>> GetAppraisalsAsync(Guid? lecturerId = null, Guid? academicYearId = null)
    {
        var query = _context.TeachingAppraisals
            .Include(a => a.Lecturer).ThenInclude(l => l!.User)
            .Include(a => a.ConductedBy)
            .Include(a => a.AcademicYear)
            .Include(a => a.AppraisalTemplate)
            .Where(a => a.IsDeleted != true);

        if (lecturerId.HasValue) query = query.Where(a => a.LecturerId == lecturerId.Value);
        if (academicYearId.HasValue) query = query.Where(a => a.AcademicYearId == academicYearId.Value);

        return await query.OrderByDescending(a => a.DateAdded).ToListAsync();
    }

    public async Task<TeachingAppraisal?> GetAppraisalByIdAsync(Guid id) =>
        await _context.TeachingAppraisals
            .Include(a => a.Lecturer).ThenInclude(l => l!.User)
            .Include(a => a.ConductedBy)
            .Include(a => a.AcademicYear)
            .Include(a => a.AppraisalTemplate).ThenInclude(t => t!.Criteria)
            .Include(a => a.Scores).ThenInclude(s => s.AppraisalCriterion)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted != true);

    public async Task<TeachingAppraisal> StartAppraisalAsync(Guid templateId, Guid lecturerId, string hodUserId, Guid academicYearId)
    {
        var template = await GetTemplateByIdAsync(templateId);
        decimal maxScore = template?.Criteria?.Sum(c => c.MaxScore) ?? 0;

        var appraisal = new TeachingAppraisal
        {
            AppraisalTemplateId = templateId,
            LecturerId = lecturerId,
            ConductedById = hodUserId,
            AcademicYearId = academicYearId,
            Status = AppraisalStatus.InProgress,
            MaxPossibleScore = maxScore
        };
        _context.TeachingAppraisals.Add(appraisal);
        await _context.SaveChangesAsync();
        return appraisal;
    }

    public async Task<bool> SaveAppraisalScoresAsync(Guid appraisalId, Dictionary<Guid, decimal> scores, string remarks)
    {
        var appraisal = await GetAppraisalByIdAsync(appraisalId);
        if (appraisal is null) return false;

        var old = await _context.TeachingAppraisalScores.Where(s => s.TeachingAppraisalId == appraisalId).ToListAsync();
        _context.TeachingAppraisalScores.RemoveRange(old);

        foreach (var (criterionId, score) in scores)
        {
            _context.TeachingAppraisalScores.Add(new TeachingAppraisalScore
            {
                TeachingAppraisalId = appraisalId,
                AppraisalCriterionId = criterionId,
                Score = score
            });
        }

        appraisal.TotalScore = scores.Values.Sum();
        appraisal.OverallRemarks = remarks;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> CompleteAppraisalAsync(Guid appraisalId)
    {
        var appraisal = await GetAppraisalByIdAsync(appraisalId);
        if (appraisal is null) return false;
        appraisal.Status = AppraisalStatus.Completed;
        appraisal.CompletedAt = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }
}
