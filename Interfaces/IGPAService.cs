using PWCEPortal.Models.Academic;
using PWCEPortal.ViewModel.Results;

namespace PWCEPortal.Interfaces;

public interface IGPAService
{
    /// <summary>Compute or recompute GPA for all students in a semester and persist the results.</summary>
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
}
