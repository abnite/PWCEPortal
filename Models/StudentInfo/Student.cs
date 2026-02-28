using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Models.StudentInfo;

public class Student:EntityHelper
{
    public string? Title { get; set; }
    public string ApplicationNumber { get; set; }
    public string? StudentID { get; set; }
    public string Surname { get; set; }
    public string OtherNames { get; set; }
    //public string Programme { get; set; }
    [ForeignKey("CollegeProgram")]
    public Guid CollegeProgramId { get; set; } // Foreign Key to Program table
    public CollegeProgram CollegeProgram { get; set; }
    public string? ContactAddress { get; set; }
    public string? Religion { get; set; }
    public bool? DisabilityStatus { get; set; }
    public string? District { get; set; }
    public string? HomeTown { get; set; }
    public string Gender { get; set; }
    public string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? ReligiousDenom { get; set; }
    public string PhoneNo { get; set; }
    public string? MaritalStatus { get; set; }
    public string? GhanaianLanguagesSpoken { get; set; }
    public int EnrolmentYear { get; set; }
    public int LevelOfEntry { get; set; }
    public int CurrentLevel { get; set; }
    public int ExpectedCompletionYear { get; set; }
    
    public decimal OutstandingFees { get; set; } = 0;
    
    [ForeignKey("CollegeClass")]
    public Guid?CollegeClassId { get; set; }
    public CollegeClass? CollegeClass { get; set; }
    
    [ForeignKey("CollegeHall")]
    public Guid?CollegeHallId { get; set; }
    public CollegeHall? CollegeHall { get; set; }
        
    public ApplicationUser? User { get; set; } // Link to Authentication
    
    public bool IsPromotionEligible { get; set; } = true; // Default to eligible
    public bool HasGraduated { get; set; } = false;
    
    public string? PassportPicturePath { get; set; }
    public string? Status { get; set; } = "Active";
    
    public string? StatusReason { get; set; }
    public string? TransferInstitution { get; set; }
    public DateTime? StatusChangeDate { get; set; }
    
    public string? CertificateFilePath { get; set; }
    
    public ICollection<StudentCourseRegistration>? StudentCourseRegistrations { get; set; }
}