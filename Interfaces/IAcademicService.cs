using PWCEPortal.Models.Academic;

namespace PWCEPortal.Interfaces;

public interface IAcademicService
{
    Task<AcademicYear> GetCurrentAcademicYearAsync();
    Task<List<Course>> GetRegisteredCoursesAsync(Guid studentId);
    Task<List<Course>> GetAvailableCoursesAsync(Guid studentId);
    Task<List<string>> GetNotificationsAsync(Guid studentId);
}