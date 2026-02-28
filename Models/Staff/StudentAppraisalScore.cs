using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Staff;

/// <summary>
/// A student's score for a single criterion within a StudentAppraisalSubmission.
/// </summary>
public class StudentAppraisalScore : EntityHelper
{
    [ForeignKey("Submission")]
    public Guid SubmissionId { get; set; }
    public StudentAppraisalSubmission? Submission { get; set; }

    [ForeignKey("Criterion")]
    public Guid AppraisalCriterionId { get; set; }
    public AppraisalCriterion? Criterion { get; set; }

    public decimal Score { get; set; }
}
