using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;
using PWCEPortal.Services;
using PWCEPortal.ViewModel.Academic;

namespace PWCEPortal.Controllers;

[Authorize]
public class AcademicController : Controller
{
    private readonly IAcademicService _academicService;
    private readonly PortalDbContext _context;
    private readonly DataHelper _dataHelper;

    public AcademicController(IAcademicService academicService, PortalDbContext context, DataHelper dataHelper)
    {
        _academicService = academicService;
        _context = context;
        _dataHelper = dataHelper;
    }

    // GET
    public IActionResult Index()
    {
        return View();
    }

    // AcademicController.cs
    [HttpGet]
    public async Task<IActionResult> RegisterCourses(Guid studentId)
    {
        var student = await _context.Students.Where(i => i.IsDeleted == false).Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            return NotFound();

        var currentSemester = await _context.AcademicSemesters
            .FirstOrDefaultAsync(s => s.IsRegistrationActive);

        if (currentSemester == null)
        {
            TempData["ErrorMessage"] = "Course registration is currently closed.";
            return RedirectToAction("Index", "StudentDashboard");
        }

        int semester = 0;
        if (currentSemester.SemesterName == "Semester 1")
        {
            semester = 1;
        }
        else if (currentSemester.SemesterName == "Semester 2")
        {
            semester = 2;
        }

        // Get courses the student HAS ALREADY registered for
        var registeredCourseIds = await _context.StudentCourseRegistrations
            .Where(r => r.StudentId == studentId && r.IsRegistered && r.Semester == currentSemester)
            .Select(r => r.CourseId)
            .ToListAsync();

        // Fetch only UNREGISTERED courses matching program, level, and semester
        var availableCourses = await _context.Courses
            .Where(c => /*c.CollegeProgramId == student.CollegeProgramId &&*/
                        c.Level == student.CurrentLevel &&
                        c.Semester == semester &&
                        (c.IsCommon || c.CollegeProgramId == student.CollegeProgramId) &&
                        !registeredCourseIds.Contains(c.Id)) // Exclude registered courses
            .ToListAsync();

        var model = new CourseRegistrationViewModel
        {
            StudentId = student.Id,
            StudentName = $"{student.Surname} {student.OtherNames}",
            CurrentLevel = student.CurrentLevel,
            CollegeProgramId = student.CollegeProgramId,
            ProgramName = student.CollegeProgram.ProgramName,
            Semester = currentSemester.SemesterName == "Semester 1" ? 1 : 2, // Parse semester number
            AvailableCourses = availableCourses.Select(c => new CourseSelection
            {
                CourseId = c.Id,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                CourseType = c.CourseType
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterCourses(CourseRegistrationViewModel model)
    {
        try
        {

            ModelState.Remove("StudentName");
            ModelState.Remove("ProgramName");
            if (!ModelState.IsValid || model.SelectedCourseIds == null || !model.SelectedCourseIds.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one course.";
                return RedirectToAction("RegisterCourses", new { studentId = model.StudentId });
            }

            // Validate registration period
            var currentSemester = await _context.AcademicSemesters
                .FirstOrDefaultAsync(s => s.IsRegistrationActive);

            if (currentSemester == null)
            {
                TempData["ErrorMessage"] = "Registration period has ended.";
                return RedirectToAction("Index", "StudentDashboard");
            }

            // Check for already registered courses
            var existingRegistrations = await _context.StudentCourseRegistrations
                .Where(r => r.StudentId == model.StudentId &&
                            model.SelectedCourseIds.Contains(r.CourseId) &&
                            r.IsRegistered && r.Semester == currentSemester)
                .ToListAsync();

            if (existingRegistrations.Any())
            {
                TempData["ErrorMessage"] = "You have already registered for one or more selected courses.";
                return RedirectToAction("RegisterCourses", new { studentId = model.StudentId });
            }

            // Save selected courses
            foreach (var courseId in model.SelectedCourseIds)
            {
                var registration = new StudentCourseRegistration
                {
                    StudentId = model.StudentId,
                    CourseId = courseId,
                    IsRegistered = true,
                    SemesterId = currentSemester.Id,
                    AddedBy = _dataHelper.GetLoggedInuser()
                };
                _context.StudentCourseRegistrations.Add(registration);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Courses registered successfully!";

        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = "Error" + e.Message;
        }

        return RedirectToAction("Index", "StudentDashboard");
    }


    // For Student Preview
    [HttpGet]
    public async Task<IActionResult> ViewRegisteredCourses(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Id == studentId && s.IsDeleted == false);

        if (student == null)
        {
            {
                TempData["ErrorMessage"] = "Student record not found or Deleted.";
                return RedirectToAction("Index", "StudentDashboard");
            }
        }

        var currentSemester = await _context.AcademicSemesters
            .FirstOrDefaultAsync(s => s.IsRegistrationActive);

        if (currentSemester == null)
        {
            // If no active semester, show all registered courses
            var allRegisteredCourses = await _context.StudentCourseRegistrations
                .Where(r => r.StudentId == studentId && r.IsRegistered)
                .Include(r => r.Course)
                .Include(r => r.Semester)
                .OrderByDescending(r => r.Semester.RegistrationStartDate)
                .ToListAsync();
            
            if (!allRegisteredCourses.Any())
            {
                TempData["ErrorMessage"] = "No registered courses found for this student.";
                return RedirectToAction("Index", "StudentDashboard");
            }

            var model = new RegisteredCoursesViewModel
            {
                StudentId = student.Id,
                StudentName = $"{student.Surname} {student.OtherNames}",
                ProgramName = student.CollegeProgram?.ProgramName,
                CurrentLevel = student.CurrentLevel,
                Courses = allRegisteredCourses.Select(r => new RegisteredCourseView
                {
                    CourseCode = r.Course.CourseCode,
                    CourseName = r.Course.CourseName,
                    CourseType = r.Course.CourseType,
                    Semester = r.Semester.SemesterName,
                    RegistrationDate = r.DateAdded.Value
                }).ToList()
            };

            return View(model);
        }

        // For current active semester
        var registeredCourses = await _context.StudentCourseRegistrations
            .Where(r => r.StudentId == studentId &&
                        r.IsRegistered &&
                        r.SemesterId == currentSemester.Id)
            .Include(r => r.Course)
            .ToListAsync();
        if (!registeredCourses.Any())
        {
            TempData["ErrorMessage"] = "No registered courses found for this student.";
            return RedirectToAction("Index", "StudentDashboard");
        }

        var viewModel = new RegisteredCoursesViewModel
        {
            StudentId = student.Id,
            StudentName = $"{student.Surname} {student.OtherNames}",
            ProgramName = student.CollegeProgram?.ProgramName,
            CurrentLevel = student.CurrentLevel,
            Semester = currentSemester.SemesterName,
            Courses = registeredCourses.Select(r => new RegisteredCourseView
            {
                CourseCode = r.Course.CourseCode,
                CourseName = r.Course.CourseName,
                CourseType = r.Course.CourseType,
                RegistrationDate = r.DateAdded.Value
            }).ToList()
        };

        return View(viewModel);
    }

    [Authorize(Roles = "System Admin, Registrar, Secretary")]
    [HttpGet]
public async Task<IActionResult> ViewAllRegistrations(int? levelFilter, string? searchString, 
    string? academicYearFilter, string? programFilter, int pageNumber = 1)
{
    ViewData["CurrentFilter"] = searchString;
    ViewData["CurrentLevelFilter"] = levelFilter;
    ViewData["CurrentAcademicYearFilter"] = academicYearFilter;
    ViewData["CurrentProgramFilter"] = programFilter;

    // Base query for students with registrations
    var studentsQuery = _context.Students
        .Where(s => s.IsDeleted==false && s.StudentCourseRegistrations.Any(r => r.IsRegistered))
        .Include(s => s.CollegeProgram)
        .Include(s => s.StudentCourseRegistrations)
            .ThenInclude(r => r.Semester)
                .ThenInclude(s => s.AcademicYear)
        .Include(s => s.StudentCourseRegistrations)
            .ThenInclude(r => r.Course)
        .OrderBy(s => s.Surname)
        .ThenBy(s => s.OtherNames)
        .AsQueryable();

    // Apply filters
    if (!string.IsNullOrEmpty(searchString))
    {
        studentsQuery = studentsQuery.Where(s =>
            s.Surname.Contains(searchString) ||
            s.OtherNames.Contains(searchString) ||
            s.StudentID.Contains(searchString) ||
            s.ApplicationNumber.Contains(searchString));
    }

    if (levelFilter.HasValue)
    {
        studentsQuery = studentsQuery.Where(s => s.CurrentLevel == levelFilter);
    }

    if (!string.IsNullOrEmpty(programFilter))
    {
        studentsQuery = studentsQuery.Where(s => s.CollegeProgram.ProgramName == programFilter);
    }

    if (!string.IsNullOrEmpty(academicYearFilter))
    {
        studentsQuery = studentsQuery.Where(s => 
            s.StudentCourseRegistrations.Any(r => 
                r.Semester.AcademicYear.Year == academicYearFilter));
    }

    // Get distinct academic years for filter dropdown
    var academicYears = await _context.AcademicSemesters.Where(i=>i.IsDeleted == false)
        .Include(s => s.AcademicYear)
        .Select(s => s.AcademicYear.Year)
        .Distinct()
        .OrderByDescending(y => y)
        .ToListAsync();

    // Get programs for filter dropdown
    var programs = await _context.CollegePrograms.Where(i=>i.IsDeleted == false)
        .OrderBy(p => p.ProgramName)
        .ToListAsync();

    ViewBag.AcademicYears = academicYears;
    ViewBag.Programs = programs;

    int pageSize = 20;
    var paginatedStudents = await PaginatedList<Student>.CreateAsync(studentsQuery.AsNoTracking(), pageNumber, pageSize);

    return View(paginatedStudents);
}
    
 
[Authorize(Roles = "System Admin, Secretary, Registrar")]
[HttpGet]
public async Task<IActionResult> ExportRegistrationsToExcel(
    string searchString,
    string programFilter,
    int? levelFilter,
    string academicYearFilter)
{
    // Build the query with all filters applied
    var query = _context.Students
        .Where(s => s.IsDeleted == false && s.StudentCourseRegistrations.Any(r => r.IsRegistered))
        .Include(s => s.CollegeProgram)
        .Include(s => s.StudentCourseRegistrations)
            .ThenInclude(r => r.Semester)
                .ThenInclude(s => s.AcademicYear)
        .Include(s => s.StudentCourseRegistrations)
            .ThenInclude(r => r.Course)
        .AsQueryable();

    // Apply filters - MUST MATCH THE VIEW'S FILTER LOGIC EXACTLY
    if (!string.IsNullOrEmpty(searchString))
    {
        query = query.Where(s =>
            s.Surname.Contains(searchString) ||
            s.OtherNames.Contains(searchString) ||
            s.StudentID.Contains(searchString) ||
            s.ApplicationNumber.Contains(searchString));
    }

    if (!string.IsNullOrEmpty(programFilter))
    {
        query = query.Where(s => s.CollegeProgram.ProgramName == programFilter);
    }

    if (levelFilter.HasValue)
    {
        query = query.Where(s => s.CurrentLevel == levelFilter.Value);
    }

    if (!string.IsNullOrEmpty(academicYearFilter))
    {
        query = query.Where(s => 
            s.StudentCourseRegistrations.Any(r => 
                r.Semester.AcademicYear.Year == academicYearFilter));
    }

    // Execute the query AFTER all filters are applied
    var students = await query
        .OrderBy(s => s.Surname)
        .ThenBy(s => s.OtherNames)
        .ToListAsync();

    // Transform the filtered data for export
    var exportData = new List<dynamic>();
    
    foreach (var student in students)
    {
        // Apply the same filtering to registrations as in the view
        var registrations = student.StudentCourseRegistrations
            .Where(r => r.IsRegistered && r.Semester != null && r.Semester.AcademicYear != null);

        // Additional filter for academic year if specified
        if (!string.IsNullOrEmpty(academicYearFilter))
        {
            registrations = registrations.Where(r => 
                r.Semester.AcademicYear.Year == academicYearFilter);
        }

        foreach (var registration in registrations)
        {
            exportData.Add(new
            {
                StudentName = $"{student.Surname}, {student.OtherNames}",
                 ApplicationNumber= student.ApplicationNumber,
                 StudentID=student.StudentID,
                Program = student.CollegeProgram?.ProgramName ?? "N/A",
                Level = student.CurrentLevel,
                AcademicYear = registration.Semester?.AcademicYear?.Year ?? "N/A",
                Semester = registration.Semester?.SemesterName ?? "N/A",
                CourseCode = registration.Course?.CourseCode ?? "N/A",
                CourseName = registration.Course?.CourseName ?? "N/A",
                CourseType = registration.Course?.CourseType ?? "N/A",
                RegistrationDate = registration.DateAdded.Value.ToString("yyyy-MM-dd"),
             
                
            });
        }
    }

    // Rest of your export code...
    var columnMappings = new Dictionary<string, string>
    {
        {"StudentName", "Student Name"},
        {"ApplicationNumber", "Application No"},
        {"StudentID", "Student ID"},
        {"Program", "Program"},
        {"Level", "Level"},
        {"AcademicYear", "Academic Year"},
        {"Semester", "Semester"},
        {"CourseCode", "Course Code"},
        {"CourseName", "Course Name"},
        {"CourseType", "Course Type"},
        {"RegistrationDate", "Registration Date"}
    };

    var exportService = new ExcelExportService();
    var fileContents = exportService.ExportToExcel(exportData, columnMappings, "StudentRegistrations");

    return File(fileContents, 
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
        $"StudentRegistrations_{DateTime.Now:yyyyMMdd}.xlsx");
}

private IQueryable<Student> BuildRegistrationsQuery(
    string searchString,
    string programFilter,
    int? levelFilter,
    string academicYearFilter)
{
    var query = _context.Students
        .Where(s => s.IsDeleted == false && s.StudentCourseRegistrations.Any(r => r.IsRegistered))
        .Include(s => s.CollegeProgram)
        .Include(s => s.StudentCourseRegistrations)
            .ThenInclude(r => r.Semester)
                .ThenInclude(s => s.AcademicYear)
        .Include(s => s.StudentCourseRegistrations)
            .ThenInclude(r => r.Course)
        .OrderBy(s => s.Surname)
        .ThenBy(s => s.OtherNames)
        .AsQueryable();

    // Apply filters
    if (!string.IsNullOrEmpty(searchString))
    {
        query = query.Where(s =>
            s.Surname.Contains(searchString) ||
            s.OtherNames.Contains(searchString) ||
            s.StudentID.Contains(searchString) ||
            s.ApplicationNumber.Contains(searchString));
    }

    if (!string.IsNullOrEmpty(programFilter))
    {
        query = query.Where(s => s.CollegeProgram.ProgramName == programFilter);
    }

    if (levelFilter.HasValue)
    {
        query = query.Where(s => s.CurrentLevel == levelFilter.Value);
    }

    if (!string.IsNullOrEmpty(academicYearFilter))
    {
        query = query.Where(s => 
            s.StudentCourseRegistrations.Any(r => 
                r.Semester.AcademicYear.Year == academicYearFilter));
    }

    return query;
}
    
    [HttpGet]
    public async Task<IActionResult> GetStudentCourses(Guid studentId, Guid semesterId)
    {
        try
        {
            var student = await _context.Students
                .Include(s => s.CollegeProgram)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return Json(new { success = false, message = "Student not found" });

            var semester = await _context.AcademicSemesters
                .FirstOrDefaultAsync(s => s.Id == semesterId);

            if (semester == null)
                return Json(new { success = false, message = "Semester not found" });

            var courses = await _context.StudentCourseRegistrations
                .Where(r => r.StudentId == studentId &&
                            r.SemesterId == semesterId &&
                            r.IsRegistered)
                .Include(r => r.Course)
                .OrderBy(r => r.Course.CourseCode)
                .Select(r => new
                {
                    courseCode = r.Course.CourseCode,
                    courseName = r.Course.CourseName,
                    courseType = r.Course.CourseType,
                    registrationDate = r.DateAdded
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                studentName = $"{student.Surname} {student.OtherNames}",
                programName = student.CollegeProgram?.ProgramName,
                academicYear = semester.AcademicYear,
                semesterName = semester.SemesterName,
                courses = courses
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}