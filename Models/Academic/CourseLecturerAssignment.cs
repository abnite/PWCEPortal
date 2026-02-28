using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class CourseLecturerAssignment : EntityHelper
{
    [ForeignKey("Lecturer")]
    public Guid LecturerId { get; set; }
    public Lecturer? Lecturer { get; set; }

    [ForeignKey("Course")]
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }

    [ForeignKey("AcademicSemester")]
    public Guid AcademicSemesterId { get; set; }
    public AcademicSemester? AcademicSemester { get; set; }

    // Derived navigation for marks entry and submissions
    public ICollection<StudentMark>? StudentMarks { get; set; }
    public ICollection<AssessmentSubmission>? Submissions { get; set; }
}
