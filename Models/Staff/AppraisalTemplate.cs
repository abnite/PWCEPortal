using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Staff;

public enum AppraisalTemplateType
{
    Teaching = 1,
    NonTeaching = 2
}

/// <summary>
/// Configurable appraisal template (one per staff type per academic year, or shared).
/// </summary>
public class AppraisalTemplate : EntityHelper
{
    [Required]
    public string TemplateName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AppraisalTemplateType TemplateType { get; set; } = AppraisalTemplateType.Teaching;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<AppraisalCriterion>? Criteria { get; set; }
}
