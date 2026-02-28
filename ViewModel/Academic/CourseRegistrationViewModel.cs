using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.Academic;

public class CourseRegistrationViewModel
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
    public int CurrentLevel { get; set; }
    public Guid CollegeProgramId { get; set; }
    public string ProgramName { get; set; }
    public int Semester { get; set; }

    [Display(Name = "Available Courses")]
    public List<CourseSelection> AvailableCourses { get; set; } = new();

    [Required(ErrorMessage = "Select at least one course")]
    public List<Guid> SelectedCourseIds { get; set; } = new();
}
public class CourseSelection
{
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; }
    public string CourseName { get; set; }
    public string CourseType { get; set; } // Core/Elective
    public bool IsSelected { get; set; }
}