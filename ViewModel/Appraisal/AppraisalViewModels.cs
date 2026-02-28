using System.ComponentModel.DataAnnotations;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.Staff;

namespace PWCEPortal.ViewModel.Appraisal;

public class AppraisalTemplateViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string TemplateName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AppraisalTemplateType TemplateType { get; set; } = AppraisalTemplateType.Teaching;

    public bool IsActive { get; set; } = true;

    public List<CriterionInputViewModel> Criteria { get; set; } = new();
}

public class CriterionInputViewModel
{
    [Required]
    public string CriterionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(1, 100)]
    public decimal MaxScore { get; set; }

    public int DisplayOrder { get; set; }
}

public class ConductAppraisalViewModel
{
    public TeachingAppraisal Appraisal { get; set; } = null!;
    public List<AppraisalCriterion> Criteria { get; set; } = new();
    public Dictionary<Guid, decimal> Scores { get; set; } = new();
    public string Remarks { get; set; } = string.Empty;
}

public class StartAppraisalViewModel
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    public Guid LecturerId { get; set; }

    [Required]
    public Guid AcademicYearId { get; set; }

    public List<AppraisalTemplate> Templates { get; set; } = new();
    public List<Lecturer> Lecturers { get; set; } = new();
    public List<AcademicYear> AcademicYears { get; set; } = new();
}
