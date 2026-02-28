using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class Course:EntityHelper
{
    public string CourseCode { get; set; }
    [ForeignKey("CollegeProgram")]
   // [Required]
    public Guid? CollegeProgramId { get; set; }  
    public CollegeProgram? CollegeProgram { get; set; }
    
    [Required]
    public string CourseName { get; set; }
    
    public string CourseType { get; set; }//Elective/Core
    
    [Required]
    [Range(100, 400, ErrorMessage = "Level must be between 100 and 400.")]
    public int Level { get; set; } // 100, 200, 300, 400
    
    [Required]
    [Range(1, 2, ErrorMessage = "Semester must be between 1 and 2.")]
    public int Semester { get; set; } // 1 or 2
    
    // NEW
    public bool IsCommon { get; set; }   

  
}