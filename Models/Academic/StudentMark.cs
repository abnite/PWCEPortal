using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// Stores a student's mark for a specific assessment component in a course assignment.
/// </summary>
public class StudentMark : EntityHelper
{
    [ForeignKey("CourseLecturerAssignment")]
    public Guid CourseLecturerAssignmentId { get; set; }
    public CourseLecturerAssignment? CourseLecturerAssignment { get; set; }

    [ForeignKey("AssessmentComponent")]
    public Guid AssessmentComponentId { get; set; }
    public AssessmentComponent? AssessmentComponent { get; set; }

    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    [Range(0, 1000)]
    public decimal? Score { get; set; } // Raw score (out of component MaxScore)

    public string? Remarks { get; set; }
}
