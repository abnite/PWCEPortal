using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// A single mark component within an assessment structure.
/// e.g., "Quiz" = 10%, "Midterm" = 20%, "Final Exam" = 70%
/// </summary>
public class AssessmentComponent : EntityHelper
{
    [ForeignKey("AssessmentStructure")]
    public Guid AssessmentStructureId { get; set; }
    public AssessmentStructure? AssessmentStructure { get; set; }

    [Required]
    public string ComponentName { get; set; } = string.Empty; // e.g., "Quiz", "Midterm", "Final Exam"

    [Required]
    [Range(1, 100)]
    public decimal WeightPercent { get; set; } // e.g., 30.00

    [Required]
    [Range(1, 1000)]
    public decimal MaxScore { get; set; } // Maximum mark for this component (e.g., 100)

    public int DisplayOrder { get; set; } = 1;

    // Navigation
    public ICollection<StudentMark>? StudentMarks { get; set; }
}
