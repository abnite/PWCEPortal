using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Models.Staff;

/// <summary>
/// An appraisal instance for a non-teaching staff member.
/// </summary>
public class NonTeachingAppraisal : EntityHelper
{
    [ForeignKey("AppraisalTemplate")]
    public Guid AppraisalTemplateId { get; set; }
    public AppraisalTemplate? AppraisalTemplate { get; set; }

    [ForeignKey("NonTeachingStaff")]
    public Guid NonTeachingStaffId { get; set; }
    public NonTeachingStaff? NonTeachingStaff { get; set; }

    public string ConductedById { get; set; } = string.Empty;
    [ForeignKey("ConductedById")]
    public ApplicationUser? ConductedBy { get; set; }

    [ForeignKey("AcademicYear")]
    public Guid AcademicYearId { get; set; }
    public AcademicYear? AcademicYear { get; set; }

    public AppraisalStatus Status { get; set; } = AppraisalStatus.InProgress;

    public decimal? TotalScore { get; set; }

    public decimal? MaxPossibleScore { get; set; }

    public string? OverallRemarks { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation
    public ICollection<NonTeachingAppraisalScore>? Scores { get; set; }
}
