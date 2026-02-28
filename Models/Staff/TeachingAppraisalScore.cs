using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Staff;

public class TeachingAppraisalScore : EntityHelper
{
    [ForeignKey("TeachingAppraisal")]
    public Guid TeachingAppraisalId { get; set; }
    public TeachingAppraisal? TeachingAppraisal { get; set; }

    [ForeignKey("AppraisalCriterion")]
    public Guid AppraisalCriterionId { get; set; }
    public AppraisalCriterion? AppraisalCriterion { get; set; }

    [Range(0, 100)]
    public decimal Score { get; set; }

    public string? Remarks { get; set; }
}
