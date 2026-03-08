using PWCEPortal.Models.Staff;

namespace PWCEPortal.Interfaces;

public interface INonTeachingStaffService
{
    // Staff records
    Task<List<NonTeachingStaff>> GetAllStaffAsync();
    Task<List<NonTeachingStaff>> GetStaffByDepartmentAsync(Guid departmentId);
    Task<NonTeachingStaff?> GetStaffByIdAsync(Guid id);
    Task<bool> CreateStaffAsync(NonTeachingStaff staff);
    Task<bool> UpdateStaffAsync(NonTeachingStaff staff);
    Task<bool> DeleteStaffAsync(Guid id);

    // Appraisals
    Task<List<NonTeachingAppraisal>> GetAppraisalsAsync(Guid? staffId = null, Guid? academicYearId = null);
    Task<NonTeachingAppraisal?> GetAppraisalByIdAsync(Guid id);
    Task<NonTeachingAppraisal> StartAppraisalAsync(Guid templateId, Guid staffId, string conductorUserId, Guid academicYearId);
    Task<int> StartBulkAppraisalAsync(Guid templateId, string conductorUserId, Guid academicYearId, Guid? departmentId);
    Task<bool> SaveAppraisalScoresAsync(Guid appraisalId, Dictionary<Guid, decimal> scores, string remarks);
    Task<bool> CompleteAppraisalAsync(Guid appraisalId);
}
