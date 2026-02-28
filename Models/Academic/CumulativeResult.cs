using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// Stores a student's cumulative GPA (CGPA) as computed at any point in time.
/// </summary>
public class CumulativeResult : EntityHelper
{
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    [ForeignKey("AcademicYear")]
    public Guid AcademicYearId { get; set; }
    public AcademicYear? AcademicYear { get; set; }

    public decimal CGPA { get; set; }

    public int TotalCreditHoursEarned { get; set; }

    public int TotalCreditHoursAttempted { get; set; }

    public string? Classification { get; set; } // "Distinction", "Merit", "Pass", "Fail"

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
