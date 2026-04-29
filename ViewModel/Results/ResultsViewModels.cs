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
    /// <summary>Provisional component-level results for courses not yet finally published.</summary>
    public List<InterimCourseResultViewModel> InterimResults { get; set; } = new();
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

/// <summary>Provisional interim result for a course where some components are still pending.</summary>
public class InterimCourseResultViewModel
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public Guid SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public List<InterimComponentResultViewModel> Components { get; set; } = new();
    /// <summary>Sum of weighted contributions for components that have marks.</summary>
    public decimal AvailableWeightedScore { get; set; }
    /// <summary>Sum of weight percentages for components that have marks.</summary>
    public decimal AvailableTotalWeight { get; set; }
}

/// <summary>Single assessment component row in the interim result view.</summary>
public class InterimComponentResultViewModel
{
    public string ComponentName { get; set; } = string.Empty;
    public decimal WeightPercent { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? Score { get; set; }                   // null = pending
    public decimal? WeightedContribution { get; set; }    // (Score/MaxScore)*WeightPercent, null if pending
}

public class ResultsPublishViewModel
{
    public AcademicSemester Semester { get; set; } = null!;
    public List<SemesterResult> Results { get; set; } = new();
    public int PublishedCount { get; set; }
    public int WithheldCount { get; set; }
    /// <summary>PrincipalApproved assignments available for interim or final publishing.</summary>
    public List<PendingInterimSubmissionViewModel> PendingSubmissions { get; set; } = new();
}

public class PendingInterimSubmissionViewModel
{
    public Guid AssignmentId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public bool IsInterimPublished { get; set; }
    /// <summary>True when every registered student has a mark for every component.</summary>
    public bool IsComplete { get; set; }
}
