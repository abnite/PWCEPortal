using PWCEPortal.Models.Staff;

namespace PWCEPortal.Interfaces;

public interface ITeachingAppraisalService
{
    // Templates
    Task<List<AppraisalTemplate>> GetTeachingTemplatesAsync();
    Task<AppraisalTemplate?> GetTemplateByIdAsync(Guid id);
    Task<bool> CreateTemplateAsync(AppraisalTemplate template, List<AppraisalCriterion> criteria);
    Task<bool> UpdateTemplateAsync(AppraisalTemplate template, List<AppraisalCriterion> criteria);
    Task<bool> DeleteTemplateAsync(Guid id);

    // Appraisals
    Task<List<TeachingAppraisal>> GetAppraisalsAsync(Guid? lecturerId = null, Guid? academicYearId = null);
    Task<TeachingAppraisal?> GetAppraisalByIdAsync(Guid id);
    Task<TeachingAppraisal> StartAppraisalAsync(Guid templateId, Guid lecturerId, string hodUserId, Guid academicYearId);
    Task<bool> SaveAppraisalScoresAsync(Guid appraisalId, Dictionary<Guid, decimal> scores, string remarks);
    Task<bool> CompleteAppraisalAsync(Guid appraisalId);
}
