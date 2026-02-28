using System.ComponentModel.DataAnnotations;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.ViewModel.CourseLecturer;

public class AssignLecturerViewModel
{
    [Required]
    public Guid LecturerId { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public Guid AcademicSemesterId { get; set; }

    // Dropdown data
    public List<Lecturer> Lecturers { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<AcademicSemester> Semesters { get; set; } = new();
}

public class LecturerViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    [Required]
    [Display(Name = "Staff ID")]
    public string StaffId { get; set; } = string.Empty;

    public string? Qualification { get; set; }

    public string? Specialisation { get; set; }
}

public class DepartmentViewModel
{
    [Required]
    [Display(Name = "Department Name")]
    public string DepartmentName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
