using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class GPAService : IGPAService
{
    private readonly PortalDbContext _context;
    private readonly IAssessmentStructureService _structureService;

    public GPAService(PortalDbContext context, IAssessmentStructureService structureService)
    {
        _context = context;
        _structureService = structureService;
    }

    public async Task<bool> ComputeSemesterResultsAsync(Guid semesterId)
    {
        var gradingScale = await _structureService.GetDefaultGradingScaleAsync();
        if (gradingScale is null) return false;

        // Get all course assignments for this semester that are fully approved
        var assignments = await _context.CourseLecturerAssignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .Where(a => a.AcademicSemesterId == semesterId
                     && a.IsDeleted != true
                     && a.Submissions!.Any(s => s.Status == SubmissionStatus.PrincipalApproved))
            .ToListAsync();

        // Get all registered students for this semester
        var registrations = await _context.StudentCourseRegistrations
            .Include(r => r.Student)
            .Where(r => r.SemesterId == semesterId && r.IsRegistered)
            .ToListAsync();

        var studentIds = registrations.Select(r => r.StudentId).Distinct().ToList();

        foreach (var studentId in studentIds)
        {
            decimal totalWeightedGP = 0;
            int totalCreditHours = 0;

            var studentCourses = registrations.Where(r => r.StudentId == studentId).ToList();

            foreach (var reg in studentCourses)
            {
                var assignment = assignments.FirstOrDefault(a => a.CourseId == reg.CourseId);
                if (assignment is null) continue;

                // Calculate weighted score for this course
                var components = await _context.AssessmentComponents
                    .Include(c => c.AssessmentStructure)
                    .Where(c => c.AssessmentStructure!.CollegeProgramId == assignment.Course!.CollegeProgramId
                             || c.AssessmentStructure!.CollegeProgramId == null)
                    .ToListAsync();

                var marks = await _context.StudentMarks
                    .Where(m => m.CourseLecturerAssignmentId == assignment.Id
                             && m.StudentId == studentId
                             && m.IsDeleted != true)
                    .ToListAsync();

                decimal courseWeightedScore = 0;
                foreach (var component in components)
                {
                    var mark = marks.FirstOrDefault(m => m.AssessmentComponentId == component.Id);
                    if (mark?.Score is null) continue;
                    // Convert score to percentage of max, then apply weight
                    decimal pct = (mark.Score.Value / component.MaxScore) * 100m;
                    courseWeightedScore += pct * (component.WeightPercent / 100m);
                }

                var grade = await _structureService.GetGradeForScoreAsync(gradingScale.Id, courseWeightedScore);
                if (grade is null) continue;

                int creditHours = 3; // Default credit hours per course
                totalWeightedGP += grade.GradePoint * creditHours;
                totalCreditHours += creditHours;
            }

            decimal gpa = totalCreditHours > 0 ? Math.Round(totalWeightedGP / totalCreditHours, 2) : 0;

            var existing = await _context.SemesterResults
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicSemesterId == semesterId);

            if (existing is null)
            {
                _context.SemesterResults.Add(new SemesterResult
                {
                    StudentId = studentId,
                    AcademicSemesterId = semesterId,
                    GPA = gpa,
                    TotalWeightedScore = totalWeightedGP,
                    TotalCreditHours = totalCreditHours
                });
            }
            else
            {
                existing.GPA = gpa;
                existing.TotalWeightedScore = totalWeightedGP;
                existing.TotalCreditHours = totalCreditHours;
            }
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<CumulativeResult> ComputeCGPAAsync(Guid studentId, Guid academicYearId)
    {
        var semesterResults = await _context.SemesterResults
            .Include(r => r.AcademicSemester)
            .Where(r => r.StudentId == studentId && r.IsPublished && r.IsDeleted != true)
            .ToListAsync();

        decimal totalWeightedGP = semesterResults.Sum(r => r.TotalWeightedScore);
        int totalCreditHours = semesterResults.Sum(r => r.TotalCreditHours);
        decimal cgpa = totalCreditHours > 0 ? Math.Round(totalWeightedGP / totalCreditHours, 2) : 0;

        string classification = cgpa switch
        {
            >= 3.5m => "Distinction",
            >= 3.0m => "Upper Credit",
            >= 2.5m => "Lower Credit",
            >= 2.0m => "Pass",
            _ => "Fail"
        };

        var existing = await _context.CumulativeResults
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicYearId == academicYearId);

        if (existing is null)
        {
            existing = new CumulativeResult
            {
                StudentId = studentId,
                AcademicYearId = academicYearId,
                CGPA = cgpa,
                TotalCreditHoursEarned = totalCreditHours,
                TotalCreditHoursAttempted = totalCreditHours,
                Classification = classification,
                ComputedAt = DateTime.UtcNow
            };
            _context.CumulativeResults.Add(existing);
        }
        else
        {
            existing.CGPA = cgpa;
            existing.TotalCreditHoursEarned = totalCreditHours;
            existing.TotalCreditHoursAttempted = totalCreditHours;
            existing.Classification = classification;
            existing.ComputedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<SemesterResult?> GetSemesterResultAsync(Guid studentId, Guid semesterId) =>
        await _context.SemesterResults
            .Include(r => r.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicSemesterId == semesterId);

    public async Task<CumulativeResult?> GetCumulativeResultAsync(Guid studentId, Guid academicYearId) =>
        await _context.CumulativeResults
            .Include(r => r.AcademicYear)
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicYearId == academicYearId);

    public async Task<List<SemesterResult>> GetStudentSemesterResultsAsync(Guid studentId) =>
        await _context.SemesterResults
            .Include(r => r.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .Where(r => r.StudentId == studentId && r.IsPublished && r.IsDeleted != true)
            .OrderBy(r => r.AcademicSemester!.AcademicYear!.Year).ThenBy(r => r.AcademicSemester!.SemesterName)
            .ToListAsync();

    public async Task<bool> PublishResultsAsync(Guid semesterId)
    {
        var results = await _context.SemesterResults
            .Where(r => r.AcademicSemesterId == semesterId && !r.IsWithheld)
            .ToListAsync();
        foreach (var r in results) r.IsPublished = true;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> WithholdResultAsync(Guid studentId, Guid semesterId, string reason)
    {
        var result = await _context.SemesterResults
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicSemesterId == semesterId);
        if (result is null) return false;
        result.IsWithheld = true;
        result.WithholdReason = reason;
        result.IsPublished = false;
        return await _context.SaveChangesAsync() > 0;
    }
}
