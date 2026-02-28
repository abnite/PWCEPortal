using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class AcademicService:IAcademicService
{
    private readonly PortalDbContext _context;

    public AcademicService(PortalDbContext context)
    {
        _context = context;
    }

    public async Task<AcademicYear> GetCurrentAcademicYearAsync()
    {
        // Fetch the active academic year
        return await _context.AcademicYears.FirstOrDefaultAsync(ay => ay.IsActive);
    }

    public async Task<List<Course>> GetRegisteredCoursesAsync(Guid studentId)
    {
        // Fetch courses registered by the student
        return await _context.StudentCourseRegistrations.Include(i=>i.Semester)
            .Where(scr => scr.StudentId == studentId && scr.IsRegistered &&  scr.Semester.IsRegistrationActive)
            .Select(scr => scr.Course)
            .ToListAsync();
    }

    public async Task<List<Course>> GetAvailableCoursesAsync(Guid studentId)
    {
        // Fetch courses not yet registered by the student
        var registeredCourseIds = await _context.StudentCourseRegistrations
            .Where(scr => scr.StudentId == studentId && scr.IsRegistered)
            .Select(scr => scr.CourseId)
            .ToListAsync();

        return await _context.Courses
            .Where(c => !registeredCourseIds.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<List<string>> GetNotificationsAsync(Guid studentId)
    {
        var notifications = new List<string>();

        // Example: Check for unverified payments
        var unverifiedPayments = await _context.Payments
            .Where(p => p.StudentId == studentId && !p.IsVerified)
            .ToListAsync();

        if (unverifiedPayments.Any())
        {
            notifications.Add("You have unverified payments. Please contact the finance office.");
        }

        // Example: Check for upcoming deadlines (e.g., fee payment deadline)
        var currentAcademicYear = await GetCurrentAcademicYearAsync();
        if (currentAcademicYear != null)
        {
            notifications.Add($"Fee payment deadline for {currentAcademicYear.Year} is approaching.");
        }

        return notifications;
    }
}