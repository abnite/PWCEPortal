namespace PWCEPortal.ViewModel.Reports;

public class StudentCourseViewModel
{
    public string StudentId { get; set; }
    public string FullName { get; set; }
    public string Program { get; set; }
    public int Level { get; set; }
    public List<string> EnrolledCourses { get; set; } = new List<string>();
}