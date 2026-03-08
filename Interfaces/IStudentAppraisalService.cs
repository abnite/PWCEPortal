using PWCEPortal.Models.Staff;

namespace PWCEPortal.Interfaces;

public interface IStudentAppraisalService
{
    // HOD: Session management
    Task<TeachingAppraisalSession> CreateSessionAsync(Guid templateId, Guid assignmentId, Guid academicYearId, string hodUserId);
    Task<int> CreateBulkSessionsAsync(Guid templateId, Guid academicYearId, string hodUserId, Guid? departmentId);
    Task<List<TeachingAppraisalSession>> GetSessionsByHODAsync(string hodUserId);
    Task<List<TeachingAppraisalSession>> GetSessionsByDepartmentAsync(Guid departmentId);
    Task<TeachingAppraisalSession?> GetSessionByIdAsync(Guid sessionId);
    Task<bool> CloseSessionAsync(Guid sessionId);
    Task<bool> ReopenSessionAsync(Guid sessionId);

    // Student: fill in appraisal
    Task<List<TeachingAppraisalSession>> GetOpenSessionsForStudentAsync(Guid studentId);
    Task<StudentAppraisalSubmission?> GetSubmissionAsync(Guid sessionId, Guid studentId);
    Task<bool> SaveSubmissionAsync(Guid sessionId, Guid studentId, Dictionary<Guid, decimal> scores, string? remarks, bool submit);

    // Results
    Task<List<StudentAppraisalSubmission>> GetSessionSubmissionsAsync(Guid sessionId);
}
