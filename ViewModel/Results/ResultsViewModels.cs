using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.ViewModel.Results;

public class StudentResultsViewModel
{
    public Models.StudentInfo.Student Student { get; set; } = null!;
    public List<SemesterResult> SemesterResults { get; set; } = new();
    public CumulativeResult? CumulativeResult { get; set; }
}

public class ResultsPublishViewModel
{
    public AcademicSemester Semester { get; set; } = null!;
    public List<SemesterResult> Results { get; set; } = new();
    public int PublishedCount { get; set; }
    public int WithheldCount { get; set; }
}
