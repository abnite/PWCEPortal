namespace PWCEPortal.ViewModel.Academic;

public class RegisteredCoursesViewModel
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
    public string ProgramName { get; set; }
    public int CurrentLevel { get; set; }
    public string Semester { get; set; } // Only for current semester view
    public List<RegisteredCourseView> Courses { get; set; }
}

public class RegisteredCourseView
{
    public string CourseCode { get; set; }
    public string CourseName { get; set; }
    public string CourseType { get; set; }
    public string Semester { get; set; } // For all semesters view
    public DateTime RegistrationDate { get; set; }
}