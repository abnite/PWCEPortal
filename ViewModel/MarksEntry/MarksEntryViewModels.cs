using System.ComponentModel.DataAnnotations;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.ViewModel.MarksEntry;

public class MarkSheetViewModel
{
    public CourseLecturerAssignment Assignment { get; set; } = null!;
    public List<AssessmentComponent> Components { get; set; } = new();
    public List<StudentMarkRowViewModel> StudentRows { get; set; } = new();
    public AssessmentSubmission? Submission { get; set; }
}

public class StudentMarkRowViewModel
{
    public Student Student { get; set; } = null!;
    public Dictionary<Guid, decimal?> ComponentScores { get; set; } = new(); // ComponentId -> Score
    public decimal? TotalWeightedScore { get; set; }
    public string? Grade { get; set; }
}

public class SaveMarksViewModel
{
    public Guid AssignmentId { get; set; }
    public List<MarkEntryViewModel> Marks { get; set; } = new();
}

public class MarkEntryViewModel
{
    public Guid StudentId { get; set; }
    public Guid ComponentId { get; set; }
    public decimal? Score { get; set; }
    public string? Remarks { get; set; }
}
