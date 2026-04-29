using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.Payment;
using PWCEPortal.ViewModel.Results;

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

        // Only include assignments that are PrincipalApproved AND have all marks filled
        var assignments = await _context.CourseLecturerAssignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .Where(a => a.AcademicSemesterId == semesterId
                     && a.IsDeleted != true
                     && a.Submissions!.Any(s => s.Status == SubmissionStatus.PrincipalApproved))
            .ToListAsync();

        // Filter to only fully-complete assignments (no missing marks for any registered student)
        var allStructures = await _context.AssessmentStructures
            .Include(s => s.Components)
            .Where(s => s.IsDeleted != true)
            .ToListAsync();

        var completeAssignments = new List<CourseLecturerAssignment>();
        foreach (var a in assignments)
        {
            var structure = ResolveStructure(allStructures, a.Course!.CollegeProgramId, a.Course.Level);
            var componentIds = structure?.Components?.Where(c => c.IsDeleted != true)
                                   .Select(c => c.Id).ToList() ?? new();
            if (!componentIds.Any()) { completeAssignments.Add(a); continue; }

            var studentIds = await _context.StudentCourseRegistrations
                .Where(r => r.CourseId == a.CourseId && r.SemesterId == semesterId && r.IsRegistered)
                .Select(r => r.StudentId).ToListAsync();

            int expected = studentIds.Count * componentIds.Count;
            int actual = await _context.StudentMarks
                .CountAsync(m => m.CourseLecturerAssignmentId == a.Id
                              && m.Score != null
                              && m.IsDeleted != true);

            if (actual >= expected) completeAssignments.Add(a);
        }

        assignments = completeAssignments;

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

    public async Task<bool> UnWithholdResultAsync(Guid studentId, Guid semesterId)
    {
        var result = await _context.SemesterResults
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicSemesterId == semesterId);
        if (result is null) return false;
        result.IsWithheld = false;
        result.WithholdReason = null;
        result.IsPublished = true;
        return await _context.SaveChangesAsync() > 0;
    }

    private static AssessmentStructure? ResolveStructure(List<AssessmentStructure> all, Guid? programId, int? level) =>
        all.FirstOrDefault(s => s.CollegeProgramId == programId && s.ApplicableLevel == level)
        ?? all.FirstOrDefault(s => s.CollegeProgramId == programId && s.ApplicableLevel == null)
        ?? all.FirstOrDefault(s => s.CollegeProgramId == null && s.ApplicableLevel == level)
        ?? all.FirstOrDefault(s => s.IsDefault)
        ?? all.FirstOrDefault(s => s.CollegeProgramId == null && s.ApplicableLevel == null);

    public async Task<bool> PublishInterimAsync(Guid assignmentId)
    {
        var submission = await _context.AssessmentSubmissions
            .FirstOrDefaultAsync(s => s.CourseLecturerAssignmentId == assignmentId
                                   && s.Status == SubmissionStatus.PrincipalApproved
                                   && s.IsDeleted != true);
        if (submission is null) return false;
        submission.IsInterimPublished = true;
        submission.InterimPublishedAt = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<InterimCourseResultViewModel>> GetInterimCourseResultsAsync(Guid studentId)
    {
        // Semester IDs already finally published for this student
        var publishedSemIds = await _context.SemesterResults
            .Where(r => r.StudentId == studentId && r.IsPublished && r.IsDeleted != true)
            .Select(r => r.AcademicSemesterId).ToListAsync();

        // Registrations for semesters NOT yet finally published
        var registrations = await _context.StudentCourseRegistrations
            .Include(r => r.Course)
            .Where(r => r.StudentId == studentId && r.IsRegistered
                     && r.SemesterId.HasValue && !publishedSemIds.Contains(r.SemesterId!.Value))
            .ToListAsync();

        if (!registrations.Any()) return new();

        var semIds = registrations.Select(r => r.SemesterId!.Value).Distinct().ToList();

        // Assignments that are interim-published for those semesters
        var assignments = await _context.CourseLecturerAssignments
            .Include(a => a.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .Include(a => a.Submissions)
            .Where(a => semIds.Contains(a.AcademicSemesterId)
                     && a.IsDeleted != true
                     && a.Submissions!.Any(s => s.IsInterimPublished && s.IsDeleted != true))
            .ToListAsync();

        if (!assignments.Any()) return new();

        var allStructures = await _context.AssessmentStructures
            .Include(s => s.Components)
            .Where(s => s.IsDeleted != true).ToListAsync();

        var allMarks = await _context.StudentMarks
            .Where(m => m.StudentId == studentId && m.IsDeleted != true).ToListAsync();

        var result = new List<InterimCourseResultViewModel>();
        foreach (var reg in registrations)
        {
            if (reg.Course is null) continue;
            var assignment = assignments.FirstOrDefault(a =>
                a.CourseId == reg.CourseId && a.AcademicSemesterId == reg.SemesterId);
            if (assignment is null) continue;

            var structure = ResolveStructure(allStructures, reg.Course.CollegeProgramId, reg.Course.Level);
            var components = structure?.Components?.Where(c => c.IsDeleted != true)
                                 .OrderBy(c => c.DisplayOrder).ToList() ?? new();
            var studentMarks = allMarks.Where(m => m.CourseLecturerAssignmentId == assignment.Id).ToList();

            var compRows = new List<InterimComponentResultViewModel>();
            decimal availableWeighted = 0;
            decimal availableWeight = 0;

            foreach (var comp in components)
            {
                var mark = studentMarks.FirstOrDefault(m => m.AssessmentComponentId == comp.Id);
                decimal? weighted = mark?.Score != null
                    ? (mark.Score.Value / comp.MaxScore) * comp.WeightPercent
                    : null;

                if (weighted.HasValue) { availableWeighted += weighted.Value; availableWeight += comp.WeightPercent; }

                compRows.Add(new InterimComponentResultViewModel
                {
                    ComponentName = comp.ComponentName,
                    WeightPercent = comp.WeightPercent,
                    MaxScore = comp.MaxScore,
                    Score = mark?.Score,
                    WeightedContribution = weighted
                });
            }

            result.Add(new InterimCourseResultViewModel
            {
                CourseCode = reg.Course.CourseCode ?? "",
                CourseName = reg.Course.CourseName ?? "",
                SemesterId = reg.SemesterId!.Value,
                SemesterName = assignment.AcademicSemester?.SemesterName ?? "",
                AcademicYear = assignment.AcademicSemester?.AcademicYear?.Year?.ToString() ?? "",
                Components = compRows,
                AvailableWeightedScore = Math.Round(availableWeighted, 1),
                AvailableTotalWeight = availableWeight
            });
        }

        return result.OrderBy(r => r.AcademicYear).ThenBy(r => r.SemesterName).ThenBy(r => r.CourseCode).ToList();
    }

    public async Task<bool> HasSufficientFeePaymentAsync(Guid studentId)
    {
        var assignments = await _context.StudentFeeAssignments
            .Where(f => f.StudentId == studentId && f.IsDeleted != true)
            .ToListAsync();
        if (!assignments.Any()) return false;
        decimal totalFee = assignments.Sum(f => f.FullFee);
        decimal totalPaid = assignments.Sum(f => f.PaymentMade);
        return totalFee > 0 && (totalPaid / totalFee) >= 0.70m;
    }

    public async Task<List<PendingInterimSubmissionViewModel>> GetPendingSubmissionsForSemesterAsync(Guid semesterId)
    {
        var assignments = await _context.CourseLecturerAssignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .Where(a => a.AcademicSemesterId == semesterId
                     && a.IsDeleted != true
                     && a.Submissions!.Any(s => s.Status == SubmissionStatus.PrincipalApproved
                                             && s.IsDeleted != true))
            .ToListAsync();

        var allStructures = await _context.AssessmentStructures
            .Include(s => s.Components)
            .Where(s => s.IsDeleted != true).ToListAsync();

        var result = new List<PendingInterimSubmissionViewModel>();
        foreach (var a in assignments)
        {
            var submission = a.Submissions!.First(s => s.Status == SubmissionStatus.PrincipalApproved);
            var structure = ResolveStructure(allStructures, a.Course!.CollegeProgramId, a.Course.Level);
            var componentIds = structure?.Components?.Where(c => c.IsDeleted != true)
                                   .Select(c => c.Id).ToList() ?? new();

            var studentIds = await _context.StudentCourseRegistrations
                .Where(r => r.CourseId == a.CourseId && r.SemesterId == semesterId && r.IsRegistered)
                .Select(r => r.StudentId).ToListAsync();

            int expected = studentIds.Count * componentIds.Count;
            int actual = expected > 0 ? await _context.StudentMarks
                .CountAsync(m => m.CourseLecturerAssignmentId == a.Id
                              && m.Score != null && m.IsDeleted != true) : 0;

            result.Add(new PendingInterimSubmissionViewModel
            {
                AssignmentId = a.Id,
                CourseName = a.Course.CourseName ?? "",
                CourseCode = a.Course.CourseCode ?? "",
                IsInterimPublished = submission.IsInterimPublished,
                IsComplete = expected > 0 && actual >= expected
            });
        }

        return result.OrderBy(r => r.CourseName).ToList();
    }

    public async Task<Dictionary<Guid, List<CourseResultViewModel>>> GetCourseResultsBySemesterAsync(Guid studentId)
    {
        var gradingScale = await _structureService.GetDefaultGradingScaleAsync();

        // Get published semester IDs for this student
        var publishedSemesterIds = await _context.SemesterResults
            .Where(r => r.StudentId == studentId && r.IsPublished && r.IsDeleted != true)
            .Select(r => r.AcademicSemesterId)
            .ToListAsync();

        var result = new Dictionary<Guid, List<CourseResultViewModel>>();
        if (!publishedSemesterIds.Any()) return result;

        // Get all registrations for published semesters
        var registrations = await _context.StudentCourseRegistrations
            .Include(r => r.Course)
            .Where(r => r.StudentId == studentId
                     && r.SemesterId.HasValue
                     && publishedSemesterIds.Contains(r.SemesterId!.Value)
                     && r.IsRegistered)
            .ToListAsync();

        // Load all assessment structures
        var allStructures = await _context.AssessmentStructures
            .Include(s => s.Components)
            .Where(s => s.IsDeleted != true)
            .ToListAsync();

        // Load all marks for this student
        var allMarks = await _context.StudentMarks
            .Where(m => m.StudentId == studentId && m.IsDeleted != true)
            .ToListAsync();

        // Load all approved assignments for these semesters
        var assignments = await _context.CourseLecturerAssignments
            .Where(a => publishedSemesterIds.Contains(a.AcademicSemesterId) && a.IsDeleted != true)
            .ToListAsync();

        foreach (var semId in publishedSemesterIds)
        {
            var semRegs = registrations.Where(r => r.SemesterId == semId).ToList();
            var courseResults = new List<CourseResultViewModel>();

            foreach (var reg in semRegs)
            {
                if (reg.Course is null) continue;
                var assignment = assignments.FirstOrDefault(a => a.CourseId == reg.CourseId && a.AcademicSemesterId == semId);
                if (assignment is null) continue;

                var structure = ResolveStructure(allStructures, reg.Course.CollegeProgramId, reg.Course.Level);

                var components = structure?.Components?.Where(c => c.IsDeleted != true).ToList() ?? new();
                var studentMarks = allMarks.Where(m => m.CourseLecturerAssignmentId == assignment.Id).ToList();

                decimal weightedScore = 0;
                foreach (var comp in components)
                {
                    var mark = studentMarks.FirstOrDefault(m => m.AssessmentComponentId == comp.Id);
                    if (mark?.Score != null)
                        weightedScore += (mark.Score.Value / comp.MaxScore) * comp.WeightPercent;
                }

                string gradeLetter = "-";
                decimal gradePoint = 0;
                if (gradingScale != null)
                {
                    var grade = await _structureService.GetGradeForScoreAsync(gradingScale.Id, weightedScore);
                    if (grade != null) { gradeLetter = grade.GradeLetter; gradePoint = grade.GradePoint; }
                }

                courseResults.Add(new CourseResultViewModel
                {
                    CourseCode = reg.Course.CourseCode,
                    CourseName = reg.Course.CourseName,
                    CreditHours = 3,
                    TotalScore = Math.Round(weightedScore, 1),
                    GradeLetter = gradeLetter,
                    GradePoint = gradePoint
                });
            }

            result[semId] = courseResults.OrderBy(c => c.CourseCode).ToList();
        }

        return result;
    }
}
