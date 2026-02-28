namespace PWCEPortal.ViewModel.Reports;

public class StudentMetricsDashboardViewModel
{
    // List of programs along with the count of students enrolled in each
    public IEnumerable<ProgramMetric> ProgramMetrics { get; set; }

    // Aggregated metrics across all students
    public int DeferredCount { get; set; }
    public int DroppedOutCount { get; set; }
    public int TransferredCount { get; set; }
    public int TotalStudentsEnrolled { get; set; }
    
    public List<LevelMetric> LevelMetrics { get; set; }
}

public class LevelMetric
{
    public int Level { get; set; }
    public int TotalEnrolled { get; set; }
}

public class ProgramMetric
{
    public string ProgramName { get; set; }
    public int TotalEnrolled { get; set; }
}

public class CourseMetric
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; }
    public string CourseCode { get; set; }
    public int TotalStudents { get; set; }
}
    
public class SemesterMetric
{
    public string Semester { get; set; }
    public int TotalRegistered { get; set; }
}