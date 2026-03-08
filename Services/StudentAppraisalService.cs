using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Staff;

namespace PWCEPortal.Services;

public class StudentAppraisalService : IStudentAppraisalService
{
    private readonly PortalDbContext _context;

    public StudentAppraisalService(PortalDbContext context)
    {
        _context = context;
    }

    // ── HOD: Session Management ──────────────────────────────────────────────

    public async Task<TeachingAppraisalSession> CreateSessionAsync(
        Guid templateId, Guid assignmentId, Guid academicYearId, string hodUserId)
    {
        var template = await _context.AppraisalTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        var session = new TeachingAppraisalSession
        {
            AppraisalTemplateId = templateId,
            CourseLecturerAssignmentId = assignmentId,
            AcademicYearId = academicYearId,
            CreatedByHODId = hodUserId,
            IsOpen = true
        };

        _context.TeachingAppraisalSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<List<TeachingAppraisalSession>> GetSessionsByHODAsync(string hodUserId) =>
        await _context.TeachingAppraisalSessions
            .Include(s => s.AppraisalTemplate)
            .Include(s => s.Assignment).ThenInclude(a => a!.Course)
            .Include(s => s.Assignment).ThenInclude(a => a!.Lecturer).ThenInclude(l => l!.User)
            .Include(s => s.Assignment).ThenInclude(a => a!.AcademicSemester).ThenInclude(sem => sem!.AcademicYear)
            .Include(s => s.AcademicYear)
            .Include(s => s.Submissions)
            .Where(s => s.CreatedByHODId == hodUserId && s.IsDeleted != true)
            .OrderByDescending(s => s.DateAdded)
            .ToListAsync();

    public async Task<List<TeachingAppraisalSession>> GetSessionsByDepartmentAsync(Guid departmentId) =>
        await _context.TeachingAppraisalSessions
            .Include(s => s.AppraisalTemplate)
            .Include(s => s.Assignment).ThenInclude(a => a!.Course)
            .Include(s => s.Assignment).ThenInclude(a => a!.Lecturer).ThenInclude(l => l!.User)
            .Include(s => s.Assignment).ThenInclude(a => a!.AcademicSemester)
            .Include(s => s.AcademicYear)
            .Include(s => s.Submissions)
            .Where(s => s.Assignment!.Lecturer!.DepartmentId == departmentId && s.IsDeleted != true)
            .OrderByDescending(s => s.DateAdded)
            .ToListAsync();

    public async Task<TeachingAppraisalSession?> GetSessionByIdAsync(Guid sessionId) =>
        await _context.TeachingAppraisalSessions
            .Include(s => s.AppraisalTemplate).ThenInclude(t => t!.Criteria)
            .Include(s => s.Assignment).ThenInclude(a => a!.Course)
            .Include(s => s.Assignment).ThenInclude(a => a!.Lecturer).ThenInclude(l => l!.User)
            .Include(s => s.Assignment).ThenInclude(a => a!.AcademicSemester)
            .Include(s => s.AcademicYear)
            .Include(s => s.Submissions).ThenInclude(sub => sub.Scores)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsDeleted != true);

    public async Task<bool> CloseSessionAsync(Guid sessionId)
    {
        var session = await _context.TeachingAppraisalSessions.FindAsync(sessionId);
        if (session is null) return false;
        session.IsOpen = false;
        session.ClosedAt = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> CreateBulkSessionsAsync(Guid templateId, Guid academicYearId, string hodUserId, Guid? departmentId)
    {
        // Fetch all active assignments, scoped to department if provided
        var query = _context.CourseLecturerAssignments
            .Where(a => a.IsDeleted != true);

        if (departmentId.HasValue)
            query = query.Where(a => a.Lecturer!.DepartmentId == departmentId.Value);

        var assignments = await query.Select(a => a.Id).ToListAsync();

        // Find assignments that already have an open session for this academic year
        var existingAssignmentIds = await _context.TeachingAppraisalSessions
            .Where(s => s.AcademicYearId == academicYearId && s.IsOpen && s.IsDeleted != true)
            .Select(s => s.CourseLecturerAssignmentId)
            .ToListAsync();

        var toCreate = assignments.Except(existingAssignmentIds).ToList();

        if (!toCreate.Any()) return 0;

        var sessions = toCreate.Select(assignmentId => new TeachingAppraisalSession
        {
            AppraisalTemplateId = templateId,
            CourseLecturerAssignmentId = assignmentId,
            AcademicYearId = academicYearId,
            CreatedByHODId = hodUserId,
            IsOpen = true
        }).ToList();

        _context.TeachingAppraisalSessions.AddRange(sessions);
        await _context.SaveChangesAsync();
        return sessions.Count;
    }

    public async Task<bool> ReopenSessionAsync(Guid sessionId)
    {
        var session = await _context.TeachingAppraisalSessions.FindAsync(sessionId);
        if (session is null) return false;
        session.IsOpen = true;
        session.ClosedAt = null;
        return await _context.SaveChangesAsync() > 0;
    }

    // ── Student: Fill In ─────────────────────────────────────────────────────

    public async Task<List<TeachingAppraisalSession>> GetOpenSessionsForStudentAsync(Guid studentId)
    {
        // Get courses the student is currently registered in
        var registeredCourseIds = await _context.StudentCourseRegistrations
            .Where(r => r.StudentId == studentId && r.IsRegistered)
            .Select(r => r.CourseId)
            .ToListAsync();

        // Get open sessions for those courses
        return await _context.TeachingAppraisalSessions
            .Include(s => s.AppraisalTemplate)
            .Include(s => s.Assignment).ThenInclude(a => a!.Course)
            .Include(s => s.Assignment).ThenInclude(a => a!.Lecturer).ThenInclude(l => l!.User)
            .Include(s => s.Submissions.Where(sub => sub.StudentId == studentId))
            .Where(s => s.IsOpen
                     && s.IsDeleted != true
                     && registeredCourseIds.Contains(s.Assignment!.CourseId))
            .OrderByDescending(s => s.DateAdded)
            .ToListAsync();
    }

    public async Task<StudentAppraisalSubmission?> GetSubmissionAsync(Guid sessionId, Guid studentId) =>
        await _context.StudentAppraisalSubmissions
            .Include(sub => sub.Scores).ThenInclude(sc => sc.Criterion)
            .Include(sub => sub.Session).ThenInclude(s => s!.AppraisalTemplate).ThenInclude(t => t!.Criteria)
            .FirstOrDefaultAsync(sub => sub.SessionId == sessionId
                                     && sub.StudentId == studentId
                                     && sub.IsDeleted != true);

    public async Task<bool> SaveSubmissionAsync(
        Guid sessionId, Guid studentId,
        Dictionary<Guid, decimal> scores, string? remarks, bool submit)
    {
        var submission = await _context.StudentAppraisalSubmissions
            .Include(sub => sub.Scores)
            .FirstOrDefaultAsync(sub => sub.SessionId == sessionId && sub.StudentId == studentId);

        if (submission is null)
        {
            submission = new StudentAppraisalSubmission
            {
                SessionId = sessionId,
                StudentId = studentId
            };
            _context.StudentAppraisalSubmissions.Add(submission);
        }
        else
        {
            // Remove old scores
            _context.StudentAppraisalScores.RemoveRange(submission.Scores ?? new List<StudentAppraisalScore>());
        }

        foreach (var (criterionId, score) in scores)
        {
            _context.StudentAppraisalScores.Add(new StudentAppraisalScore
            {
                SubmissionId = submission.Id,
                AppraisalCriterionId = criterionId,
                Score = score
            });
        }

        submission.TotalScore = scores.Values.Sum();
        submission.Remarks = remarks;

        if (submit)
        {
            submission.IsSubmitted = true;
            submission.SubmittedAt = DateTime.UtcNow;
        }

        return await _context.SaveChangesAsync() > 0;
    }

    // ── Results ──────────────────────────────────────────────────────────────

    public async Task<List<StudentAppraisalSubmission>> GetSessionSubmissionsAsync(Guid sessionId) =>
        await _context.StudentAppraisalSubmissions
            .Include(sub => sub.Student)
            .Include(sub => sub.Scores).ThenInclude(sc => sc.Criterion)
            .Where(sub => sub.SessionId == sessionId && sub.IsSubmitted && sub.IsDeleted != true)
            .ToListAsync();
}
