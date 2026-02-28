using System.ComponentModel.DataAnnotations;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.Staff;

namespace PWCEPortal.ViewModel.NonTeachingStaff;

public class NonTeachingStaffFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Staff ID")]
    public string StaffId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Position { get; set; } = string.Empty;

    public string? Department { get; set; }

    [Display(Name = "Department")]
    public Guid? DepartmentId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNo { get; set; }

    public string? Gender { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date Employed")]
    public DateTime? DateEmployed { get; set; }

    public string? Qualification { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ConductNonTeachingAppraisalViewModel
{
    public NonTeachingAppraisal Appraisal { get; set; } = null!;
    public List<AppraisalCriterion> Criteria { get; set; } = new();
    public Dictionary<Guid, decimal> Scores { get; set; } = new();
    public string Remarks { get; set; } = string.Empty;
}

public class StartNonTeachingAppraisalViewModel
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    public Guid StaffId { get; set; }

    [Required]
    public Guid AcademicYearId { get; set; }

    public List<AppraisalTemplate> Templates { get; set; } = new();
    public List<PWCEPortal.Models.Staff.NonTeachingStaff> StaffList { get; set; } = new();
    public List<AcademicYear> AcademicYears { get; set; } = new();
}
