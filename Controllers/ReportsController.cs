using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.Payment;
using PWCEPortal.Models.StudentInfo;
using PWCEPortal.Services;
using PWCEPortal.ViewModel.Dashboard;
using PWCEPortal.ViewModel.Reports;

namespace PWCEPortal.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly PortalDbContext _context;
    private readonly IReportService _reportService;
    private readonly IPaymentService _paymentService;
    private int pageSize = 30;

    public ReportsController(PortalDbContext context, IReportService reportService, IPaymentService paymentService)
    {
        _context = context;
        _reportService = reportService;
        _paymentService = paymentService;
    }

    // GET
    public async Task<IActionResult> Index()
    {
        // We assume that a student with Status "Enrolled" is actively enrolled.
        var totalEnrolled =
            await _context.Students.CountAsync(
                s => s.IsDeleted == false && s.HasGraduated == false && s.Status == "Active");

        // Group by program for enrolled students
        var programMetrics = await _context.Students
            .Where(s => s.IsDeleted == false && s.HasGraduated == false  && s.Status == "Active")
            .GroupBy(s => s.CollegeProgram)
            .Select(g => new ProgramMetric
            {
                ProgramName = g.Key.ProgramName,
                TotalEnrolled = g.Count()
            })
            .ToListAsync();
        
        // Add level metrics query
        var levelMetrics = await _context.Students
            .Where(s => s.IsDeleted == false && s.HasGraduated == false && s.Status == "Active")
            .GroupBy(s => s.CurrentLevel)
            .Select(g => new LevelMetric
            {
                Level = g.Key,
                TotalEnrolled = g.Count()
            })
            .OrderBy(l => l.Level)
            .ToListAsync();

        // Other aggregated counts
        int deferredCount = await _context.Students.CountAsync(s => s.Status == "Deferred");
        int droppedOutCount = await _context.Students.CountAsync(s => s.Status == "Dropped Out");
        int transferredCount = await _context.Students.CountAsync(s => s.Status == "Transferred");

        var viewModel = new StudentMetricsDashboardViewModel
        {
            ProgramMetrics = programMetrics,
            DeferredCount = deferredCount,
            DroppedOutCount = droppedOutCount,
            TransferredCount = transferredCount,
            TotalStudentsEnrolled = totalEnrolled,
            LevelMetrics = levelMetrics
        };

        return View(viewModel);
    }


    [Authorize (Roles = "System Admin, Finance Officer, Administrator")]
    public async Task<IActionResult> PaymentReport(DateTime? startDate, DateTime? endDate, string searchTerm,
        int pageIndex = 1)
    {
        var query = _context.Payments.Include(i => i.VerifiedBy).Include(p => p.Student).Include(s=>s.Student.CollegeProgram).AsQueryable();

        // Filter by date range if provided
        if (startDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= endDate.Value);
        }

        // Filter by search term (search in PaymentReference or Student's surname)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p => p.PaymentReference.Contains(searchTerm)
                                     || p.Student.Surname.Contains(searchTerm) ||
                                     p.Student.OtherNames.Contains(searchTerm) ||
                                     p.BankReferenceNumber.Contains(searchTerm) ||
                                     p.VerifiedBy.LastName.Contains(searchTerm) |
                                     p.VerifiedBy.FirstName.Contains(searchTerm));
        }

        query = query.OrderByDescending(p => p.PaymentDate);
        var paginatedPayments = await PaginatedList<Payment>.CreateAsync(query, pageIndex, pageSize);

        // Calculate aggregates for the filtered query
        decimal totalFeesCollected = await _context.Payments
            .Where(p => p.IsVerified
                        && (!startDate.HasValue || p.PaymentDate >= startDate.Value)
                        && (!endDate.HasValue || p.PaymentDate <= endDate.Value))
            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;
       /* decimal totalOutstanding = await _context.Payments
            .Where(p => !p.IsVerified
                        && (!startDate.HasValue || p.PaymentDate >= startDate.Value)
                        && (!endDate.HasValue || p.PaymentDate <= endDate.Value))
            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;*/
       decimal totalOutstanding = await _context.StudentFeeAssignments
            .Where(i => i.AcademicYear.IsActive)
            .SumAsync(s => (decimal?)s.OutstandingFee) ?? 0;

        ViewBag.TotalFeesCollected = totalFeesCollected;
        ViewBag.TotalOutstanding = totalOutstanding;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.SearchTerm = searchTerm;

        return View(paginatedPayments);
    }

    [Authorize (Roles = "System Admin")]
    public async Task<IActionResult> AuditLogReport(DateTime? startDate, DateTime? endDate, string searchTerm,
        int pageIndex = 1)
    {
        var query = _context.AuditLogs.Include(i => i.User).AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(a => a.DateAdded >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.DateAdded <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(a => a.ActionPerformed.Contains(searchTerm)
                                     || a.UserId.Contains(searchTerm));
        }

        query = query.OrderByDescending(a => a.DateAdded);
        var paginatedLogs = await PaginatedList<AuditLog>.CreateAsync(query, pageIndex, pageSize);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        return View(paginatedLogs);
    }

    [Authorize (Roles = "System Admin, Finance Officer, Administrator")]
    public async Task<IActionResult> RevenueReport(DateTime? startDate, DateTime? endDate, string searchTerm,
        int pageIndex = 1)
    {
        var query = _context.Payments.Include(i => i.VerifiedBy).Include(p => p.Student).Where(i => i.IsVerified)
            .AsQueryable();

        // Filter by date range if provided
        if (startDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= endDate.Value);
        }

        // Filter by search term (search in PaymentReference or Student's surname)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p => p.PaymentReference.Contains(searchTerm)
                                     || p.Student.Surname.Contains(searchTerm));
        }

        query = query.OrderByDescending(p => p.PaymentDate);
        var paginatedPayments = await PaginatedList<Payment>.CreateAsync(query, pageIndex, pageSize);

        // Calculate aggregates for the filtered query
        decimal totalFeesCollected = await _context.Payments
            .Where(p => p.IsVerified
                        && (!startDate.HasValue || p.PaymentDate >= startDate.Value)
                        && (!endDate.HasValue || p.PaymentDate <= endDate.Value))
            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;
        decimal totalOutstanding = await _context.Payments
            .Where(p => !p.IsVerified
                        && (!startDate.HasValue || p.PaymentDate >= startDate.Value)
                        && (!endDate.HasValue || p.PaymentDate <= endDate.Value))
            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

        ViewBag.TotalFeesCollected = totalFeesCollected;
        ViewBag.TotalOutstanding = totalOutstanding;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.SearchTerm = searchTerm;

        return View(paginatedPayments);
    }


    //Over payment
    [Authorize (Roles = "System Admin, Finance Officer, Administrator")]
    public async Task<IActionResult> OverPaymentFees(string searchQuery = null, int? level = null, int page = 1)
    {
        var (outstandingFees, totalCount) =
            await _paymentService.GetOverpaymentFeesAsync(searchQuery, level, page, pageSize);

        var viewModel = new OutstandingFeesViewModel
        {
            OutstandingFees = outstandingFees,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            SearchQuery = searchQuery,
            Level = level
        };

        return View(viewModel);
    }

    [Authorize (Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    public async Task<IActionResult> DownloadOverPaymentFees(string searchQuery = null, int? level = null)
    {
        var excelBytes = await _paymentService.DownloadOverpaymentFeesAsync(searchQuery, level);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "OverPaymentFees.xlsx");
    }

    [Authorize (Roles = "System Admin, Finance Officer, Administrator")]
    public async Task<IActionResult> ExportPayment(DateTime? startDate, DateTime? endDate, string searchTerm)
    {
        var excelBytes = await _reportService.DownloadFeesPaymentReport(startDate, endDate, searchTerm);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Payments.xlsx");
    }



    // ---------------- Export to Excel for Payment Report ----------------
    /* public async Task<IActionResult> ExportPaymentReportToExcel(DateTime? startDate, DateTime? endDate, string searchTerm)
     {
         var query = _context.Payments.Include(p => p.Student).AsQueryable();
         if (startDate.HasValue)
             query = query.Where(p => p.PaymentDate >= startDate.Value);
         if (endDate.HasValue)
             query = query.Where(p => p.PaymentDate <= endDate.Value);
         if (!string.IsNullOrEmpty(searchTerm))
             query = query.Where(p => p.PaymentReference.Contains(searchTerm)
                                      || p.Student.Surname.Contains(searchTerm));
         query = query.OrderByDescending(p => p.PaymentDate);
         var payments = await query.ToListAsync();

         using (var workbook = new XLWorkbook())
         {
             var worksheet = workbook.Worksheets.Add("Payment Report");
             // Header row
             worksheet.Cell(1, 1).Value = "Payment Date";
             worksheet.Cell(1, 2).Value = "Student Name";
             worksheet.Cell(1, 3).Value = "Application Number";
             worksheet.Cell(1, 4).Value = "Amount Paid";
             worksheet.Cell(1, 5).Value = "Payment Reference";
             worksheet.Cell(1, 6).Value = "Bank Reference";
             worksheet.Cell(1, 7).Value = "Status";

             int row = 2;
             foreach (var payment in payments)
             {
                 worksheet.Cell(row, 1).Value = payment.PaymentDate.ToString("dd MMM yyyy");
                 worksheet.Cell(row, 2).Value = $"{payment.Student?.Surname} {payment.Student?.OtherNames}";
                 worksheet.Cell(row, 3).Value = payment.Student?.ApplicationNumber;
                 worksheet.Cell(row, 4).Value = payment.AmountPaid;
                 worksheet.Cell(row, 5).Value = payment.PaymentReference;
                 worksheet.Cell(row, 6).Value = payment.BankReferenceNumber;
                 worksheet.Cell(row, 7).Value = payment.IsVerified ? "Verified" : "Pending";
                 row++;
             }
             using (var stream = new System.IO.MemoryStream())
             {
                 workbook.SaveAs(stream);
                 var content = stream.ToArray();
                 return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PaymentReport.xlsx");
             }
         }*/

    public async Task<IActionResult> NoStudentsByCourse(string searchString, int? pageNumber)
    {
        int pageSize = 10; // Number of items per page
        ViewData["CurrentFilter"] = searchString;

        var query = _context.StudentCourseRegistrations
            .Include(sr => sr.Course)
            .Include(sr => sr.Student)
            .Where(sr => sr.Student.HasGraduated == false && sr.IsDeleted == false)
            .GroupBy(sr => sr.Course)
            .Select(g => new CourseMetric
            {
                CourseId = g.Key.Id,
                CourseCode = g.Key.CourseCode,
                CourseName = g.Key.CourseName,
                TotalStudents = g.Count()
            });

        // Apply search filter if provided
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(c =>
                c.CourseName.Contains(searchString) ||
                c.CourseCode.Contains(searchString));
        }

        // Order by most popular courses first
        query = query.OrderByDescending(c => c.TotalStudents);

        var paginatedList =
            await PaginatedList<CourseMetric>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize);

        ViewBag.TotalCourses = await query.CountAsync();
        return View(paginatedList);
    }


    [HttpGet]
    public async Task<IActionResult> CourseStudents(
        Guid courseId,
        string searchString,
        string academicYear,
        string programFilter,
        int? pageNumber)
    {
        const int pageSize = 20;

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
        {
            return NotFound();
        }

        ViewData["CourseInfo"] = course;
        ViewData["CurrentFilter"] = searchString;
        ViewData["AcademicYearFilter"] = academicYear;
        ViewData["ProgramFilter"] = programFilter;

        var studentsQuery = _context.StudentCourseRegistrations
            .Include(sr => sr.Student)
            .ThenInclude(s => s.CollegeProgram)
            .Include(sr => sr.Semester).Include(sr => sr.Semester.AcademicYear)
            .Where(sr => sr.CourseId == courseId && sr.IsDeleted == false);

        if (!string.IsNullOrEmpty(searchString))
        {
            studentsQuery = studentsQuery.Where(sr =>
                sr.Student.Surname.Contains(searchString) ||
                sr.Student.OtherNames.Contains(searchString) ||
                sr.Student.ApplicationNumber.Contains(searchString)|| 
                sr.Student.StudentID.Contains(searchString));
        }

        if (!string.IsNullOrEmpty(academicYear))
        {
            studentsQuery = studentsQuery.Where(sr => sr.Semester.AcademicYear.Year == academicYear);
        }

        if (!string.IsNullOrEmpty(programFilter))
        {
            studentsQuery = studentsQuery.Where(sr =>
                sr.Student.CollegeProgram.ProgramName == programFilter);
        }

        // Initialize with empty lists if null
        ViewBag.AcademicYears = await studentsQuery
            .Select(sr => sr.Semester.AcademicYear.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync() ?? new List<string>();

        ViewBag.Programs = await studentsQuery
            .Select(sr => sr.Student.CollegeProgram.ProgramName)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync() ?? new List<string>();

        var paginatedStudents = await PaginatedList<StudentCourseRegistration>.CreateAsync(
            studentsQuery.OrderBy(sr => sr.Student.Surname)
                .ThenBy(sr => sr.Student.OtherNames)
                .AsNoTracking(),
            pageNumber ?? 1,
            pageSize);

        return View(paginatedStudents);
    }

    public async Task<IActionResult> DroppedOutStudents(
        string searchString,
        string programFilter,
        DateTime? fromDate,
        DateTime? toDate,
        int? pageNumber)
    {
        const int pageSize = 20;

        ViewData["CurrentFilter"] = searchString;
        ViewData["ProgramFilter"] = programFilter;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        var query = _context.Students
            .Include(s => s.CollegeProgram)
            .Where(s => s.Status == "Dropped Out");

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

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate >= fromDate);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate <= toDate);
        }

        ViewBag.Programs = await _context.CollegePrograms
            .Select(p => p.ProgramName)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var students = await PaginatedList<Student>.CreateAsync(
            query.OrderByDescending(s => s.DateAdded)
                .AsNoTracking(),
            pageNumber ?? 1,
            pageSize);

        return View(students);
    }
    
    [HttpGet]
    public async Task<IActionResult> ExportDroppedOutToExcel(
        string searchString,
        string programFilter,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = BuildDroppedOutQuery(searchString, programFilter, fromDate, toDate);
        var students = await query.ToListAsync();
        
        var columnMappings = new Dictionary<string, string>
        {
            {"Title", "Title"},
            {"ApplicationNumber", "Application No"},
            {"StudentID", "Student ID"},
            {"Surname", "Last Name"},
            {"OtherNames", "Other Name"},
            {"Gender","Gender"},
            {"Email","Email"},
            {"PhoneNo","PhoneNo"},
            {"CollegeProgram.ProgramName", "Program"},
            {"EnrolmentYear","Enrolment Year"},
            {"LevelOfEntry","Level of Entry"},
            {"CurrentLevel","Current Level"},
            {"ContactAddress","Contact Address"},
            {"CollegeClass.ClassName","Class Name"},
            {"CollegeHall.HallName","College Hall"},
            {"DateOfBirth","Date Of Birth"},
            {"Religion","Religion"},
            {"StatusChangeDate", "Dropped Out Date"},
            {"StatusReason", "Reason"},
        };
    
        var exportService = new ExcelExportService();
        var fileContents = exportService.ExportToExcel(students,columnMappings, "DroppedOutStudents");
    
        return File(fileContents, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"DroppedOutStudents_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private IQueryable<Student> BuildDroppedOutQuery(
        string searchString,
        string programFilter,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = _context.Students
            .Include(s => s.CollegeProgram).Include(i=>i.CollegeClass).Include(i=>i.CollegeHall)
            .Where(s => s.Status == "Dropped Out" && s.IsDeleted == false);

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

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate >= fromDate);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate <= toDate);
        }

        return query.OrderByDescending(s => s.StatusChangeDate);
    }

    public async Task<IActionResult> TransferredStudents(
        string searchString,
        string programFilter,
        string institutionFilter,
        int? pageNumber)
    {
        const int pageSize = 20;

        ViewData["CurrentFilter"] = searchString;
        ViewData["ProgramFilter"] = programFilter;
        ViewData["InstitutionFilter"] = institutionFilter;

        var query = _context.Students
            .Include(s => s.CollegeProgram)
            .Where(s => s.Status == "Transferred" && s.IsDeleted == false);

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

        if (!string.IsNullOrEmpty(institutionFilter))
        {
            query = query.Where(s => s.TransferInstitution.Contains(institutionFilter));
        }

        ViewBag.Programs = await _context.CollegePrograms
            .Select(p => p.ProgramName)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        ViewBag.Institutions = await _context.Students
            .Where(s => s.Status == "Transferred")
            .Select(s => s.TransferInstitution)
            .Distinct()
            .OrderBy(i => i)
            .ToListAsync();

        var students = await PaginatedList<Student>.CreateAsync(
            query.OrderByDescending(s => s.DateAdded)
                .AsNoTracking(),
            pageNumber ?? 1,
            pageSize);

        return View(students);
    }

    [HttpGet]
    public async Task<IActionResult> ExportTransferredToExcel(
        string searchString,
        string programFilter,
        string institutionFilter)
    {
        var query = BuildTransferredQuery(searchString, programFilter, institutionFilter);
        var students = await query.ToListAsync();
        
        var columnMappings = new Dictionary<string, string>
        {
            {"Title", "Title"},
            {"ApplicationNumber", "Student ID"},
            {"Surname", "Last Name"},
            {"OtherNames", "Other Name"},
            {"Gender","Gender"},
            {"Email","Email"},
            {"PhoneNo","PhoneNo"},
            {"CollegeProgram.ProgramName", "Program"},
            {"EnrolmentYear","Enrolment Year"},
            {"LevelOfEntry","Level of Entry"},
            {"CurrentLevel","Current Level"},
            {"ContactAddress","Contact Address"},
            {"CollegeClass.ClassName","Class Name"},
            {"CollegeHall.HallName","College Hall"},
            {"DateOfBirth","Date Of Birth"},
            {"Religion","Religion"},
            {"StatusChangeDate", "Transfer Date"},
            {"StatusReason", "Reason"},
            {"TransferInstitution", "Institution"}
        };

        var exportService = new ExcelExportService();
        var fileContents = exportService.ExportToExcel(students,columnMappings, "TransferredStudents");

        return File(fileContents,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"TransferredStudents_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private IQueryable<Student> BuildTransferredQuery(
        string searchString,
        string programFilter,
        string institutionFilter)
    {
        var query = _context.Students
            .Include(s => s.CollegeProgram).Include(i=>i.CollegeHall).Include(i=>i.CollegeClass)
            .Where(s => s.Status == "Transferred" && s.IsDeleted == false);

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

        if (!string.IsNullOrEmpty(institutionFilter))
        {
            query = query.Where(s => s.TransferInstitution.Contains(institutionFilter));
        }

        return query.OrderByDescending(s => s.StatusChangeDate);
    }
    
    public async Task<IActionResult> DeferredStudents(
        string searchString,
        string programFilter,
        DateTime? fromDate,
        DateTime? toDate,
        int? pageNumber)
    {
        const int pageSize = 20;

        ViewData["CurrentFilter"] = searchString;
        ViewData["ProgramFilter"] = programFilter;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        var query = _context.Students
            .Include(s => s.CollegeProgram)
            .Where(s => s.Status == "Deferred");

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

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate >= fromDate);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate <= toDate);
        }

        ViewBag.Programs = await _context.CollegePrograms
            .Select(p => p.ProgramName)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var students = await PaginatedList<Student>.CreateAsync(
            query.OrderByDescending(s => s.DateAdded)
                .AsNoTracking(),
            pageNumber ?? 1,
            pageSize);

        return View(students);
    }
    
    [HttpGet]
    public async Task<IActionResult> ExportDeferredToExcel(
        string searchString,
        string programFilter,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = BuildDeferredQuery(searchString, programFilter, fromDate, toDate);
        var students = await query.ToListAsync();
        
        var columnMappings = new Dictionary<string, string>
        {
            {"Title", "Title"},
            {"ApplicationNumber", "Application No"},
            {"StudentID", "Student ID"},
            {"Surname", "Last Name"},
            {"OtherNames", "Other Name"},
            {"Gender","Gender"},
            {"Email","Email"},
            {"PhoneNo","PhoneNo"},
            {"CollegeProgram.ProgramName", "Program"},
            {"EnrolmentYear","Enrolment Year"},
            {"LevelOfEntry","Level of Entry"},
            {"CurrentLevel","Current Level"},
            {"ContactAddress","Contact Address"},
            {"CollegeClass.ClassName","Class Name"},
            {"CollegeHall.HallName","College Hall"},
            {"DateOfBirth","Date Of Birth"},
            {"Religion","Religion"},
            {"StatusChangeDate", "Deferred Date"},
            {"StatusReason", "Reason"},
        };
    
        var exportService = new ExcelExportService();
        var fileContents = exportService.ExportToExcel(students,columnMappings, "DeferredStudents");
    
        return File(fileContents, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"DeferredStudents_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private IQueryable<Student> BuildDeferredQuery(
        string searchString,
        string programFilter,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = _context.Students
            .Include(s => s.CollegeProgram).Include(i=>i.CollegeClass).Include(i=>i.CollegeHall)
            .Where(s => s.Status == "Deferred" && s.IsDeleted == false);

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

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate >= fromDate);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StatusChangeDate <= toDate);
        }

        return query.OrderByDescending(s => s.StatusChangeDate);
    }
    
    //Graduation List
    public async Task<IActionResult> GraduatedStudents(
        string searchString,
        string programFilter,
        int? fromDate,
        int? toDate,
        int? pageNumber)
    {
        const int pageSize = 20;

        ViewData["CurrentFilter"] = searchString;
        ViewData["ProgramFilter"] = programFilter;
        ViewData["FromDate"] = fromDate?.ToString();
        ViewData["ToDate"] = toDate?.ToString();

        var query = _context.Students
            .Include(s => s.CollegeProgram)
            .Where(s => s.HasGraduated==true);

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

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.ExpectedCompletionYear >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.ExpectedCompletionYear <= toDate.Value);
        }

        ViewBag.Programs = await _context.CollegePrograms
            .Select(p => p.ProgramName)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var students = await PaginatedList<Student>.CreateAsync(
            query.OrderByDescending(s => s.DateAdded)
                .AsNoTracking(),
            pageNumber ?? 1,
            pageSize);

        return View(students);
    }
    
    [HttpGet]
    public async Task<IActionResult> ExportGraduatedToExcel(
        string searchString,
        string programFilter,
        int? fromDate,
        int? toDate)
    {
        var query = BuildGraduatedQuery(searchString, programFilter, fromDate, toDate);
        var students = await query.ToListAsync();
        
        var columnMappings = new Dictionary<string, string>
        {
            {"Title", "Title"},
            {"ApplicationNumber", "Application No"},
            {"StudentID", "Student ID"},
            {"Surname", "Last Name"},
            {"OtherNames", "Other Name"},
            {"Gender","Gender"},
            {"Email","Email"},
            {"PhoneNo","PhoneNo"},
            {"CollegeProgram.ProgramName", "Program"},
            {"EnrolmentYear","Enrolment Year"},
            {"LevelOfEntry","Level of Entry"},
            {"ExpectedCompletionYear","Graduated Year"},
            {"ContactAddress","Contact Address"},
            {"CollegeClass.ClassName","Class Name"},
            {"CollegeHall.HallName","College Hall"},
            {"DateOfBirth","Date Of Birth"},
            {"Religion","Religion"},
            {"StatusChangeDate", "Graduated Date"},
        };
    
        var exportService = new ExcelExportService();
        var fileContents = exportService.ExportToExcel(students,columnMappings, "GraduatedStudents");
    
        return File(fileContents, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"GraduatedStudents_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private IQueryable<Student> BuildGraduatedQuery(
        string searchString,
        string programFilter,
        int? fromDate,
        int? toDate)
    {
        var query = _context.Students
            .Include(s => s.CollegeProgram).Include(i=>i.CollegeClass).Include(i=>i.CollegeHall)
            .Where(s => s.HasGraduated==true && s.IsDeleted == false);

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

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.ExpectedCompletionYear >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.ExpectedCompletionYear <= toDate.Value);
        }

        return query.OrderByDescending(s => s.StatusChangeDate);
    }
    
    
    //All Students Based on Program, Level, Class
// Controller
public async Task<IActionResult> AllActiveStudents(
    string searchString,
    string? programFilter,
    int? level,
    string? collegeClass,
    int? pageNumber)
{
    const int pageSize = 20;

    ViewData["CurrentFilter"] = searchString;
    ViewData["ProgramFilter"] = programFilter;
    ViewData["Level"] = level;
    ViewData["CollegeClass"] = collegeClass;

    var query = _context.Students
        .Include(s => s.CollegeProgram)
        .Include(s => s.CollegeClass)
        .Include(s => s.CollegeHall)
        .Where(s => s.Status == "Active" && !s.HasGraduated && s.IsDeleted == false);

    if (!string.IsNullOrEmpty(searchString))
    {
        query = query.Where(s =>
            s.Surname.Contains(searchString) ||
            s.OtherNames.Contains(searchString) ||
            s.ApplicationNumber.Contains(searchString)|| 
            s.StudentID.Contains(searchString));
    }

    if (!string.IsNullOrEmpty(programFilter))
    {
        query = query.Where(s => s.CollegeProgram.ProgramName == programFilter);
    }

    if (level.HasValue)
    {
        query = query.Where(s => s.CurrentLevel == level.Value);
    }
    
    if (!string.IsNullOrEmpty(collegeClass))
    {
        query = query.Where(s => s.CollegeClass.ClassName == collegeClass);
    }

    // Get filter options
    ViewBag.Programs = await _context.CollegePrograms
        .Select(p => p.ProgramName)
        .Distinct()
        .OrderBy(p => p)
        .ToListAsync();
    
    ViewBag.Levels = new List<SelectListItem>
    {
        new SelectListItem { Value = "100", Text = "Level 100" },
        new SelectListItem { Value = "200", Text = "Level 200" },
        new SelectListItem { Value = "300", Text = "Level 300" },
        new SelectListItem { Value = "400", Text = "Level 400" }
    };

    ViewBag.CollegeClass = await _context.collegeClasses
        .Include(c => c.collegeProgram) // Include program for filtering
        .OrderBy(c => c.ClassName)
        .Select(c => new {
            Id = c.Id,
            Name = c.ClassName,
            ProgramName = c.collegeProgram.ProgramName
        })
        .ToListAsync();

    var students = await PaginatedList<Student>.CreateAsync(
        query.OrderBy(s => s.Surname)
            .ThenBy(s => s.OtherNames)
            .AsNoTracking(),
        pageNumber ?? 1,
        pageSize);

    return View(students);
}
    
    [HttpGet]
    public async Task<IActionResult> ExportAllActiveToExcel(
        string searchString,
        string programFilter,
        int? level,
        string? collegeClass)
    {
        var query = BuildAllActiveQuery(searchString, programFilter, level, collegeClass);
        var students = await query.ToListAsync();
        
        var columnMappings = new Dictionary<string, string>
        {
            {"Title", "Title"},
            {"ApplicationNumber", "Application No"},
            {"StudentID", "Student ID"},
            {"Surname", "Last Name"},
            {"OtherNames", "Other Name"},
            {"Gender","Gender"},
            {"Email","Email"},
            {"PhoneNo","PhoneNo"},
            {"CollegeProgram.ProgramName", "Program"},
            {"EnrolmentYear","Enrolment Year"},
            {"LevelOfEntry","Level of Entry"},
            {"ExpectedCompletionYear","Expected Completion Year"},
            {"ContactAddress","Contact Address"},
            {"CollegeClass.ClassName","Class Name"},
            {"CollegeHall.HallName","College Hall"},
            {"DateOfBirth","Date Of Birth"},
            {"Religion","Religion"},
        };
    
        var exportService = new ExcelExportService();
        var fileContents = exportService.ExportToExcel(students,columnMappings, "AllActiveStudents");
    
        return File(fileContents, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"AllActiveStudents_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private IQueryable<Student> BuildAllActiveQuery(
        string searchString,
        string programFilter,
        int? level,
        string collegeClass)
    {
        var query = _context.Students
            .Include(s => s.CollegeProgram).Include(i=>i.CollegeClass).Include(i=>i.CollegeHall)
            .Where(s => s.Status=="Active" && s.IsDeleted == false);

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

        if (level.HasValue)
        {
            query = query.Where(s => s.CurrentLevel == level.Value);
        }
        
        if (!string.IsNullOrEmpty(collegeClass))
        {
            query = query.Where(s => s.CollegeClass.ClassName == collegeClass);
        }

        return query.OrderByDescending(s => s.DateAdded);
    }
    
    
    //Student Financial Info
    public async Task<IActionResult> AllFinancialInfoStudents(
    string searchString,
    string? programFilter,
    int? level,
    string? collegeClass,
    int? pageNumber)
{
    const int pageSize = 20;

    ViewData["CurrentFilter"] = searchString;
    ViewData["ProgramFilter"] = programFilter;
    ViewData["Level"] = level;
    ViewData["CollegeClass"] = collegeClass;

    var query = _context.FinancialInfos
        .Include(s=>s.Student)
        .Include(s => s.Student.CollegeProgram)
        .Include(s => s.Student.CollegeClass)
        .Include(s => s.Student.CollegeHall)
        .Where(s => s.Student.Status == "Active" && !s.Student.HasGraduated && s.Student.IsDeleted==false && s.IsDeleted == false);

    if (!string.IsNullOrEmpty(searchString))
    {
        query = query.Where(s =>
            s.Student.Surname.Contains(searchString) ||
            s.Student.OtherNames.Contains(searchString) ||
            s.Student.ApplicationNumber.Contains(searchString)||
            s.Student.Email.Contains(searchString) ||
            s.Student.ApplicationNumber.Contains(searchString)||
            s.Student.StudentID.Contains(searchString)||
            s.SSNITNumber.Contains(searchString));
    }

    if (!string.IsNullOrEmpty(programFilter))
    {
        query = query.Where(s => s.Student.CollegeProgram.ProgramName == programFilter);
    }

    if (level.HasValue)
    {
        query = query.Where(s => s.Student.CurrentLevel == level.Value);
    }
    
    if (!string.IsNullOrEmpty(collegeClass))
    {
        query = query.Where(s => s.Student.CollegeClass.ClassName == collegeClass);
    }

    // Get filter options
    ViewBag.Programs = await _context.CollegePrograms
        .Select(p => p.ProgramName)
        .Distinct()
        .OrderBy(p => p)
        .ToListAsync();
    
    ViewBag.Levels = new List<SelectListItem>
    {
        new SelectListItem { Value = "100", Text = "Level 100" },
        new SelectListItem { Value = "200", Text = "Level 200" },
        new SelectListItem { Value = "300", Text = "Level 300" },
        new SelectListItem { Value = "400", Text = "Level 400" }
    };

    ViewBag.CollegeClass = await _context.collegeClasses
        .Include(c => c.collegeProgram) // Include program for filtering
        .OrderBy(c => c.ClassName)
        .Select(c => new {
            Id = c.Id,
            Name = c.ClassName,
            ProgramName = c.collegeProgram.ProgramName
        })
        .ToListAsync();

    var students = await PaginatedList<FinancialInfo>.CreateAsync(
        query.OrderBy(s => s.Student.Surname)
            .ThenBy(s => s.Student.OtherNames)
            .AsNoTracking(),
        pageNumber ?? 1,
        pageSize);

    return View(students);
}
    
    [HttpGet]
    public async Task<IActionResult> ExportFinancialInfoToExcel(
        string searchString,
        string programFilter,
        int? level,
        string? collegeClass)
    {
        var query = BuildFinancialInfoQuery(searchString, programFilter, level, collegeClass);
        var students = await query.ToListAsync();
        
        var exportedData = students.Select(s => new
        {
            s.SSNITNumber,
            s.Student.Surname,
            s.Student.OtherNames,
            s.Student.ApplicationNumber,
            s.EZwichAccountName,
            s.EZwichAccountNumber,
            s.Student.EnrolmentYear,
            s.Student.LevelOfEntry,
            s.Student.CurrentLevel,
            s.Student.ExpectedCompletionYear, 
            s.Student.CollegeProgram.ProgramName,
            s.Student.DateOfBirth,
            s.Student.Gender,
            s.Student.PhoneNo,
            s.Student.Email,

            // ✅ Static fields
            Nationality = "Ghanaian",
        }).ToList();

        
        var columnMappings = new Dictionary<string, string>
        {
            {"SSNITNumber", "SSNIT Number"},
            {"Student.Surname", "Last Name"},
            {"Student.OtherNames", "Other Name"},
            {"Student.ApplicationNumber", "Student Number"},
            {"EZwichAccountName","E-zwich Account Name"},
            {"EZwichAccountNumber","E-zwich Account Number"},
            {"Student.EnrolmentYear","Enrolment Year"},
            {"Student.LevelOfEntry","Level of Entry"},
            {"Student.CurrentLevel","Current Level"},
            {"Student.ExpectedCompletionYear","Expected Completion Year"},
           // {"ExpectedCompletionYear","Expected Completion Year"},
           {"Student.CollegeProgram.ProgramName", "Programme of Study"},
           {"Student.DateOfBirth","Date Of Birth"},
            {"Student.Gender","Gender"},
            {"Student.PhoneNo","PhoneNo"},
            {"Student.Email","Email"},
            {"Nationality","Nationality"}
        };
    
        var exportService = new ExcelExportService();
        var fileContents = exportService.ExportToExcel(students,columnMappings, "StudentFinancialInfo");
    
        return File(fileContents, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"StudentFinancialInfo_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private IQueryable<FinancialInfo> BuildFinancialInfoQuery(
        string searchString,
        string programFilter,
        int? level,
        string collegeClass)
    {
        var query = _context.FinancialInfos
            .Include(s=>s.Student)
            .Include(s => s.Student.CollegeProgram)
            .Include(s => s.Student.CollegeClass)
            .Include(s => s.Student.CollegeHall)
            .Where(s => s.Student.Status == "Active" && !s.Student.HasGraduated && s.IsDeleted == false);


        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(s =>
                s.Student.Surname.Contains(searchString) ||
                s.Student.OtherNames.Contains(searchString) ||
                s.Student.ApplicationNumber.Contains(searchString)||
                s.SSNITNumber.Contains(searchString));
        }

        if (!string.IsNullOrEmpty(programFilter))
        {
            query = query.Where(s => s.Student.CollegeProgram.ProgramName == programFilter);
        }

        if (level.HasValue)
        {
            query = query.Where(s => s.Student.CurrentLevel == level.Value);
        }
        
        if (!string.IsNullOrEmpty(collegeClass))
        {
            query = query.Where(s => s.Student.CollegeClass.ClassName == collegeClass);
        }

        return query.OrderByDescending(s => s.DateAdded);
    }
    
    [HttpGet]
    public async Task<IActionResult> LegacyOutstandingFeesReport(
        string? searchTerm,
        string? academicYear,
        DateTime? startDate,
        DateTime? endDate,
        int pageIndex = 1,
        int pageSize = 20)
    {
        ViewBag.SearchTerm = searchTerm;
        ViewBag.AcademicYear = academicYear;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;

        var query = _context.LegacyOutstandingFees
            .Include(l => l.Student)
            .Where(l => l.IsDeleted==false)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(l =>
                l.Student.Surname.Contains(searchTerm) ||
                l.Student.OtherNames.Contains(searchTerm) ||
                l.Student.ApplicationNumber.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(academicYear))
        {
            query = query.Where(l => l.AcademicYear == academicYear);
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.DateAdded >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.DateAdded <= endDate.Value);
        }

        query = query.OrderBy(l => l.Student.Surname)
                     .ThenBy(l => l.Student.OtherNames)
                     .ThenBy(l => l.AcademicYear);

        var pagedList = await PaginatedList<LegacyOutstandingFee>.CreateAsync(query, pageIndex, pageSize);

        // Aggregate totals for header cards
        decimal totalLegacyOutstanding = await query.SumAsync(l => (decimal?)l.Amount) ?? 0;

        ViewBag.TotalLegacyOutstanding = totalLegacyOutstanding;

        // For AcademicYear dropdown (distinct values from table)
        var years = await _context.LegacyOutstandingFees
            .Where(l => l.IsDeleted==false && l.AcademicYear != null)
            .Select(l => l.AcademicYear)
            .Distinct()
            .OrderBy(y => y)
            .ToListAsync();

        ViewBag.AcademicYears = years;

        return View(pagedList);
    }

    // GET: Export Legacy Outstanding fees to Excel
    [HttpGet]
    public async Task<IActionResult> ExportLegacyOutstandingFees(
        string? searchTerm,
        string? academicYear,
        DateTime? startDate,
        DateTime? endDate)
    {
        var bytes = await _reportService.DownloadLegacyOutstandingFeesReport(
            searchTerm,
            academicYear,
            startDate,
            endDate);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "LegacyOutstandingFees.xlsx");
    }

}