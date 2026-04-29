using PWCEPortal.Models.Academic;
using PWCEPortal.ViewModel.Results;

namespace PWCEPortal.Interfaces;

public interface IGPAService
{
    /// <summary>Compute or recompute GPA for all students in a semester and persist the results.
    /// Only processes course assignments where every registered student has marks for every component.</summary>
    Task<bool> ComputeSemesterResultsAsync(Guid semesterId);

    /// <summary>Compute CGPA for a specific student up to and including the given academic year.</summary>
    Task<CumulativeResult> ComputeCGPAAsync(Guid studentId, Guid academicYearId);

    Task<SemesterResult?> GetSemesterResultAsync(Guid studentId, Guid semesterId);
    Task<CumulativeResult?> GetCumulativeResultAsync(Guid studentId, Guid academicYearId);
    Task<List<SemesterResult>> GetStudentSemesterResultsAsync(Guid studentId);

    Task<bool> PublishResultsAsync(Guid semesterId);
    Task<bool> WithholdResultAsync(Guid studentId, Guid semesterId, string reason);
    Task<bool> UnWithholdResultAsync(Guid studentId, Guid semesterId);

    /// <summary>Per-course breakdown for a student keyed by SemesterId.</summary>
    Task<Dictionary<Guid, List<CourseResultViewModel>>> GetCourseResultsBySemesterAsync(Guid studentId);

    /// <summary>Mark a course assignment as interim-published so fee-paying students can see provisional marks.</summary>
    Task<bool> PublishInterimAsync(Guid assignmentId);

    /// <summary>Provisional component-level results for courses that are interim-published but not yet finally published.</summary>
    Task<List<InterimCourseResultViewModel>> GetInterimCourseResultsAsync(Guid studentId);

    /// <summary>True when the student has paid at least 70% of their total outstanding fees across all years.</summary>
    Task<bool> HasSufficientFeePaymentAsync(Guid studentId);

    /// <summary>For the AllResults admin page: PrincipalApproved submissions with completeness info.</summary>
    Task<List<PendingInterimSubmissionViewModel>> GetPendingSubmissionsForSemesterAsync(Guid semesterId);
}
