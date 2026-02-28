using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// A single grade band within a grading scale.
/// e.g., A: 80–100 = 4.0 GP
/// </summary>
public class GradeDefinition : EntityHelper
{
    [ForeignKey("GradingScale")]
    public Guid GradingScaleId { get; set; }
    public GradingScale? GradingScale { get; set; }

    [Required]
    public string GradeLetter { get; set; } = string.Empty; // "A", "B+", "B", etc.

    [Required]
    [Range(0, 100)]
    public decimal MinScore { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal MaxScore { get; set; }

    [Required]
    [Range(0, 5)]
    public decimal GradePoint { get; set; } // GPA contribution (e.g., 4.0)

    public string? Remark { get; set; } // "Distinction", "Merit", "Pass", "Fail"
}
