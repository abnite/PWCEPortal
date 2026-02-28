using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Services;

public class CourseLecturerService : ICourseLecturerService
{
    private readonly PortalDbContext _context;

    public CourseLecturerService(PortalDbContext context)
    {
        _context = context;
    }

    // ── Departments ─────────────────────────────────────────────────────────

    public async Task<List<Department>> GetAllDepartmentsAsync() =>
        await _context.Departments.Where(d => d.IsDeleted != true).OrderBy(d => d.DepartmentName).ToListAsync();

    public async Task<Department?> GetDepartmentByIdAsync(Guid id) =>
        await _context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted != true);

    public async Task<bool> CreateDepartmentAsync(Department department)
    {
        _context.Departments.Add(department);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateDepartmentAsync(Department department)
    {
        _context.Departments.Update(department);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteDepartmentAsync(Guid id)
    {
        var dept = await GetDepartmentByIdAsync(id);
        if (dept is null) return false;
        dept.IsDeleted = true;
        dept.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    // ── Lecturers ────────────────────────────────────────────────────────────

    public async Task<List<Lecturer>> GetAllLecturersAsync() =>
        await _context.Lecturers
            .Include(l => l.User)
            .Include(l => l.Department)
            .Where(l => l.IsDeleted != true)
            .OrderBy(l => l.User!.LastName)
            .ToListAsync();

    public async Task<List<Lecturer>> GetLecturersByDepartmentAsync(Guid departmentId) =>
        await _context.Lecturers
            .Include(l => l.User)
            .Include(l => l.Department)
            .Where(l => l.IsDeleted != true && l.DepartmentId == departmentId)
            .OrderBy(l => l.User!.LastName)
            .ToListAsync();

    public async Task<Lecturer?> GetLecturerByIdAsync(Guid id) =>
        await _context.Lecturers
            .Include(l => l.User)
            .Include(l => l.Department)
            .FirstOrDefaultAsync(l => l.Id == id && l.IsDeleted != true);

    public async Task<Lecturer?> GetLecturerByUserIdAsync(string userId) =>
        await _context.Lecturers
            .Include(l => l.User)
            .Include(l => l.Department)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.IsDeleted != true);

    public async Task<bool> CreateLecturerAsync(Lecturer lecturer)
    {
        _context.Lecturers.Add(lecturer);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateLecturerAsync(Lecturer lecturer)
    {
        _context.Lecturers.Update(lecturer);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteLecturerAsync(Guid id)
    {
        var lecturer = await GetLecturerByIdAsync(id);
        if (lecturer is null) return false;
        lecturer.IsDeleted = true;
        lecturer.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    // ── Assignments ──────────────────────────────────────────────────────────

    public async Task<List<CourseLecturerAssignment>> GetAssignmentsAsync(Guid? semesterId = null, Guid? departmentId = null)
    {
        var query = _context.CourseLecturerAssignments
            .Include(a => a.Lecturer).ThenInclude(l => l!.User)
            .Include(a => a.Lecturer).ThenInclude(l => l!.Department)
            .Include(a => a.Course).ThenInclude(c => c!.CollegeProgram)
            .Include(a => a.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .Where(a => a.IsDeleted != true);

        if (semesterId.HasValue)
            query = query.Where(a => a.AcademicSemesterId == semesterId.Value);

        if (departmentId.HasValue)
            query = query.Where(a => a.Lecturer!.DepartmentId == departmentId.Value);

        return await query.OrderBy(a => a.Course!.CourseName).ToListAsync();
    }

    public async Task<List<CourseLecturerAssignment>> GetAssignmentsForLecturerAsync(Guid lecturerId, Guid semesterId) =>
        await _context.CourseLecturerAssignments
            .Include(a => a.Course).ThenInclude(c => c!.CollegeProgram)
            .Include(a => a.AcademicSemester)
            .Where(a => a.LecturerId == lecturerId && a.AcademicSemesterId == semesterId && a.IsDeleted != true)
            .ToListAsync();

    public async Task<CourseLecturerAssignment?> GetAssignmentByIdAsync(Guid id) =>
        await _context.CourseLecturerAssignments
            .Include(a => a.Lecturer).ThenInclude(l => l!.User)
            .Include(a => a.Course).ThenInclude(c => c!.CollegeProgram)
            .Include(a => a.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted != true);

    public async Task<bool> AssignLecturerAsync(Guid lecturerId, Guid courseId, Guid semesterId)
    {
        if (await AssignmentExistsAsync(courseId, semesterId)) return false;

        _context.CourseLecturerAssignments.Add(new CourseLecturerAssignment
        {
            LecturerId = lecturerId,
            CourseId = courseId,
            AcademicSemesterId = semesterId
        });
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RemoveAssignmentAsync(Guid assignmentId)
    {
        var assignment = await GetAssignmentByIdAsync(assignmentId);
        if (assignment is null) return false;
        assignment.IsDeleted = true;
        assignment.DateDeleted = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> AssignmentExistsAsync(Guid courseId, Guid semesterId) =>
        await _context.CourseLecturerAssignments
            .AnyAsync(a => a.CourseId == courseId && a.AcademicSemesterId == semesterId && a.IsDeleted != true);
}
