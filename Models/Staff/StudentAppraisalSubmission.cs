using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Staff;

/// <summary>
/// A single student's response to a TeachingAppraisalSession.
/// One submission per student per session.
/// </summary>
public class StudentAppraisalSubmission : EntityHelper
{
    [ForeignKey("Session")]
    public Guid SessionId { get; set; }
    public TeachingAppraisalSession? Session { get; set; }

    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public decimal? TotalScore { get; set; }

    public string? Remarks { get; set; }

    public bool IsSubmitted { get; set; } = false;

    public DateTime? SubmittedAt { get; set; }

    // Navigation
    public ICollection<StudentAppraisalScore>? Scores { get; set; }
}
