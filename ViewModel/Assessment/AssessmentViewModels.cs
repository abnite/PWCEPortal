using System.ComponentModel.DataAnnotations;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.ViewModel.Assessment;

public class AssessmentStructureViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? CollegeProgramId { get; set; }

    public int? ApplicableLevel { get; set; }

    public List<ComponentInputViewModel> Components { get; set; } = new();
    public List<CollegeProgram> Programs { get; set; } = new();
}

public class ComponentInputViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string ComponentName { get; set; } = string.Empty;

    [Required]
    [Range(1, 100)]
    public decimal WeightPercent { get; set; }

    [Required]
    [Range(1, 1000)]
    public decimal MaxScore { get; set; }

    public int DisplayOrder { get; set; }
}

public class GradingScaleViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string ScaleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public List<GradeDefinitionInputViewModel> Grades { get; set; } = new();
}

public class GradeDefinitionInputViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string GradeLetter { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public decimal MinScore { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal MaxScore { get; set; }

    [Required]
    [Range(0, 5)]
    public decimal GradePoint { get; set; }

    public string? Remark { get; set; }
}
