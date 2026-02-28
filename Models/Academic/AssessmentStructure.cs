using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// Defines how assessment marks are distributed for a programme/course combination.
/// e.g., "All Level 100 courses: CA=30%, Exam=70%"
/// </summary>
public class AssessmentStructure : EntityHelper
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Optionally scoped to a programme and/or level; null means applies to all
    [ForeignKey("CollegeProgram")]
    public Guid? CollegeProgramId { get; set; }
    public CollegeProgram? CollegeProgram { get; set; }

    public int? ApplicableLevel { get; set; } // 100, 200, … or null for all

    // Navigation
    public ICollection<AssessmentComponent>? Components { get; set; }
}
