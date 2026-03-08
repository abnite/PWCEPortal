using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.ViewModel.Results;

public class StudentResultsViewModel
{
    public Models.StudentInfo.Student Student { get; set; } = null!;
    public List<SemesterResult> SemesterResults { get; set; } = new();
    public CumulativeResult? CumulativeResult { get; set; }
    /// <summary>Keyed by SemesterId – per-course breakdown for that semester.</summary>
    public Dictionary<Guid, List<CourseResultViewModel>> CourseResultsBySemester { get; set; } = new();
}

/// <summary>Per-course result shown inside MyResults and Transcript.</summary>
public class CourseResultViewModel
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int CreditHours { get; set; } = 3;
    public decimal TotalScore { get; set; }       // Weighted percentage (0-100)
    public string GradeLetter { get; set; } = "-";
    public decimal GradePoint { get; set; }
    public decimal QualityPoints => GradePoint * CreditHours;
}

public class ResultsPublishViewModel
{
    public AcademicSemester Semester { get; set; } = null!;
    public List<SemesterResult> Results { get; set; } = new();
    public int PublishedCount { get; set; }
    public int WithheldCount { get; set; }
}
