using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Models.Staff;

/// <summary>
/// An appraisal session opened by an HOD for a specific course-lecturer assignment.
/// Students registered to that course fill in their feedback via StudentAppraisalSubmission.
/// </summary>
public class TeachingAppraisalSession : EntityHelper
{
    [ForeignKey("AppraisalTemplate")]
    public Guid AppraisalTemplateId { get; set; }
    public AppraisalTemplate? AppraisalTemplate { get; set; }

    /// <summary>The course-lecturer-semester assignment this session covers.</summary>
    [ForeignKey("Assignment")]
    public Guid CourseLecturerAssignmentId { get; set; }
    public CourseLecturerAssignment? Assignment { get; set; }

    [ForeignKey("AcademicYear")]
    public Guid AcademicYearId { get; set; }
    public AcademicYear? AcademicYear { get; set; }

    /// <summary>The HOD who created and opened this session.</summary>
    public string CreatedByHODId { get; set; } = string.Empty;
    [ForeignKey("CreatedByHODId")]
    public ApplicationUser? CreatedByHOD { get; set; }

    /// <summary>When false, students can no longer submit responses.</summary>
    public bool IsOpen { get; set; } = true;

    public DateTime? ClosedAt { get; set; }

    // Navigation
    public ICollection<StudentAppraisalSubmission>? Submissions { get; set; }
}
