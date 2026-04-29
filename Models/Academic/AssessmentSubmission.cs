using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;

namespace PWCEPortal.Models.Academic;

public enum SubmissionStatus
{
    Draft = 0,
    SubmittedForReview = 1,
    HODApproved = 2,
    HODRejected = 3,
    QAApproved = 4,
    QAFlagged = 5,
    PrincipalApproved = 6,
    Unlocked = 7
}

/// <summary>
/// Represents the submission of a course's marks for the multi-stage approval workflow.
/// </summary>
public class AssessmentSubmission : EntityHelper
{
    [ForeignKey("CourseLecturerAssignment")]
    public Guid CourseLecturerAssignmentId { get; set; }
    public CourseLecturerAssignment? CourseLecturerAssignment { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;

    public DateTime? SubmittedAt { get; set; }

    // HOD review
    public string? HODReviewerId { get; set; }
    [ForeignKey("HODReviewerId")]
    public ApplicationUser? HODReviewer { get; set; }
    public DateTime? HODReviewedAt { get; set; }
    public string? HODComments { get; set; }

    // QA review
    public string? QAReviewerId { get; set; }
    [ForeignKey("QAReviewerId")]
    public ApplicationUser? QAReviewer { get; set; }
    public DateTime? QAReviewedAt { get; set; }
    public string? QAComments { get; set; }

    // Principal approval
    public string? PrincipalReviewerId { get; set; }
    [ForeignKey("PrincipalReviewerId")]
    public ApplicationUser? PrincipalReviewer { get; set; }
    public DateTime? PrincipalReviewedAt { get; set; }
    public string? PrincipalComments { get; set; }

    // Interim publish (marks visible to fee-paid students before all components arrive)
    public bool IsInterimPublished { get; set; } = false;
    public DateTime? InterimPublishedAt { get; set; }

    // Navigation
    public ICollection<AssessmentApprovalLog>? ApprovalLogs { get; set; }
}
