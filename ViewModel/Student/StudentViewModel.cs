using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PWCEPortal.ApplicationClass;

namespace PWCEPortal.ViewModel.Student;

public class StudentViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Application Number")]
    public string ApplicationNumber { get; set; }
    public string? StudentId { get; set; }
    
    [Required]
    [Display(Name = "Title")]
    public string Title { get; set; }

    [Required]
    public string Surname { get; set; }

    [Required]
    [Display(Name = "Other Names")]
    public string OtherNames { get; set; }

    [Required]
    [Display(Name = "College Program")]
    public Guid CollegeProgramId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [Display(Name = "Phone Number")]
    public string PhoneNo { get; set; }

    [Required]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public string Gender { get; set; }

    [Required]
    [Display(Name = "Enrolment Year")]
    public int EnrolmentYear { get; set; }

    [Required]
    [Display(Name = "Current Level")]
    public int CurrentLevel { get; set; }
    
    [Required]
    [Display(Name = "Entry Level")]
    public int EntryLevel { get; set; }
    
    public string? ContactAddress { get; set; }
    public string? Religion { get; set; }
    public string? ReligiousDenomination{ get; set; }
    public bool DisabilityStatus{ get; set; }
    public string? District{ get; set; }
    public string? HomeTown{ get; set; }
    public string? PlaceofBirth{ get; set; }
    public string? MaritalStatus{ get; set; }
    public string? GhanaianlanguagesSpoken{ get; set; }
    
    public Guid? CollegeHallId{ get; set; }
    public Guid?CollegeClassId { get; set; }
    
    [Display(Name = "Passport Picture")]
    [DataType(DataType.Upload)]
    [CustomImageValidation(ErrorMessage = "Only .jpg, .jpeg, or .png files are allowed")]
    [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Maximum allowed file size is 5MB")] 
    public IFormFile? PassportPicture { get; set; }

    public string? ExistingPassportPicturePath { get; set; }
    
    public IFormFile? CertificateFile { get; set; }

    public string? CertificateFilePath { get; set; }
    
    

    // Dropdown for College Programs
    public List<SelectListItem> CollegePrograms { get; set; } = new List<SelectListItem>();

}


