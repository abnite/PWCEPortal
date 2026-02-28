using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Models.Staff;

public enum AppraisalStatus
{
    InProgress = 0,
    Completed = 1
}

/// <summary>
/// An appraisal instance for a teaching staff member conducted by the HOD.
/// </summary>
public class TeachingAppraisal : EntityHelper
{
    [ForeignKey("AppraisalTemplate")]
    public Guid AppraisalTemplateId { get; set; }
    public AppraisalTemplate? AppraisalTemplate { get; set; }

    // The lecturer being appraised
    [ForeignKey("Lecturer")]
    public Guid LecturerId { get; set; }
    public Lecturer? Lecturer { get; set; }

    // HOD conducting the appraisal
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
    public ICollection<TeachingAppraisalScore>? Scores { get; set; }
}
