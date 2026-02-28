using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// Audit trail for each action taken during the assessment approval workflow.
/// </summary>
public class AssessmentApprovalLog : EntityHelper
{
    [ForeignKey("AssessmentSubmission")]
    public Guid AssessmentSubmissionId { get; set; }
    public AssessmentSubmission? AssessmentSubmission { get; set; }

    [Required]
    public string ActorId { get; set; } = string.Empty;
    [ForeignKey("ActorId")]
    public ApplicationUser? Actor { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty; // "Submitted", "HODApproved", "HODRejected", "QAApproved", etc.

    public string? Comments { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}
