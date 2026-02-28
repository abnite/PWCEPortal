using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Academic;

public class StudentCourseRegistration:EntityHelper
{
    [Required]
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }  
    public Student Student { get; set; }

    [Required]
    [ForeignKey("Course")]
    public Guid CourseId { get; set; }
    public Course Course { get; set; }
    
    [ForeignKey("Semester")]
    public Guid? SemesterId { get; set; }
    public AcademicSemester? Semester { get; set; }

    public bool IsRegistered { get; set; } // True if registration is successful

}