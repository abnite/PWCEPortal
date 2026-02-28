using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Staff;

namespace PWCEPortal.Services;

public class NonTeachingStaffService : INonTeachingStaffService
{
    private readonly PortalDbContext _context;

    public NonTeachingStaffService(PortalDbContext context)
    {
        _context = context;
    }

    public async Task<List<NonTeachingStaff>> GetAllStaffAsync() =>
        await _context.NonTeachingStaffMembers
            .Where(s => s.IsDeleted != true)
            .OrderBy(s => s.FullName)
            .ToListAsync();

    public async Task<NonTeachingStaff?> GetStaffByIdAsync(Guid id) =>
        await _context.NonTeachingStaffMembers
            .FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted != true);

    public async Task<bool> CreateStaffAsync(NonTeachingStaff staff)
    {
        _context.NonTeachingStaffMembers.Add(staff);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateStaffAsync(NonTeachingStaff staff)
    {
        _context.NonTeachingStaffMembers.Update(staff);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteStaffAsync(Guid id)
    {
        var staff = await GetStaffByIdAsync(id);
        if (staff is null) return false;
        staff.IsDeleted = true;
        staff.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<NonTeachingAppraisal>> GetAppraisalsAsync(Guid? staffId = null, Guid? academicYearId = null)
    {
        var query = _context.NonTeachingAppraisals
            .Include(a => a.NonTeachingStaff)
            .Include(a => a.ConductedBy)
            .Include(a => a.AcademicYear)
            .Include(a => a.AppraisalTemplate)
            .Where(a => a.IsDeleted != true);

        if (staffId.HasValue) query = query.Where(a => a.NonTeachingStaffId == staffId.Value);
        if (academicYearId.HasValue) query = query.Where(a => a.AcademicYearId == academicYearId.Value);

        return await query.OrderByDescending(a => a.DateAdded).ToListAsync();
    }

    public async Task<NonTeachingAppraisal?> GetAppraisalByIdAsync(Guid id) =>
        await _context.NonTeachingAppraisals
            .Include(a => a.NonTeachingStaff)
            .Include(a => a.ConductedBy)
            .Include(a => a.AcademicYear)
            .Include(a => a.AppraisalTemplate).ThenInclude(t => t!.Criteria)
            .Include(a => a.Scores).ThenInclude(s => s.AppraisalCriterion)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted != true);

    public async Task<NonTeachingAppraisal> StartAppraisalAsync(Guid templateId, Guid staffId, string conductorUserId, Guid academicYearId)
    {
        var template = await _context.AppraisalTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == templateId);
        decimal maxScore = template?.Criteria?.Sum(c => c.MaxScore) ?? 0;

        var appraisal = new NonTeachingAppraisal
        {
            AppraisalTemplateId = templateId,
            NonTeachingStaffId = staffId,
            ConductedById = conductorUserId,
            AcademicYearId = academicYearId,
            Status = AppraisalStatus.InProgress,
            MaxPossibleScore = maxScore
        };
        _context.NonTeachingAppraisals.Add(appraisal);
        await _context.SaveChangesAsync();
        return appraisal;
    }

    public async Task<bool> SaveAppraisalScoresAsync(Guid appraisalId, Dictionary<Guid, decimal> scores, string remarks)
    {
        var appraisal = await GetAppraisalByIdAsync(appraisalId);
        if (appraisal is null) return false;

        var old = await _context.NonTeachingAppraisalScores.Where(s => s.NonTeachingAppraisalId == appraisalId).ToListAsync();
        _context.NonTeachingAppraisalScores.RemoveRange(old);

        foreach (var (criterionId, score) in scores)
        {
            _context.NonTeachingAppraisalScores.Add(new NonTeachingAppraisalScore
            {
                NonTeachingAppraisalId = appraisalId,
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
