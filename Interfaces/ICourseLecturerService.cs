using PWCEPortal.Models.Academic;
using PWCEPortal.ViewModel.CourseLecturer;

namespace PWCEPortal.Interfaces;

public interface ICourseLecturerService
{
    // Departments
    Task<List<Department>> GetAllDepartmentsAsync();
    Task<Department?> GetDepartmentByIdAsync(Guid id);
    Task<bool> CreateDepartmentAsync(Department department);
    Task<bool> UpdateDepartmentAsync(Department department);
    Task<bool> DeleteDepartmentAsync(Guid id);

    // Lecturers
    Task<List<Lecturer>> GetAllLecturersAsync();
    Task<Lecturer?> GetLecturerByIdAsync(Guid id);
    Task<Lecturer?> GetLecturerByUserIdAsync(string userId);
    Task<bool> CreateLecturerAsync(Lecturer lecturer);
    Task<bool> UpdateLecturerAsync(Lecturer lecturer);
    Task<bool> DeleteLecturerAsync(Guid id);

    // Assignments
    Task<List<CourseLecturerAssignment>> GetAssignmentsAsync(Guid? semesterId = null, Guid? departmentId = null);
    Task<List<CourseLecturerAssignment>> GetAssignmentsForLecturerAsync(Guid lecturerId, Guid semesterId);
    Task<CourseLecturerAssignment?> GetAssignmentByIdAsync(Guid id);
    Task<bool> AssignLecturerAsync(Guid lecturerId, Guid courseId, Guid semesterId);
    Task<bool> RemoveAssignmentAsync(Guid assignmentId);
    Task<bool> AssignmentExistsAsync(Guid courseId, Guid semesterId);
}
