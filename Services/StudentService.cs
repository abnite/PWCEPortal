using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Services;

public class StudentService: IStudentService
{
    public readonly PortalDbContext _context;
    public readonly IConfiguration _configuration;
    public readonly IEmailSender _emailSenderService;
    public readonly DataHelper _dataHelper;
    public StudentService(PortalDbContext context, IConfiguration configuration, IEmailSender emailSenderService, DataHelper dataHelper)
    {
        _context = context;
        _configuration = configuration;
        _emailSenderService = emailSenderService;
        _dataHelper = dataHelper;
    }
    public async Task<List<Student>> GetAllStudentsAsync()
    {
        return await _context.Students.Where(p=> p.IsDeleted==false).ToListAsync();
    }

    public async Task<Student> GetStudentByIdAsync(Guid id)
    {
        return await _context.Students.FindAsync(id);
    }
    
    public async Task<Student> GetStudent_ProgramByIdAsync(Guid id)
    {
        return await _context.Students.Include(i=>i.CollegeClass).Include(i=>i.CollegeHall).Include(i=>i.CollegeProgram).FirstOrDefaultAsync(i=>i.Id == id);
    }

    public async Task AddStudentAsync(Student student)
    {
        
        student.ExpectedCompletionYear=student.EnrolmentYear+4;
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStudentAsync(Student student)
    {
        student.ExpectedCompletionYear=student.EnrolmentYear+4;
        _context.Students.Update(student);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteStudentAsync(Guid id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            student.IsDeleted = true;
            student.DateDeleted = DateTime.Now;
            student.DeletedBy = _dataHelper.GetLoggedInuser();
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<CollegeProgram>> GetCollegeProgramsAsync()
    {
        return await _context.CollegePrograms.Where(p=>p.IsDeleted==false).ToListAsync();
    }

    public async Task<(List<Student> Students, int TotalCount)> GetStudentsAsync(
        string searchQuery = null,
        int? level = null,
        string status=null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Students.Where(i=>i.IsDeleted!=true && i.HasGraduated!=true).OrderByDescending(i=>i.Status=="Active").AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(s =>
                s.Surname.Contains(searchQuery) ||
                s.OtherNames.Contains(searchQuery) ||
                s.StudentID.Contains(searchQuery) ||
                s.ApplicationNumber.Contains(searchQuery));
        }

        // Apply level filter
        if (level.HasValue)
        {
            query = query.Where(s => s.CurrentLevel == level.Value);
        }
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var students = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (students, totalCount);
    }

    public async Task<byte[]> DownloadStudentsAsync(string searchQuery = null, int? level = null)
    {
        var query = _context.Students.Where(p=>p.IsDeleted==false && p.Status=="Active").AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(s =>
                s.Surname.Contains(searchQuery) ||
                s.OtherNames.Contains(searchQuery) ||
                s.StudentID.Contains(searchQuery) ||
                s.ApplicationNumber.Contains(searchQuery));
        }

        // Apply level filter
        if (level.HasValue)
        {
            query = query.Where(s => s.CurrentLevel == level.Value);
        }

        var students = await query.Include(i=>i.CollegeProgram).ToListAsync();

        // Convert students to CSV format
        var csv = new StringBuilder();
        csv.AppendLine("Application Number,Student ID No,Surname,Other Names,Gender,DoB,Email,PhoneNo,CurrentLevel,Entry Level,Program, Year of Enrollment, Year of Completion");

        foreach (var student in students)
        {
            csv.AppendLine($"{student.ApplicationNumber},{student.StudentID},{student.Surname},{student.OtherNames},{student.Gender},{student.DateOfBirth},{student.Email},{student.PhoneNo},{student.CurrentLevel},{student.LevelOfEntry},{student.CollegeProgram.ProgramName},{student.EnrolmentYear},{student.ExpectedCompletionYear}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    
    //Parent
    public async Task<List<ParentGuardian>> GetParentsByStudentIdAsync(Guid studentId)
    {
        return await _context.ParentGuardians.Where(p => p.StudentId == studentId &&  p.IsDeleted==false).ToListAsync();
    }

    public async Task<ParentGuardian> GetParentGuardianByIdAsync(Guid id)
    {
        return await _context.ParentGuardians.FindAsync(id);
    }

    public async Task AddParentGuardianAsync(ParentGuardian parentGuardian)
    {
        _context.ParentGuardians.Add(parentGuardian);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateParentGuardianAsync(ParentGuardian parentGuardian)
    {
        _context.ParentGuardians.Update(parentGuardian);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteParentGuardianAsync(Guid id)
    {
        var parentGuardian = await _context.ParentGuardians.FindAsync(id);
        if (parentGuardian != null)
        {
            parentGuardian.IsDeleted = true;
            parentGuardian.DateDeleted = DateTime.Now;
            parentGuardian.DeletedBy = _dataHelper.GetLoggedInuser();
            await _context.SaveChangesAsync();
        }
    }
    
    //Financial Info
    public async Task<List<FinancialInfo>> GetFinancialInfoByStudentIdAsync(Guid studentId)
    {
        return await _context.FinancialInfos.Where(p => p.StudentId == studentId && p.IsDeleted==false).ToListAsync();
    }

    public async Task<FinancialInfo> GetFinancialInfoByIdAsync(Guid id)
    {
        return await _context.FinancialInfos.FindAsync(id);
    }

    public async Task AddFinancialInfoAsync(FinancialInfo financialInfo)
    {
        _context.FinancialInfos.Add(financialInfo);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateFinancialInfoAsync(FinancialInfo financialInfo)
    {
        _context.FinancialInfos.Update(financialInfo);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteFinancialInfoAsync(Guid id)
    {
        var financialInfo = await _context.FinancialInfos.FindAsync(id);
        if (financialInfo != null)
        {
            financialInfo.IsDeleted = true;
            financialInfo.DateDeleted = DateTime.Now;
            financialInfo.DeletedBy = _dataHelper.GetLoggedInuser();
            await _context.SaveChangesAsync();
        }
    }
    
    //Educational History
    public async Task<List<EducationHistory>> GetEducationHistoriesByStudentIdAsync(Guid studentId)
    {
        return await _context.EducationHistories.Where(e => e.StudentId == studentId && e.IsDeleted==false).ToListAsync();
    }

    public async Task<EducationHistory> GetEducationHistoryByIdAsync(Guid id)
    {
        return await _context.EducationHistories.FindAsync(id);
    }

    public async Task AddEducationHistoryAsync(EducationHistory educationHistory)
    {
        _context.EducationHistories.Add(educationHistory);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEducationHistoryAsync(EducationHistory educationHistory)
    {
        _context.EducationHistories.Update(educationHistory);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteEducationHistoryAsync(Guid id)
    {
        var educationHistory = await _context.EducationHistories.FindAsync(id);
        if (educationHistory != null)
        {
            educationHistory.IsDeleted = true;
            educationHistory.DateDeleted = DateTime.Now;
            educationHistory.DeletedBy = _dataHelper.GetLoggedInuser();
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<(int SuccessCount, int ErrorCount, List<string> Errors)> UploadStudentsFromExcelAsync(Stream fileStream)
    {
        var errors = new List<string>();
        int successCount = 0;
        int errorCount = 0;

        // Set the EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage(fileStream))
        {
            var worksheet = package.Workbook.Worksheets[0]; // Assume data is in the first sheet

            // Start from row 2 (skip header)
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                try
                {
                    // Parse DateOfBirth in "MM/dd/yyyy" format
                    var dateOfBirthText = worksheet.Cells[row, 8].Text;
                    DateTime dateOfBirth;
                    if (!DateTime.TryParseExact(dateOfBirthText, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth))
                    {
                        throw new Exception("Invalid DateOfBirth format. Expected format: MM/dd/yyyy. For Student ID "+worksheet.Cells[row, 2].Text);
                    }
                    var getProgram=worksheet.Cells[row, 13].Value.ToString();
                    var getProgramId =await _context.CollegePrograms.Where(i => i.ProgramName == getProgram).FirstOrDefaultAsync();
                    
                    if (getProgramId == null)
                    {
                        throw new Exception($"Program '{getProgram}' not found. For Student ID "+worksheet.Cells[row, 2].Text);
                    }
                    
                    var applicationNumber = worksheet.Cells[row, 2].Text;
                    var existingStudent = await _context.Students
                        .FirstOrDefaultAsync(s => s.ApplicationNumber == applicationNumber);

                    if (existingStudent != null)
                    {
                        throw new Exception($"Duplicate studentID No: {applicationNumber}.");
                    }

                    
                    var student = new Student
                    {
                        ApplicationNumber = worksheet.Cells[row, 2].Text,
                        StudentID = worksheet.Cells[row, 3].Text,
                        Title = worksheet.Cells[row, 4].Text,
                        Surname = worksheet.Cells[row, 5].Text,
                        OtherNames = worksheet.Cells[row, 6].Text,
                        Gender = worksheet.Cells[row, 7].Text,
                        DateOfBirth = dateOfBirth,
                        PhoneNo = worksheet.Cells[row, 9].Text,
                        Email = worksheet.Cells[row, 10].Text,
                        CurrentLevel =int.Parse(worksheet.Cells[row, 11].Text),
                        LevelOfEntry = int.Parse(worksheet.Cells[row, 12].Text),
                        CollegeProgramId =Guid.Parse(getProgramId.Id.ToString()),
                        EnrolmentYear = int.Parse(worksheet.Cells[row, 14].Text),
                        ExpectedCompletionYear = int.Parse(worksheet.Cells[row, 15].Text),
                    };

                    // Validate the student data
                    if (string.IsNullOrEmpty(student.Surname) ||
                        string.IsNullOrEmpty(student.OtherNames) ||
                        string.IsNullOrEmpty(student.ApplicationNumber))
                    {
                        throw new Exception("Required fields are missing.");
                    }

                    // Add the student to the database
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add($"Row {row}: {ex.Message}");
                }
            }
        }

        return (successCount, errorCount, errors);
    }
    
}