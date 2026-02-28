using Microsoft.AspNetCore.Mvc.Rendering;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Interfaces;

public interface IStudentService
{
    Task<List<Student>> GetAllStudentsAsync();
    Task<Student> GetStudentByIdAsync(Guid id);
    Task<Student> GetStudent_ProgramByIdAsync(Guid id);
    Task AddStudentAsync(Student student);
    Task UpdateStudentAsync(Student student);
    Task DeleteStudentAsync(Guid id);
    Task<List<CollegeProgram>> GetCollegeProgramsAsync();
    
    // New methods for search, pagination, and filtering
    Task<(List<Student> Students, int TotalCount)> GetStudentsAsync(
        string searchQuery = null,
        int? level = null,
        string status=null,
        int page = 1,
        int pageSize = 10);

    Task<byte[]> DownloadStudentsAsync(string searchQuery = null, int? level = null);
    
    //Parent and Guardian
    Task<List<ParentGuardian>> GetParentsByStudentIdAsync(Guid studentId);
    Task<ParentGuardian> GetParentGuardianByIdAsync(Guid id);
    Task AddParentGuardianAsync(ParentGuardian parentGuardian);
    Task UpdateParentGuardianAsync(ParentGuardian parentGuardian);
    Task DeleteParentGuardianAsync(Guid id);
    
    //FinancialInfo
    Task<List<FinancialInfo>> GetFinancialInfoByStudentIdAsync(Guid studentId);
    Task<FinancialInfo> GetFinancialInfoByIdAsync(Guid id);
    Task AddFinancialInfoAsync(FinancialInfo financialInfo);
    Task UpdateFinancialInfoAsync(FinancialInfo financialInfo);
    Task DeleteFinancialInfoAsync(Guid id);
    
    //EducationHistories
    Task<List<EducationHistory>> GetEducationHistoriesByStudentIdAsync(Guid studentId);
    Task<EducationHistory> GetEducationHistoryByIdAsync(Guid id);
    Task AddEducationHistoryAsync(EducationHistory educationHistory);
    Task UpdateEducationHistoryAsync(EducationHistory educationHistory);
    Task DeleteEducationHistoryAsync(Guid id);
    
    Task<(int SuccessCount, int ErrorCount, List<string> Errors)> UploadStudentsFromExcelAsync(Stream fileStream);
}