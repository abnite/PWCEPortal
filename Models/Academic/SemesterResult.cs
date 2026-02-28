using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// Stores a student's computed semester GPA after results are published.
/// </summary>
public class SemesterResult : EntityHelper
{
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    [ForeignKey("AcademicSemester")]
    public Guid AcademicSemesterId { get; set; }
    public AcademicSemester? AcademicSemester { get; set; }

    public decimal TotalWeightedScore { get; set; }

    public decimal GPA { get; set; }

    public int TotalCreditHours { get; set; }

    public bool IsPublished { get; set; } = false;

    public bool IsWithheld { get; set; } = false;

    public string? WithholdReason { get; set; }
}
