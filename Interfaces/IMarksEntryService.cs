using PWCEPortal.Models.Academic;

namespace PWCEPortal.Interfaces;

public interface IMarksEntryService
{
    Task<List<CourseLecturerAssignment>> GetAssignmentsForLecturerAsync(string userId, Guid semesterId);
    Task<List<StudentMark>> GetMarksForAssignmentAsync(Guid assignmentId);
    Task<StudentMark?> GetMarkAsync(Guid assignmentId, Guid studentId, Guid componentId);
    Task<bool> SaveMarkAsync(StudentMark mark);
    Task<bool> SaveMarksAsync(List<StudentMark> marks);
    Task<(int saved, int errors, List<string> errorMessages)> BulkUploadMarksFromExcelAsync(
        Guid assignmentId, Stream fileStream, bool fillBlanksOnly = false);
    Task<bool> MarksDraftExistsAsync(Guid assignmentId);
    Task<AssessmentSubmission?> GetSubmissionForAssignmentAsync(Guid assignmentId);
}
