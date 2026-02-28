using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;

namespace PWCEPortal.Models.Academic;

public class Lecturer : EntityHelper
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [ForeignKey("Department")]
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string StaffId { get; set; } = string.Empty;

    public string? Qualification { get; set; }

    public string? Specialisation { get; set; }

    // Navigation
    public ICollection<CourseLecturerAssignment>? CourseAssignments { get; set; }
}
