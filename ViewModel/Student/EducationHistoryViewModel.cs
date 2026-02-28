using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.Student;

public class EducationHistoryViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "School Name")]
    public string SchoolName { get; set; }

    [Required]
    [Display(Name = "From Date")]
    [DataType(DataType.Date)]
    public DateTime FromDate { get; set; }

    [Required]
    [Display(Name = "To Date")]
    [DataType(DataType.Date)]
    public DateTime ToDate { get; set; }

    [Display(Name = "Office Held")]
    public string OfficeHeld { get; set; }

    // Foreign Key to Student
    public Guid StudentId { get; set; }
}