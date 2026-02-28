using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class AcademicSemester : EntityHelper,IValidatableObject
{
    [ForeignKey("AcademicYear")]
    [Required]
    public Guid AcademicYearId { get; set; } // Foreign key to the academic year
    public AcademicYear AcademicYear { get; set; }
    [Required]
    public string SemesterName { get; set; } // e.g., "Semester 1", "Semester 2"
    public bool IsRegistrationActive { get; set; } // Indicates if course registration is open for this semester
    [Required]
    public DateTime RegistrationStartDate { get; set; }
    [Required]
    public DateTime RegistrationEndDate { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RegistrationEndDate < RegistrationStartDate)
        {
            yield return new ValidationResult(
                "The registration end date must be greater than or equal to the start date.",
                new[] { nameof(RegistrationEndDate) });
        }
    }
}