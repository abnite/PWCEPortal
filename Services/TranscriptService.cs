using System.Text;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class TranscriptService : ITranscriptService
{
    private readonly PortalDbContext _context;
    private readonly IAssessmentStructureService _structureService;
    private readonly IGPAService _gpaService;

    public TranscriptService(PortalDbContext context, IAssessmentStructureService structureService, IGPAService gpaService)
    {
        _context = context;
        _structureService = structureService;
        _gpaService = gpaService;
    }

    public async Task<byte[]> GenerateTranscriptPdfAsync(
        Guid studentId, TranscriptType type, string requestedByUserId, string? purpose = null)
    {
        var student = await _context.Students
            .Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        var semesterResults = await _context.SemesterResults
            .Include(r => r.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .Where(r => r.StudentId == studentId && r.IsPublished && r.IsDeleted != true)
            .OrderBy(r => r.AcademicSemester!.AcademicYear!.Year).ThenBy(r => r.AcademicSemester!.SemesterName)
            .ToListAsync();

        var latestCumulative = await _context.CumulativeResults
            .Include(r => r.AcademicYear)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.ComputedAt)
            .FirstOrDefaultAsync();

        // Load per-course breakdown
        var courseResultsBySemester = await _gpaService.GetCourseResultsBySemesterAsync(studentId);

        var sb = new StringBuilder();
        const int W = 80;

        sb.AppendLine(Center("PWCE SCHOOL PORTAL", W));
        sb.AppendLine(Center("OFFICIAL ACADEMIC TRANSCRIPT", W));
        sb.AppendLine(new string('=', W));

        if (student != null)
        {
            sb.AppendLine($"Student Name  : {student.Surname} {student.OtherNames}");
            sb.AppendLine($"Student ID    : {student.StudentID}");
            sb.AppendLine($"Programme     : {student.CollegeProgram?.ProgramName}");
            sb.AppendLine($"Level of Entry: {student.LevelOfEntry}");
            sb.AppendLine($"Enrolment Year: {student.EnrolmentYear}");
        }
        sb.AppendLine($"Generated     : {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");
        sb.AppendLine($"Type          : {(type == TranscriptType.Official ? "Official" : "Unofficial")}");
        if (!string.IsNullOrEmpty(purpose))
            sb.AppendLine($"Purpose       : {purpose}");

        sb.AppendLine(new string('=', W));
        sb.AppendLine();

        // Group by academic year
        var byYear = semesterResults
            .GroupBy(r => r.AcademicSemester?.AcademicYear?.Year ?? "Unknown")
            .OrderBy(g => g.Key);

        decimal cumulativeWeightedGP = 0;
        int cumulativeCreditHours = 0;

        foreach (var yearGroup in byYear)
        {
            sb.AppendLine($"YEAR: {yearGroup.Key}");
            sb.AppendLine(new string('-', W));

            foreach (var sem in yearGroup)
            {
                sb.AppendLine($"  Semester: {sem.AcademicSemester?.SemesterName}");
                sb.AppendLine();

                // Column header
                sb.AppendLine(
                    $"  {"Course Code",-12} {"Course Name",-35} {"Cr",3}  {"Score%",7}  {"Grd",4}  {"GP",5}  {"QP",7}");
                sb.AppendLine("  " + new string('-', W - 2));

                var courses = courseResultsBySemester.TryGetValue(sem.AcademicSemesterId, out var cl)
                    ? cl
                    : new List<ViewModel.Results.CourseResultViewModel>();

                decimal semQP = 0;
                foreach (var c in courses)
                {
                    decimal qp = c.GradePoint * c.CreditHours;
                    semQP += qp;
                    sb.AppendLine(
                        $"  {c.CourseCode,-12} {Truncate(c.CourseName, 35),-35} {c.CreditHours,3}  {c.TotalScore,7:F1}  {c.GradeLetter,4}  {c.GradePoint,5:F2}  {qp,7:F2}");
                }

                sb.AppendLine("  " + new string('-', W - 2));
                sb.AppendLine($"  {"Semester Total",-48} {sem.TotalCreditHours,3}  {"",7}  {"",4}  {"",5}  {semQP,7:F2}");
                sb.AppendLine($"  Semester GPA: {sem.GPA:F2}");

                cumulativeWeightedGP += semQP;
                cumulativeCreditHours += sem.TotalCreditHours;
                decimal runningCGPA = cumulativeCreditHours > 0
                    ? Math.Round(cumulativeWeightedGP / cumulativeCreditHours, 2)
                    : 0;
                sb.AppendLine($"  Cumulative GPA: {runningCGPA:F2}");
                sb.AppendLine();
            }
        }

        // Summary
        sb.AppendLine(new string('=', W));
        sb.AppendLine(Center("SUMMARY", W));
        sb.AppendLine(new string('=', W));
        sb.AppendLine($"Total Credit Hours Attempted : {latestCumulative?.TotalCreditHoursAttempted ?? cumulativeCreditHours}");
        sb.AppendLine($"Total Credit Hours Earned    : {latestCumulative?.TotalCreditHoursEarned ?? cumulativeCreditHours}");
        decimal finalCGPA = latestCumulative?.CGPA ??
            (cumulativeCreditHours > 0 ? Math.Round(cumulativeWeightedGP / cumulativeCreditHours, 2) : 0);
        sb.AppendLine($"Cumulative GPA               : {finalCGPA:F2}");
        sb.AppendLine($"Classification               : {latestCumulative?.Classification ?? "-"}");
        sb.AppendLine(new string('=', W));

        // Log the request
        var request = new TranscriptRequest
        {
            StudentId = studentId,
            TranscriptType = type,
            RequestedById = requestedByUserId,
            RequestedAt = DateTime.UtcNow,
            Purpose = purpose
        };
        _context.TranscriptRequests.Add(request);
        await _context.SaveChangesAsync();

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<List<TranscriptRequest>> GetTranscriptLogsAsync(Guid? studentId = null)
    {
        var query = _context.TranscriptRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedBy)
            .Where(r => r.IsDeleted != true);

        if (studentId.HasValue)
            query = query.Where(r => r.StudentId == studentId.Value);

        return await query.OrderByDescending(r => r.RequestedAt).ToListAsync();
    }

    public async Task<TranscriptRequest?> GetRequestByIdAsync(Guid id) =>
        await _context.TranscriptRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedBy)
            .FirstOrDefaultAsync(r => r.Id == id);

    private static string Center(string text, int width)
    {
        int padding = Math.Max(0, (width - text.Length) / 2);
        return new string(' ', padding) + text;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}
