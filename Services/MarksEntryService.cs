using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class MarksEntryService : IMarksEntryService
{
    private readonly PortalDbContext _context;

    public MarksEntryService(PortalDbContext context)
    {
        _context = context;
    }

    public async Task<List<CourseLecturerAssignment>> GetAssignmentsForLecturerAsync(string userId, Guid semesterId)
    {
        var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == userId && l.IsDeleted != true);
        if (lecturer is null) return new List<CourseLecturerAssignment>();

        return await _context.CourseLecturerAssignments
            .Include(a => a.Course)
            .Include(a => a.AcademicSemester)
            .Where(a => a.LecturerId == lecturer.Id && a.AcademicSemesterId == semesterId && a.IsDeleted != true)
            .ToListAsync();
    }

    public async Task<List<StudentMark>> GetMarksForAssignmentAsync(Guid assignmentId) =>
        await _context.StudentMarks
            .Include(m => m.Student)
            .Include(m => m.AssessmentComponent)
            .Where(m => m.CourseLecturerAssignmentId == assignmentId && m.IsDeleted != true)
            .OrderBy(m => m.Student!.Surname).ThenBy(m => m.AssessmentComponent!.DisplayOrder)
            .ToListAsync();

    public async Task<StudentMark?> GetMarkAsync(Guid assignmentId, Guid studentId, Guid componentId) =>
        await _context.StudentMarks
            .FirstOrDefaultAsync(m => m.CourseLecturerAssignmentId == assignmentId
                                   && m.StudentId == studentId
                                   && m.AssessmentComponentId == componentId
                                   && m.IsDeleted != true);

    public async Task<bool> SaveMarkAsync(StudentMark mark)
    {
        var existing = await GetMarkAsync(mark.CourseLecturerAssignmentId, mark.StudentId, mark.AssessmentComponentId);
        if (existing is null)
            _context.StudentMarks.Add(mark);
        else
        {
            existing.Score = mark.Score;
            existing.Remarks = mark.Remarks;
        }
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SaveMarksAsync(List<StudentMark> marks)
    {
        foreach (var mark in marks)
            await SaveMarkAsync(mark);
        return true;
    }

    public async Task<(int saved, int errors, List<string> errorMessages)> BulkUploadMarksFromExcelAsync(
        Guid assignmentId, Stream fileStream)
    {
        int saved = 0, errors = 0;
        var errorMessages = new List<string>();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(fileStream);
        var sheet = package.Workbook.Worksheets.FirstOrDefault();
        if (sheet is null) return (0, 1, new List<string> { "Excel file is empty or has no worksheets." });

        // Expected columns: StudentID | ComponentName | Score | Remarks
        int rowCount = sheet.Dimension?.Rows ?? 0;
        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                var studentIdStr = sheet.Cells[row, 1].Text?.Trim();
                var componentName = sheet.Cells[row, 2].Text?.Trim();
                var scoreStr = sheet.Cells[row, 3].Text?.Trim();
                var remarks = sheet.Cells[row, 4].Text?.Trim();

                if (string.IsNullOrWhiteSpace(studentIdStr) || string.IsNullOrWhiteSpace(componentName))
                {
                    errors++;
                    errorMessages.Add($"Row {row}: StudentID or ComponentName is missing.");
                    continue;
                }

                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == studentIdStr);
                if (student is null)
                {
                    errors++;
                    errorMessages.Add($"Row {row}: Student '{studentIdStr}' not found.");
                    continue;
                }

                var assignment = await _context.CourseLecturerAssignments
                    .Include(a => a.Course)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);
                if (assignment is null)
                {
                    errors++;
                    errorMessages.Add($"Row {row}: Assignment not found.");
                    continue;
                }

                // Find component – look via structure linked to the course/programme/level
                var component = await _context.AssessmentComponents
                    .Include(c => c.AssessmentStructure)
                    .Where(c => c.ComponentName == componentName
                             && c.AssessmentStructure!.IsDeleted != true
                             && (c.AssessmentStructure.CollegeProgramId == assignment.Course!.CollegeProgramId
                                 || c.AssessmentStructure.CollegeProgramId == null)
                             && c.IsDeleted != true)
                    .FirstOrDefaultAsync();

                if (component is null)
                {
                    errors++;
                    errorMessages.Add($"Row {row}: Assessment component '{componentName}' not found.");
                    continue;
                }

                decimal score = 0;
                if (!string.IsNullOrEmpty(scoreStr) && !decimal.TryParse(scoreStr, out score))
                {
                    errors++;
                    errorMessages.Add($"Row {row}: Invalid score '{scoreStr}'.");
                    continue;
                }

                var mark = new StudentMark
                {
                    CourseLecturerAssignmentId = assignmentId,
                    StudentId = student.Id,
                    AssessmentComponentId = component.Id,
                    Score = score,
                    Remarks = remarks
                };

                await SaveMarkAsync(mark);
                saved++;
            }
            catch (Exception ex)
            {
                errors++;
                errorMessages.Add($"Row {row}: Unexpected error – {ex.Message}");
            }
        }

        return (saved, errors, errorMessages);
    }

    public async Task<bool> MarksDraftExistsAsync(Guid assignmentId) =>
        await _context.StudentMarks.AnyAsync(m => m.CourseLecturerAssignmentId == assignmentId && m.IsDeleted != true);

    public async Task<AssessmentSubmission?> GetSubmissionForAssignmentAsync(Guid assignmentId) =>
        await _context.AssessmentSubmissions
            .FirstOrDefaultAsync(s => s.CourseLecturerAssignmentId == assignmentId && s.IsDeleted != true);
}
