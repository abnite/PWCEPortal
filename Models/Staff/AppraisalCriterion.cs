using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Staff;

/// <summary>
/// A single criterion within an appraisal template.
/// e.g., "Punctuality" max 10, "Lesson Delivery" max 20
/// </summary>
public class AppraisalCriterion : EntityHelper
{
    [ForeignKey("AppraisalTemplate")]
    public Guid AppraisalTemplateId { get; set; }
    public AppraisalTemplate? AppraisalTemplate { get; set; }

    [Required]
    public string CriterionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(1, 100)]
    public decimal MaxScore { get; set; }

    public int DisplayOrder { get; set; } = 1;
}
