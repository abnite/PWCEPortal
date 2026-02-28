using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.Student;

public class ParentGuardianViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [Display(Name = "Telephone")]
    public string Telephone { get; set; }

    [Display(Name = "Occupation")]
    public string Occupation { get; set; }

    [Display(Name = "Contact Address")]
    public string ContactAddress { get; set; }

    [Required]
    [Display(Name = "Relationship")]
    public string Relationship { get; set; }

    // Foreign Key to Student
    public Guid StudentId { get; set; }

}