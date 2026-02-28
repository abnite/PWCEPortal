using System.Text;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class TranscriptService : ITranscriptService
{
    private readonly PortalDbContext _context;

    public TranscriptService(PortalDbContext context)
    {
        _context = context;
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

        // Generate simple HTML-based transcript content (rendered as text bytes)
        var sb = new StringBuilder();
        sb.AppendLine("PWCE SCHOOL PORTAL");
        sb.AppendLine("OFFICIAL ACADEMIC TRANSCRIPT");
        sb.AppendLine(new string('=', 60));
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
        sb.AppendLine(new string('-', 60));
        sb.AppendLine("SEMESTER RESULTS");
        sb.AppendLine(new string('-', 60));

        foreach (var result in semesterResults)
        {
            sb.AppendLine($"{result.AcademicSemester?.AcademicYear?.Year} – {result.AcademicSemester?.SemesterName}");
            sb.AppendLine($"  GPA: {result.GPA:F2}  |  Credit Hours: {result.TotalCreditHours}");
        }

        if (latestCumulative != null)
        {
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"CGPA: {latestCumulative.CGPA:F2}  |  Classification: {latestCumulative.Classification}");
        }

        sb.AppendLine(new string('=', 60));

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
}
