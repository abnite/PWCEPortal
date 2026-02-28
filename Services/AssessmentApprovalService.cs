using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class AssessmentApprovalService : IAssessmentApprovalService
{
    private readonly PortalDbContext _context;

    public AssessmentApprovalService(PortalDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssessmentSubmission>> GetPendingSubmissionsAsync(string userId)
    {
        return await _context.AssessmentSubmissions
            .Include(s => s.CourseLecturerAssignment).ThenInclude(a => a!.Course)
            .Include(s => s.CourseLecturerAssignment).ThenInclude(a => a!.Lecturer).ThenInclude(l => l!.User)
            .Include(s => s.CourseLecturerAssignment).ThenInclude(a => a!.AcademicSemester)
            .Where(s => s.IsDeleted != true
                     && s.Status != SubmissionStatus.Draft
                     && s.Status != SubmissionStatus.PrincipalApproved)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<AssessmentSubmission?> GetSubmissionByIdAsync(Guid id) =>
        await _context.AssessmentSubmissions
            .Include(s => s.CourseLecturerAssignment).ThenInclude(a => a!.Course)
            .Include(s => s.CourseLecturerAssignment).ThenInclude(a => a!.Lecturer).ThenInclude(l => l!.User)
            .Include(s => s.CourseLecturerAssignment).ThenInclude(a => a!.AcademicSemester)
            .Include(s => s.ApprovalLogs)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted != true);

    public async Task<bool> SubmitForReviewAsync(Guid assignmentId, string lecturerUserId)
    {
        var existing = await _context.AssessmentSubmissions
            .FirstOrDefaultAsync(s => s.CourseLecturerAssignmentId == assignmentId && s.IsDeleted != true);

        if (existing is null)
        {
            existing = new AssessmentSubmission { CourseLecturerAssignmentId = assignmentId };
            _context.AssessmentSubmissions.Add(existing);
        }

        existing.Status = SubmissionStatus.SubmittedForReview;
        existing.SubmittedAt = DateTime.UtcNow;

        _context.AssessmentApprovalLogs.Add(new AssessmentApprovalLog
        {
            AssessmentSubmissionId = existing.Id,
            ActorId = lecturerUserId,
            Action = "Submitted for Review",
            ActionDate = DateTime.UtcNow
        });

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> HODReviewAsync(Guid submissionId, string hodUserId, bool approve, string comments)
    {
        var submission = await GetSubmissionByIdAsync(submissionId);
        if (submission is null) return false;

        submission.HODReviewerId = hodUserId;
        submission.HODReviewedAt = DateTime.UtcNow;
        submission.HODComments = comments;
        submission.Status = approve ? SubmissionStatus.HODApproved : SubmissionStatus.HODRejected;

        _context.AssessmentApprovalLogs.Add(new AssessmentApprovalLog
        {
            AssessmentSubmissionId = submissionId,
            ActorId = hodUserId,
            Action = approve ? "HOD Approved" : "HOD Rejected",
            Comments = comments,
            ActionDate = DateTime.UtcNow
        });

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> QAReviewAsync(Guid submissionId, string qaUserId, bool approve, string comments)
    {
        var submission = await GetSubmissionByIdAsync(submissionId);
        if (submission is null) return false;

        submission.QAReviewerId = qaUserId;
        submission.QAReviewedAt = DateTime.UtcNow;
        submission.QAComments = comments;
        submission.Status = approve ? SubmissionStatus.QAApproved : SubmissionStatus.QAFlagged;

        _context.AssessmentApprovalLogs.Add(new AssessmentApprovalLog
        {
            AssessmentSubmissionId = submissionId,
            ActorId = qaUserId,
            Action = approve ? "QA Approved" : "QA Flagged",
            Comments = comments,
            ActionDate = DateTime.UtcNow
        });

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> PrincipalApproveAsync(Guid submissionId, string principalUserId, string comments)
    {
        var submission = await GetSubmissionByIdAsync(submissionId);
        if (submission is null) return false;

        submission.PrincipalReviewerId = principalUserId;
        submission.PrincipalReviewedAt = DateTime.UtcNow;
        submission.PrincipalComments = comments;
        submission.Status = SubmissionStatus.PrincipalApproved;

        _context.AssessmentApprovalLogs.Add(new AssessmentApprovalLog
        {
            AssessmentSubmissionId = submissionId,
            ActorId = principalUserId,
            Action = "Principal Approved",
            Comments = comments,
            ActionDate = DateTime.UtcNow
        });

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UnlockAssessmentAsync(Guid submissionId, string principalUserId, string reason)
    {
        var submission = await GetSubmissionByIdAsync(submissionId);
        if (submission is null) return false;

        submission.Status = SubmissionStatus.Unlocked;

        _context.AssessmentApprovalLogs.Add(new AssessmentApprovalLog
        {
            AssessmentSubmissionId = submissionId,
            ActorId = principalUserId,
            Action = "Unlocked for Correction",
            Comments = reason,
            ActionDate = DateTime.UtcNow
        });

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<AssessmentApprovalLog>> GetApprovalLogsAsync(Guid submissionId) =>
        await _context.AssessmentApprovalLogs
            .Include(l => l.Actor)
            .Where(l => l.AssessmentSubmissionId == submissionId)
            .OrderBy(l => l.ActionDate)
            .ToListAsync();
}
