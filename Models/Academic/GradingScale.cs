using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

/// <summary>
/// A named grading scale (e.g., "Ghana Polytechnic Standard").
/// </summary>
public class GradingScale : EntityHelper
{
    [Required]
    public string ScaleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    // Navigation
    public ICollection<GradeDefinition>? Grades { get; set; }
}
