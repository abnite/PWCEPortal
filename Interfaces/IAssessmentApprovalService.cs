using PWCEPortal.Models.Academic;

namespace PWCEPortal.Interfaces;

public interface IAssessmentApprovalService
{
    Task<List<AssessmentSubmission>> GetPendingSubmissionsAsync(string userId, bool includeApproved = false);
    Task<AssessmentSubmission?> GetSubmissionByIdAsync(Guid id);
    Task<bool> SubmitForReviewAsync(Guid assignmentId, string lecturerUserId);
    Task<bool> HODReviewAsync(Guid submissionId, string hodUserId, bool approve, string comments);
    Task<bool> QAReviewAsync(Guid submissionId, string qaUserId, bool approve, string comments);
    Task<bool> PrincipalApproveAsync(Guid submissionId, string principalUserId, string comments);
    Task<bool> UnlockAssessmentAsync(Guid submissionId, string principalUserId, string reason);
    Task<List<AssessmentApprovalLog>> GetApprovalLogsAsync(Guid submissionId);
}
