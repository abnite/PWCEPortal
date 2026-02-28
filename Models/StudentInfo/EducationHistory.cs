using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.StudentInfo;

public class EducationHistory:EntityHelper
{
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public string SchoolName { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? OfficeHeld { get; set; }

  
}