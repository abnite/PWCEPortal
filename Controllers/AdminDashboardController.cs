using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.Payment;
using PWCEPortal.Models.StudentInfo;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using PWCEPortal.ApplicationClass;
using PWCEPortal.ViewModel.Academic;
using PWCEPortal.ViewModel.Dashboard;
using PWCEPortal.ViewModel.Student;

namespace PWCEPortal.Controllers;

[Authorize (Roles = "System Admin, Finance Officer, Administrator")]
public class AdminDashboardController : Controller
{
    private readonly PortalDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DataHelper _dataHelper;
    private const int PageSize = 30;

    public AdminDashboardController(PortalDbContext context, IConfiguration configuration, IEmailSender emailSender, UserManager<ApplicationUser> userManager, DataHelper dataHelper)
    {
        _context = context;
        _configuration = configuration;
        _emailSender = emailSender;
        _userManager = userManager;
        _emailSender = emailSender;
        _dataHelper = dataHelper;
    }

    public IActionResult CreateBackup()
    {
        return View();
    }
     // GET: /DatabaseBackup/Backup
    public async Task<IActionResult> Backup()
{
    var connectionString = _context.Database.GetDbConnection().ConnectionString;
    var databaseName = _context.Database.GetDbConnection().Database;
    string backupFileName = $"{databaseName}_Backup_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}.bak";

    try
    {
        // 1. Determine a safe backup location
        string backupPath;
        
        // Try SQL Server's default backup location first
        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var cmd = new SqlCommand("SELECT SERVERPROPERTY('InstanceDefaultBackupPath')", conn);
            var defaultPath = (await cmd.ExecuteScalarAsync())?.ToString();

            backupPath = !string.IsNullOrEmpty(defaultPath) 
                ? Path.Combine(defaultPath, backupFileName)
                : Path.Combine(Path.GetTempPath(), backupFileName);
        }

        // 2. Execute the backup command
        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var cmd = new SqlCommand(
                $"BACKUP DATABASE [{databaseName}] TO DISK = @backupPath WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10", 
                conn);
            
            cmd.Parameters.AddWithValue("@backupPath", backupPath);
            await cmd.ExecuteNonQueryAsync();
        }

        // 3. Verify and return the backup file
        if (!System.IO.File.Exists(backupPath))  // Explicitly use System.IO.File
        {
            throw new Exception($"Backup file not found at: {backupPath}. " +
                "Check SQL Server service account permissions to write to this location.");
        }

        // Read and delete the temporary backup file
        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(backupPath);  // Explicitly use System.IO.File
        System.IO.File.Delete(backupPath);  // Explicitly use System.IO.File

        // Return the file using the Controller's File method
        return File(fileBytes, "application/octet-stream", backupFileName);
    }
    catch (Exception ex)
    {
        //_logger.LogError(ex, "Database backup failed");
        return BadRequest($"Backup failed: {ex.Message}");
    }
}

     public async Task<IActionResult> Backup12()
{
    // Get the current connection
    var connection = _context.Database.GetDbConnection();
    if (connection == null)
    {
        return BadRequest("Database connection is not available.");
    }
    
    // Retrieve the database name, and provide a default if null
    string databaseName = connection.Database;
    if (string.IsNullOrEmpty(databaseName))
    {
        // You might decide to throw an error or set a default name.
        databaseName = "DefaultDB";
    }

    // Define a backup file name with a timestamp
    string cleanDatabaseName = databaseName.Replace("\0", "");
    string backupFileName = cleanDatabaseName +"_Backup_"+DateTime.UtcNow +".bak";
    // Use a temporary folder (or a specific folder with write permissions)
    string backupFolder = Path.Combine(Path.GetTempPath(), "DatabaseBackups");
    if (!Directory.Exists(backupFolder))
    {
        Directory.CreateDirectory(backupFolder);
    }

    string backupFilePath = Path.Combine(backupFolder, backupFileName);

    // Execute the BACKUP DATABASE command
    try
    {
        using (var sqlConnection = new SqlConnection(connection.ConnectionString))
        {
            await sqlConnection.OpenAsync();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText =
                    $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupFilePath}' WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10";
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    catch (Exception ex)
    {
        // Log the exception as needed and return an error message
        return BadRequest("Error during backup: " + ex.Message);
    }

    // Read the backup file into a byte array and then delete the temporary file
    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(backupFilePath);
    System.IO.File.Delete(backupFilePath);

    // Return the backup file as a download
    return File(fileBytes, "application/octet-stream", backupFileName);
}


     // GET
    [Authorize(Roles = "System Admin")]
    public async Task<IActionResult> Index_Old()
    {
        // Total registered students.
        var totalStudents = await _context.Students.CountAsync();
        int studentCount = 0;
        var students = await _userManager.GetUsersInRoleAsync("Student");
        studentCount=students.Count();
        int pendingVerification=totalStudents-studentCount;
        // Sum of verified fee payments.
        var totalFeesCollected = await _context.Payments
            .Where(p => p.IsVerified)
            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

        // Count pending payment verifications.
        var pendingVerifications = await _context.Payments
            .Where(p => !p.IsVerified)
            .CountAsync();

        // Sum outstanding fees from students (assumes you store this in Student.OutstandingFees).
        var outstandingFees = await _context.StudentFeeAssignments.Where(i=>i.AcademicYear.IsActive).SumAsync(s => (decimal?)s.OutstandingFee) ?? 0;

        // Retrieve the active academic year.
        var activeAcademicYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        string activeYearText = activeAcademicYear != null ? activeAcademicYear.Year : "N/A";

        var model = new AdminDashboardViewModel
        {
            TotalStudents = totalStudents,
            TotalFeesCollected = totalFeesCollected,
            OutstandingFees = outstandingFees,
            PendingVerifications = pendingVerification,
            ActiveAcademicYear = activeYearText
        };

        //return RedirectToAction("AcademicYears");

        return View(model);
    }

    public async Task<IActionResult> Index()
    {
        // 1. Basic Stats
        
            var totalStudents = await _context.Students.Where(i=>i.IsDeleted==false && i.Status=="Active" && i.HasGraduated==false).CountAsync();
            int studentCount = 0;
            var students = await _userManager.GetUsersInRoleAsync("Student");
            studentCount=students.Count();
            int pendingVerification=totalStudents-studentCount;
            var totalFeesCollected = await _context.Payments
                .Where(p => p.IsVerified)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;
            var outstandingFees = await _context.StudentFeeAssignments
                .Where(i => i.AcademicYear.IsActive)
                .SumAsync(s => (decimal?)s.OutstandingFee) ?? 0;
            var pendingVerifications = await _context.Payments
                .Where(p => !p.IsVerified)
                .CountAsync();

            // 2. Monthly Payment Data
            var monthlyPayments = await _context.Payments
                .Where(p => p.IsVerified)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAmount = g.Sum(p => p.AmountPaid)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            // Ensure we have at least 6 months of data
            var monthLabels = new List<string>();
            var paymentData = new List<decimal>();
            var currentDate = DateTime.Now;
            
            for (int i = 5; i >= 0; i--)
            {
                var date = currentDate.AddMonths(-i);
                var monthData = monthlyPayments.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);
                
                monthLabels.Add(date.ToString("MMM yyyy"));
                paymentData.Add(monthData?.TotalAmount ?? 0);
            }

            // 3. Level Breakdown Data
            var levelBreakdown = await _context.Payments
                .Where(p => p.IsVerified)
                .GroupBy(p => p.Student.CurrentLevel)
                .Select(g => new {
                    Level = g.Key,
                    TotalAmount = g.Sum(p => p.AmountPaid)
                })
                .OrderBy(g => g.Level)
                .ToListAsync();

            var levelLabels = levelBreakdown.Select(x => $"Level {x.Level}").ToList();
            var levelData = levelBreakdown.Select(x => x.TotalAmount).ToList();

            // 4. Recent Activities
            var recentPayments = await _context.Payments
                .Include(p => p.Student)
                .Where(p => p.IsVerified)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalStudents = totalStudents,
                TotalFeesCollected = totalFeesCollected,
                OutstandingFees = outstandingFees,
                PendingVerifications = pendingVerification,
                ActiveAcademicYear = (await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive))?.Year ?? "N/A",
                MonthlyPaymentLabels = monthLabels,
                MonthlyPaymentData = paymentData,
                LevelLabels = levelLabels,
                LevelData = levelData,
                RecentPayments = recentPayments
            };

            return View(model);
        }
    
    
    // ------------------- Academic Year Management -------------------

    // GET: List all Academic Years
    [Authorize(Roles = "System Admin")]
    public async Task<IActionResult> AcademicYears()
    {
        var academicYears = await _context.AcademicYears.Where(i=>i.IsDeleted==false).OrderByDescending(a =>  a.IsActive).ThenByDescending(i=>i.DateAdded).ToListAsync();
        return View(academicYears);
    }

    // GET: Create Academic Year
    [Authorize(Roles = "System Admin")]
    [HttpGet]
    public IActionResult CreateAcademicYear()
    {
        return View();
    }

    // POST: Create Academic Year (set new one as active)
    [Authorize(Roles = "System Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAcademicYear(AcademicYear model)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var getYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.Year == model.Year);
                if (getYear != null)
                {
                    TempData["ErrorMessage"] = "Academic Year Already Exists";
                    return View();
                }

                // Deactivate any currently active academic years.
                var activeYears = await _context.AcademicYears.Where(a => a.IsActive).ToListAsync();
                foreach (var ay in activeYears)
                {
                    ay.IsActive = false;
                }

                // Mark new academic year as active.
                model.IsActive = true;
                _context.AcademicYears.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Academic Year created and activated successfully.";
                return RedirectToAction(nameof(AcademicYears));
            }
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = e.Message;

        }

        return View(model);
    }
    
    
    // EDIT ACADEMIC YEAR
    [Authorize(Roles = "System Admin")]
    [HttpGet]
    public async Task<IActionResult> EditAcademicYear(Guid id)
    {
        var academicYear = await _context.AcademicYears.FindAsync(id);
        if (academicYear == null)
            return NotFound();
        return View(academicYear);
    }

    [Authorize(Roles = "System Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAcademicYear(AcademicYear model)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _context.AcademicYears.Update(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Academic Year updated successfully.";
                return RedirectToAction("AcademicYears");
            }
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = e.Message;

        }

        return View(model);
    }

// DELETE ACADEMIC YEAR
    [Authorize(Roles = "System Admin")]
    [HttpGet]
    public async Task<IActionResult> DeleteAcademicYear(Guid id)
    {
        var academicYear = await _context.AcademicYears.FindAsync(id);
        if (academicYear == null)
            return NotFound();
        return View(academicYear);
    }

    [Authorize(Roles = "System Admin")]
    [HttpPost, ActionName("DeleteAcademicYear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAcademicYearConfirmed(Guid id)
    {
        try
        {
            var academicYear = await _context.AcademicYears.FindAsync(id);
            if (academicYear != null)
            {
                academicYear.IsDeleted = true;
                academicYear.DateDeleted = DateTime.Now;
                academicYear.IsActive = false;
                _context.AcademicYears.Update(academicYear);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Academic Year deleted successfully.";
            }
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = e.Message;

        }

        return RedirectToAction("AcademicYears");
    }
    
    
    //---------Academic Semester---------//
     [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageAcademicSemesters()
        {
            var academicSemesters = await _context.AcademicSemesters.Where(i=>i.IsDeleted==false).Include(p => p.AcademicYear).ToListAsync();
            return View(academicSemesters);
        }

        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public IActionResult CreateAcademicSemester()
        {
           var academicYear = _context.AcademicYears.Where(i => i.IsActive).FirstOrDefault();
           ViewBag.academicYear = academicYear.Year;
           ViewBag.academicYearId = academicYear.Id;
            return View();
        }

        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAcademicSemester(AcademicSemester model)
        {
            try
            {
                var academicYear = _context.AcademicYears.Where(i => i.IsActive).FirstOrDefault();
                ViewBag.academicYear = academicYear.Year;
                ViewBag.academicYearId = academicYear.Id;
                ModelState.Remove("AcademicYear");
                if (ModelState.IsValid)
                {
                    var getAcademicSemester = await _context.AcademicSemesters
                        .Where(i => i.AcademicYearId == model.AcademicYearId).FirstOrDefaultAsync();
                    if (getAcademicSemester != null)
                    {
                        TempData["ErrorMessage"] = "Academic Semester for the current academic year  already exists.";
                    }
                    // Deactivate any currently active academic Semester.
                    var activeYears = await _context.AcademicSemesters.Where(a => a.IsRegistrationActive).ToListAsync();
                    foreach (var ay in activeYears)
                    {
                        ay.IsRegistrationActive = false;
                    }

                    model.IsRegistrationActive = true;
                    _context.AcademicSemesters.Add(model);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Academic Semester is created successfully.";
                        return RedirectToAction(nameof(ManageAcademicSemesters));
                    
                }
                else
                {
                    var errorMessage = "";
                    // Log or display validation errors
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        // Log the error message or add it to a list to display to the user
                        errorMessage = error.ErrorMessage;
                        // Handle the error message as needed
                    }

                    TempData["ErrorMessage"] = "Part-payment configuration could not be added." + errorMessage;
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }
        
        // GET: Edit Part Payment Configuration
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public async Task<IActionResult> EditAcademicSemester(Guid id)
        {
            var academicSemester = await _context.AcademicSemesters
                .Include(p => p.AcademicYear)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (academicSemester == null)
                return NotFound();
            return View(academicSemester);
        }

// POST: Edit Part Payment Configuration
    [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAcademicSemester(AcademicSemester model)
        {
            try
            {
                ModelState.Remove("AcademicYear");
                if (ModelState.IsValid)
                {
                    _context.AcademicSemesters.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Academic Semester updated successfully.";
                    return RedirectToAction("ManageAcademicSemesters");
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }
        
        
    public async Task<IActionResult> DeleteAcademicSemester(Guid id)
    {
        var semester = await _context.AcademicSemesters.Include(i=>i.AcademicYear).FirstOrDefaultAsync(i=>i.Id==id);
        if (semester == null)
            return NotFound();
        return View(semester);
    }

    [Authorize(Roles = "System Admin")]
    [HttpPost, ActionName("DeleteAcademicSemester")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAcademicSemesterConfirmed(Guid id)
    {
        try
        {
            var semester = await _context.AcademicSemesters.FindAsync(id);
            if (semester != null)
            {
                semester.IsDeleted = true;
                semester.DateDeleted = DateTime.Now;
                semester.IsRegistrationActive = false;
                _context.AcademicSemesters.Update(semester);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Academic Semester deleted successfully.";
            }
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = e.Message;

        }

        return RedirectToAction("ManageAcademicSemesters");
    }
    
    
    public async Task<IActionResult> ActivateSemester(Guid id)
    {
        var semester = await _context.AcademicSemesters.Include(i=>i.AcademicYear).FirstOrDefaultAsync(i=>i.Id==id);
        if (semester == null)
            return NotFound();
        return View(semester);
    }

    [Authorize(Roles = "System Admin")]
    [HttpPost, ActionName("ActivateAcademicSemester")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateAcademicSemesterConfirmed(Guid id)
    {
        try
        {
            var semester = await _context.AcademicSemesters.FindAsync(id);
            if (semester != null)
            {
                semester.IsDeleted = false;
                semester.DateDeleted = null;
                semester.IsRegistrationActive = true;
                _context.AcademicSemesters.Update(semester);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Academic Semester Activated successfully.";
            }
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = e.Message;

        }

        return RedirectToAction("ManageAcademicSemesters");
    }


    
    
        // ------------------- Fee Structure Management -------------------

        // GET: Manage Fee Structures
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public async Task<IActionResult> ManageFeeStructures()
        {
            var feeStructures = await _context.FeeStructures.Include(f => f.AcademicYear).Where(i=>i.IsDeleted==false).OrderByDescending(i=>i.AcademicYear.IsActive).ThenByDescending(f => f.DateAdded).ToListAsync();
            return View(feeStructures);
        }

        // GET: Create Fee Structure
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public IActionResult CreateFeeStructure()
        {
              ViewBag.academicYear = _context.AcademicYears.Where(i => i.IsActive).Select(i => i.Id).FirstOrDefault();
              ViewBag.academicYearName = _context.AcademicYears.Where(i => i.IsActive).Select(i => i.Year).FirstOrDefault();
            return View();
        }

        // POST: Create Fee Structure
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFeeStructure(FeeStructure model)
        {
            try
            {
                ModelState.Remove("AcademicYear");
                ViewBag.academicYear = _context.AcademicYears.Where(i => i.IsActive).Select(i => i.Id).FirstOrDefault();
                if (ModelState.IsValid)
                {
                    var feeStructure = _context.FeeStructures
                        .Where(i => i.AcademicYearId == model.AcademicYearId && i.Level == model.Level)
                        .FirstOrDefault();
                    if (feeStructure != null)
                    {
                        TempData["ErrorMessage"] = "Fee structure already exists.";
                        return View();
                    }

                    _context.FeeStructures.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Fee Structure added successfully.";
                    return RedirectToAction(nameof(ManageFeeStructures));
                }
                else
                {
                    var errorMessage = "";
                    // Log or display validation errors
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        // Log the error message or add it to a list to display to the user
                        errorMessage = error.ErrorMessage;
                        // Handle the error message as needed
                    }

                    TempData["ErrorMessage"] = "Fee structure could not be added." + errorMessage;
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }
        
        // EDIT FEE STRUCTURE
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public async Task<IActionResult> EditFeeStructure(Guid id)
        {
            var feeStructure = await _context.FeeStructures.FindAsync(id);
            if (feeStructure == null)
                return NotFound();
            return View(feeStructure);
        }

        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFeeStructure(FeeStructure model)
        {
            try
            {
                ModelState.Remove("AcademicYear");
                if (ModelState.IsValid)
                {
                    _context.FeeStructures.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Fee Structure updated successfully.";
                    return RedirectToAction("ManageFeeStructures");
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }

    // DELETE FEE STRUCTURE
    [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public async Task<IActionResult> DeleteFeeStructure(Guid id)
        {
            var feeStructure = await _context.FeeStructures.Include(f => f.AcademicYear).FirstOrDefaultAsync(f => f.Id == id);
            if (feeStructure == null)
                return NotFound();
            return View(feeStructure);
        }

    [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpPost, ActionName("DeleteFeeStructure")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeeStructureConfirmed(Guid id)
        {
            try
            {
                var feeStructure = await _context.FeeStructures.FindAsync(id);
                if (feeStructure != null)
                {
                    feeStructure.IsDeleted = true;
                    feeStructure.DateDeleted = DateTime.Now;
                    _context.FeeStructures.Update(feeStructure);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Fee Structure deleted successfully.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return RedirectToAction("ManageFeeStructures");
        }
        
        
        // Generate Fees Assignment to Student and their levels
      //  [HttpPost]
       // [ValidateAntiForgeryToken]
       [Authorize(Roles = "System Admin, Finance Officer")]
        public async Task<IActionResult> GenerateFeeAssignmentsForActiveYear()
        {
            try
            {
                // Get the active academic year
                var activeAcademicYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
                if (activeAcademicYear == null)
                {
                    TempData["ErrorMessage"] = "No active academic year found.";
                    return RedirectToAction("AcademicYears");
                }

                // Retrieve the FeeStructures for the active year (keyed by level)
                var feeStructures = await _context.FeeStructures
                    .Where(f => f.AcademicYearId == activeAcademicYear.Id && f.IsDeleted==false)
                    .ToListAsync();

                if (feeStructures.Count == 0)
                {
                    TempData["ErrorMessage"] = "No fee structure found for this "+activeAcademicYear.Year+" academic year.";
                    return RedirectToAction("academicyears");
                }

                // Loop through all students who don't have a fee assignment for this academic year
                var studentsWithoutAssignment = await _context.Students
                    .Where(s =>s.IsDeleted==false && s.Status=="Active" && !s.HasGraduated && !_context.StudentFeeAssignments.Any(fa =>
                        fa.StudentId == s.Id && fa.AcademicYearId == activeAcademicYear.Id))
                    .ToListAsync();

                foreach (var student in studentsWithoutAssignment)
                {
                    var previousFeeAssignment = await _context.StudentFeeAssignments
                        .Where(fa => fa.StudentId == student.Id && fa.AcademicYearId != activeAcademicYear.Id)
                        .OrderByDescending(fa => fa.DateAdded).FirstOrDefaultAsync();
                    
                    decimal arrears = 0;
                    if (previousFeeAssignment != null && previousFeeAssignment.OutstandingFee != 0)
                    {
                        arrears = previousFeeAssignment.OutstandingFee; // Could be positive or negative
    
                        // Reset the previous outstanding balance to zero
                        previousFeeAssignment.OutstandingFee = 0;
                        previousFeeAssignment.PaymentStatus = arrears > 0 ? "Carried Forward" : "Credit Applied";
                        _context.StudentFeeAssignments.Update(previousFeeAssignment);
                    }

                    // If there's a previous fee assignment, use its OutstandingFee; otherwise, assume zero arrears.
                   // decimal arrears = previousFeeAssignment != null ? previousFeeAssignment.OutstandingFee : 0;


                    // Find the fee structure that matches the student's current level
                    var feeStructure = feeStructures.FirstOrDefault(f => f.Level == student.CurrentLevel);
                    if (feeStructure == null)
                    {
                        TempData["ErrorMessage"] = "Fee structure not Create for.";
                    }
                    if (feeStructure != null)
                    {
                        var feeAssignment = new StudentFeeAssignment
                        {
                            StudentId = student.Id,
                            AcademicYearId = activeAcademicYear.Id,
                            FullFee = feeStructure.FullFee,
                            PaymentMade = 0,
                            OutstandingFee = feeStructure.FullFee + arrears,
                            PaymentStatus = "Not Paid"
                        };
                        _context.StudentFeeAssignments.Add(feeAssignment);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] =
                    $"{studentsWithoutAssignment.Count} fee assignments generated for the active academic year.";
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return RedirectToAction("AcademicYears");
        }



        // ------------------- Part-Payment Configuration (Optional) -------------------
        // (If using a separate configuration for minimum part-payment percentage)
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public async Task<IActionResult> ManagePartPaymentConfigs()
        {
            var configs = await _context.PartPaymentConfigs.Where(i=>i.IsDeleted==false).Include(p => p.AcademicYear).ToListAsync();
            return View(configs);
        }

        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public IActionResult CreatePartPaymentConfig()
        {
           var academicYear = _context.AcademicYears.Where(i => i.IsActive).FirstOrDefault();
           ViewBag.academicYear = academicYear.Year;
           ViewBag.academicYearId = academicYear.Id;
            return View();
        }

        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePartPaymentConfig(PartPaymentConfig model)
        {
            try
            {
                var academicYear = _context.AcademicYears.Where(i => i.IsActive).FirstOrDefault();
                ViewBag.academicYear = academicYear.Year;
                ViewBag.academicYearId = academicYear.Id;
                ModelState.Remove("AcademicYear");
                if (ModelState.IsValid)
                {
                    var getConfig = await _context.PartPaymentConfigs
                        .Where(i => i.AcademicYearId == model.AcademicYearId).FirstOrDefaultAsync();
                    if (getConfig == null)
                    {
                        _context.PartPaymentConfigs.Add(model);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Part-payment configuration created successfully.";
                        return RedirectToAction(nameof(ManagePartPaymentConfigs));
                    }
                    else
                    {
                        TempData["ErrorMessage"] =
                            "Part-payment configuration for the current academic year  already exists.";
                    }
                }
                else
                {
                    var errorMessage = "";
                    // Log or display validation errors
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        // Log the error message or add it to a list to display to the user
                        errorMessage = error.ErrorMessage;
                        // Handle the error message as needed
                    }

                    TempData["ErrorMessage"] = "Part-payment configuration could not be added." + errorMessage;
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }
        
        // GET: Edit Part Payment Configuration
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpGet]
        public async Task<IActionResult> EditPartPaymentConfig(Guid id)
        {
            var config = await _context.PartPaymentConfigs
                .Include(p => p.AcademicYear)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (config == null)
                return NotFound();
            return View(config);
        }


        // POST: Edit Part Payment Configuration
  
        [Authorize(Roles = "System Admin, Finance Officer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartPaymentConfig(PartPaymentConfig model)
        {
            try
            {
                ModelState.Remove("AcademicYear");
                if (ModelState.IsValid)
                {
                    _context.PartPaymentConfigs.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Part Payment Configuration updated successfully.";
                    return RedirectToAction("ManagePartPaymentConfigs");
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }
        
        
    [Authorize(Roles = "System Admin, Finance Officer")]
    [HttpGet]
    public async Task<IActionResult> ManageRequiredFees()
    {
        var requiredAmounts = await _context.RequiredFees.Where(i=>i.IsDeleted==false).OrderByDescending(i=>i.IsActive).ThenByDescending(i=>i.DateAdded).ToListAsync();
        return View(requiredAmounts);
    }
    
    [Authorize(Roles = "System Admin, Finance Officer")]
    [HttpGet]
    public IActionResult CreateRequiredFee()
    {
       
        return View();
    }

    [Authorize(Roles = "System Admin, Finance Officer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequiredFee(RequiredFee model)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var getRequirement = await _context.RequiredFees.FirstOrDefaultAsync(a => a.RequiredAmount == model.RequiredAmount && a.IsActive==true && a.IsDeleted==false);
                if (getRequirement != null)
                {
                    TempData["ErrorMessage"] = "Required Amount  already exists";
                    return View();
                }
                
                var getRequiredAmount = await _context.RequiredFees.Where(a => a.IsActive).ToListAsync();
                foreach (var r in getRequiredAmount)
                {
                    r.IsActive = false;
                }
                
                model.IsActive=true;
                _context.RequiredFees.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Required Amount created successfully.";
                return RedirectToAction(nameof(ManageRequiredFees));
                

            }
            else
            {
                var errorMessage = "";
                // Log or display validation errors                                                                         
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    // Log the error message or add it to a list to display to the user                                     
                    errorMessage = error.ErrorMessage;
                    // Handle the error message as needed                                                                   
                }

                TempData["ErrorMessage"] = "Required Amount could not be added." + errorMessage;
            }
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = e.Message;

        }

        return View(model);
    }
    
    // GET: Edit Part Payment Configuration
    [Authorize(Roles = "System Admin, Finance Officer")]
    [HttpGet]
    public async Task<IActionResult> EditRequiredFee(Guid id)
    {
        var config = await _context.RequiredFees.FirstOrDefaultAsync(p => p.Id == id);
        if (config == null)
            return NotFound();
        return View(config);
    }

// POST: Edit Part Payment Configuration
    [Authorize(Roles = "System Admin, Finance Officer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRequiredFee(RequiredFee model)
    {
        try
        {
            ModelState.Remove("AcademicYear");
            if (ModelState.IsValid)
            {
                _context.RequiredFees.Update(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Required Fee Configuration updated successfully.";
                return RedirectToAction("ManageRequiredFees");
            }
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = e.Message;

        }

        return View(model);
    }




    // ------------------- Program Management -------------------

        // GET: List all Programs
        [HttpGet]
        [Authorize(Roles = "System Admin")]
        public async Task<IActionResult> ManagePrograms()
        {
            var programs = await _context.CollegePrograms.Where(i=>i.IsDeleted==false).OrderBy(p => p.ProgramName).ToListAsync();
            return View(programs);
        }

        // GET: Create Program
        [HttpGet]
        [Authorize(Roles = "System Admin")]
        public IActionResult CreateProgram()
        {
            return View();
        }

        // POST: Create Program
        [Authorize(Roles = "System Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProgram(CollegeProgram model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var checkProgram = await _context.CollegePrograms.Where(p => p.ProgramName == model.ProgramName)
                        .FirstOrDefaultAsync();
                    if (checkProgram == null)
                    {
                        _context.CollegePrograms.Add(model);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Program created successfully.";
                        return RedirectToAction(nameof(ManagePrograms));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Program already exists.";
                    }


                }
                else
                {
                    TempData["ErrorMessage"] = "program could not be created.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }
        
        // EDIT PROGRAM
        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> EditProgram(Guid id)
        {
            var program = await _context.CollegePrograms.FindAsync(id);
            if (program == null)
                return NotFound();
            return View(program);
        }

        [Authorize(Roles = "System Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProgram(CollegeProgram model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.CollegePrograms.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Program updated successfully.";
                    return RedirectToAction("ManagePrograms");
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }

        
// DELETE PROGRAM
    [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> DeleteProgram(Guid id)
        {
            var program = await _context.CollegePrograms.FindAsync(id);
            if (program == null)
                return NotFound();
            return View(program);
        }

    [Authorize(Roles = "System Admin")]
        [HttpPost, ActionName("DeleteProgram")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgramConfirmed(Guid id)
        {
            try
            {
                var program = await _context.CollegePrograms.FindAsync(id);
                if (program != null)
                {
                    program.IsDeleted = true;
                    program.DateDeleted = DateTime.Now;
                    _context.CollegePrograms.Update(program);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Program deleted successfully.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;
            }

            return RedirectToAction("ManagePrograms");
        }


        // ------------------- Course Management -------------------

        // GET: List all Courses
        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageCourses()
        {
            var courses = await _context.Courses.Where(i=>i.IsDeleted==false).Include(c => c.CollegeProgram).OrderBy(c => c.CourseName).ToListAsync();
            return View(courses);
        }

        // GET: Create Course
        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public IActionResult CreateCourse()
        {
            // Provide a list of programs for selection.
            ViewBag.Programs = _context.CollegePrograms.Where(i=>i.IsDeleted==false).ToList();
            return View();
        }

        // POST: Create Course
        [Authorize(Roles = "System Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course model)
        {
            try
            {
                ViewBag.Programs = _context.CollegePrograms.Where(i=>i.IsDeleted==false).ToList();
                ModelState.Remove("CollegeProgram");
                
                // sanity: if IsCommon, force ProgramId to null
                if (model.IsCommon)
                    model.CollegeProgramId = null;

                if (!model.IsCommon && model.CollegeProgramId == null)
                {
                    TempData["ErrorMessage"] = "Please select a programme or mark as common.";
                    return View(model);
                }
                if (ModelState.IsValid)
                {
                    var checkCourse = await _context.Courses.Where(c =>
                        /*c.CollegeProgramId == model.CollegeProgramId && */
                        c.CourseName == model.CourseName &&
                        c.Level == model.Level &&
                        c.IsCommon == model.IsCommon &&
                        ((c.IsCommon && model.IsCommon) ||
                         (!c.IsCommon && !model.IsCommon && c.CollegeProgramId == model.CollegeProgramId))
                    ).FirstOrDefaultAsync();
                    if (checkCourse == null)
                    {
                        _context.Courses.Add(model);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Course created successfully.";
                        return RedirectToAction(nameof(ManageCourses));
                    }
                    else
                    {
                        TempData["ErrorMessage"] =
                            "Course could not be added. Course Name already exists for this program.";
                    }


                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(model);
        }

        
        // EDIT COURSE
        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> EditCourse(Guid id)
        {
            var course = await _context.Courses.Include(c => c.CollegeProgram).FirstOrDefaultAsync(c => c.Id == id);
            if (course == null)
                return NotFound();
            ViewBag.Programs = _context.CollegePrograms.Where(i=>i.IsDeleted==false).ToList();
            return View(course);
        }

        [Authorize(Roles = "System Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(Course model)
        {
            try
            {
                ModelState.Remove("CollegeProgram");
                if (model.IsCommon)
                    model.CollegeProgramId = null;

                if (!model.IsCommon && model.CollegeProgramId == null)
                {
                    TempData["ErrorMessage"] = "Please select a programme or mark as common.";
                    ViewBag.Programs = _context.CollegePrograms.Where(i => i.IsDeleted==false).ToList();
                    return View(model);
                }
                
                bool exists = await _context.Courses.AnyAsync(c =>
                    c.Id != model.Id &&
                    c.CourseName == model.CourseName &&
                    c.Level == model.Level &&
                    c.Semester == model.Semester &&
                    c.IsCommon == model.IsCommon &&
                    ((c.IsCommon && model.IsCommon) ||
                     (!c.IsCommon && !model.IsCommon && c.CollegeProgramId == model.CollegeProgramId))
                );

                if (exists)
                {
                    TempData["ErrorMessage"] = "Duplicate course for the same level/semester (check common/programme setting).";
                    ViewBag.Programs = _context.CollegePrograms.Where(i => i.IsDeleted==false).ToList();
                    return View(model);
                }
                
                if (ModelState.IsValid)
                {
                    _context.Courses.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Course updated successfully.";
                    return RedirectToAction("ManageCourses");
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            ViewBag.Programs = _context.CollegePrograms.Where(i=>i.IsDeleted==false).ToList();
            return View(model);
        }

        // DELETE COURSE
        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            var course = await _context.Courses.Include(c => c.CollegeProgram).FirstOrDefaultAsync(c => c.Id == id);
            if (course == null)
                return NotFound();
            return View(course);
        }

        [Authorize(Roles = "System Admin")]
        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseConfirmed(Guid id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course != null)
                {
                    _context.Courses.Remove(course);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Course deleted successfully.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return RedirectToAction("ManageCourses");
        }
        
        
        
        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public IActionResult UploadCourses()
        {
            return View();
        }
        
        [Authorize(Roles = "System Admin")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UploadCourses(IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        TempData["ErrorMessage"] = "Please select an Excel file to upload.";
        return View();
    }

    // Only allow Excel
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (extension != ".xlsx" && extension != ".xls")
    {
        TempData["ErrorMessage"] = "Invalid file type. Please upload an Excel (.xlsx or .xls) file.";
        return View();
    }

    var errors = new List<string>();
    int createdCount = 0;
    int skippedCount = 0;

    try
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet == null)
                {
                    TempData["ErrorMessage"] = "No worksheet found in the uploaded file.";
                    return View();
                }

                // Expected headers (row 1):
                // 1: CourseName
                // 2: CourseCode
                // 3: ProgrammeName (can be empty if IsCommon = true)
                // 4: Level
                // 5: Semester
                // 6: CourseType (Core/Elective)
                // 7: IsCommon (Yes/No, True/False, 1/0)

                int rowCount = worksheet.Dimension.End.Row;

                for (int row = 2; row <= rowCount; row++)
                {
                    var courseName = worksheet.Cells[row, 2].Text.Trim();
                    if (string.IsNullOrWhiteSpace(courseName))
                    {
                        // Empty row – skip
                        continue;
                    }

                    var courseCode = worksheet.Cells[row, 3].Text.Trim();
                    var programmeName = worksheet.Cells[row, 4].Text.Trim();
                    var levelText = worksheet.Cells[row, 5].Text.Trim();
                    var semesterText = worksheet.Cells[row, 6].Text.Trim();
                    var courseType = worksheet.Cells[row, 7].Text.Trim();
                    var isCommonText = worksheet.Cells[row, 8].Text.Trim();

                    // Parse Level
                    if (!int.TryParse(levelText, out int level))
                    {
                        errors.Add($"Row {row}: Invalid Level '{levelText}'.");
                        skippedCount++;
                        continue;
                    }

                    // Parse Semester
                    if (!int.TryParse(semesterText, out int semester) || (semester != 1 && semester != 2))
                    {
                        errors.Add($"Row {row}: Invalid Semester '{semesterText}'. Expected 1 or 2.");
                        skippedCount++;
                        continue;
                    }

                    // Parse IsCommon
                    bool isCommon = false;
                    if (!string.IsNullOrWhiteSpace(isCommonText))
                    {
                        var val = isCommonText.Trim().ToLower();
                        isCommon = val == "yes" || val == "y" || val == "true" || val == "1";
                    }

                    // Resolve programme if NOT common
                    CollegeProgram programme = null;
                    Guid? programmeId = null;

                    if (!isCommon)
                    {
                        if (string.IsNullOrWhiteSpace(programmeName))
                        {
                            errors.Add($"Row {row}: ProgrammeName is required when course is not common.");
                            skippedCount++;
                            continue;
                        }

                        programme = await _context.CollegePrograms
                            .Where(p => p.IsDeleted==false &&
                                        p.ProgramName.ToLower() == programmeName.ToLower())
                            .FirstOrDefaultAsync();

                        if (programme == null)
                        {
                            errors.Add($"Row {row}: Programme '{programmeName}' not found in the system.");
                            skippedCount++;
                            continue;
                        }

                        programmeId = programme.Id;
                    }

                    // CourseType normalisation
                    if (string.IsNullOrWhiteSpace(courseType))
                    {
                        courseType = "Core"; // default
                    }

                    // Check for duplicates
                    bool exists = await _context.Courses.AnyAsync(c =>
                        c.CourseCode == courseCode &&
                        c.Level == level &&
                        c.Semester == semester &&
                        c.IsCommon == isCommon &&
                        (isCommon || c.CollegeProgramId == programmeId));

                    if (exists)
                    {
                        errors.Add($"Row {row}: Course '{courseCode}' at Level {level}, Semester {semester} already exists.");
                        skippedCount++;
                        continue;
                    }

                    // Create course
                    var newCourse = new Course
                    {
                        CourseName = courseName,
                        CourseCode = courseCode,
                        CollegeProgramId = programmeId, // null if IsCommon
                        Level = level,
                        Semester = semester,
                        CourseType = courseType,
                        IsCommon = isCommon
                    };

                    _context.Courses.Add(newCourse);
                    createdCount++;
                }

                await _context.SaveChangesAsync();
            }
        }

        TempData["SuccessMessage"] =
            $"Courses upload completed. Created: {createdCount}, Skipped: {skippedCount}.";

        if (errors.Any())
        {
            // join with <br/> for display
            TempData["ErrorMessage"] = string.Join("<br/>", errors);
        }

        return RedirectToAction("ManageCourses");
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Error processing file: " + ex.Message;
        return View();
    }
}


        
        // ------------------- Student Promotion -------------------

        // GET: List students for promotion
        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> PromoteStudents(string searchString, int? levelFilter, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentLevelFilter"] = levelFilter;

            var studentsQuery = _context.Students
                .Include(s => s.CollegeProgram)
                .Where(s => s.IsDeleted==false && !s.HasGraduated && s.Status=="Active");

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                studentsQuery = studentsQuery.Where(s =>
                    s.ApplicationNumber.Contains(searchString) || s.StudentID.Contains(searchString)||
                    (s.Surname + " " + s.OtherNames).Contains(searchString));
            }

            // Apply level filter if provided
            if (levelFilter.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.CurrentLevel == levelFilter);
            }

            // Order by application number by default
            studentsQuery = studentsQuery.OrderBy(s => s.ApplicationNumber);

            // Pagination
            var paginatedStudents = await PaginatedList<Student>.CreateAsync(
                studentsQuery.AsNoTracking(), 
                pageNumber ?? 1, 
                PageSize);

            return View(paginatedStudents);
        }
        // POST: Toggle promotion eligibility for a student
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromotionEligibility(Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student != null)
            {
                student.IsPromotionEligible = !student.IsPromotionEligible;
                _context.Update(student);
                await _context.SaveChangesAsync();
            
                return Json(new { success = true, isEligible = student.IsPromotionEligible });
            }
            return Json(new { success = false });
        }
        
        // POST: Promote all eligible students
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteAllEligibleStudents()
        {
            try
            {
                var studentsToPromote = await _context.Students
                    .Where(s => s.IsPromotionEligible && s.IsDeleted==false && !s.HasGraduated && s.Status == "Active")
                    .ToListAsync();

                foreach (var student in studentsToPromote)
                {
                    if (student.CurrentLevel == 400)
                    {
                        // Handle graduation
                        student.HasGraduated = true;
                        student.Status = "Graduated";
                        student.ExpectedCompletionYear = DateTime.Now.Year;
                    }
                    else
                    {
                        // Normal promotion
                        student.CurrentLevel += 100;
                    }
                    _context.Update(student);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Successfully promoted {studentsToPromote.Count} students.";
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = $"Error during promotion: {e.Message}";
            }

            return RedirectToAction(nameof(PromoteStudents));
        }
        
        // POST: Demote students of a specific level
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteStudents(int level)
        {
            try
            {
                // Get students at the specified level
                var studentsToDemote = await _context.Students
                    .Where(s => s.CurrentLevel == level && s.IsDeleted == false && !s.HasGraduated && s.Status == "Active")
                    .ToListAsync();

                int affectedCount = 0;

                foreach (var student in studentsToDemote)
                {
                    // Only demote if not already at minimum level (100)
                    if (student.CurrentLevel > 100)
                    {
                        student.CurrentLevel -= 100;
                        _context.Update(student);
                        affectedCount++;
                    }
                }

                await _context.SaveChangesAsync();
        
                return Json(new { 
                    success = true, 
                    affectedCount = affectedCount,
                    message = $"Successfully demoted {affectedCount} students."
                });
            }
            catch (Exception e)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error during demotion: {e.Message}"
                });
            }
        }
        
        
        // POST: Update eligibility in bulk
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBulkEligibility(string level, bool isEligible)
        {
            try
            {
                var query = _context.Students
                    .Where(s => s.IsDeleted == false && !s.HasGraduated && s.Status == "Active");

                if (!string.IsNullOrEmpty(level))
                {
                    int levelInt = int.Parse(level);
                    query = query.Where(s => s.CurrentLevel == levelInt);
                }

                var students = await query.ToListAsync();
                foreach (var student in students)
                {
                    student.IsPromotionEligible = isEligible;
                    _context.Update(student);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = e.Message });
            }
        }

// POST: Toggle eligibility for current page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePageEligibility(List<Guid> studentIds)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => studentIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var student in students)
                {
                    student.IsPromotionEligible = !student.IsPromotionEligible;
                    _context.Update(student);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = e.Message });
            }
        }

        [Authorize(Roles = "System Admin")]
        // POST: Promote a Student (manual promotion)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteStudent(int studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student != null)
                {
                    // Promotion logic: Increase current level by 100 (e.g., 100 -> 200, 200 -> 300 ->400).
                    student.CurrentLevel += 100;
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Student promoted successfully.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return RedirectToAction(nameof(PromoteStudents));
        }
        
        // GET: Bulk Upload Form
        [HttpGet]
        public IActionResult BulkUploadStudents()
        {
            return View();
        }

        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> CollegeHalls()
        {
            var collegeHalls =await _context.CollegeHalls.ToListAsync();
            var model = new HallAndListViewModel()
            {
                Halls = collegeHalls, 
                collegeHall = new CollegeHall()
            };
            return View(model);
        }

        [Authorize(Roles = "System Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollegeHalls(CollegeHall collegeHall)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var getHall = await _context.CollegeHalls.Where(i => i.HallName == collegeHall.HallName)
                        .FirstOrDefaultAsync();
                    if (getHall != null)
                    {
                        TempData["ErrorMessage"] = "Hall already Exists.";
                        return RedirectToAction("CollegeHalls");
                    }

                    collegeHall.AddedBy = _dataHelper.GetLoggedInuser();
                    _context.CollegeHalls.Add(collegeHall);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "College hall added successfully.";
                    return RedirectToAction("CollegeHalls");
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return RedirectToAction("CollegeHalls");
        }

        [Authorize(Roles = "System Admin")]
        [HttpGet]
        public async Task<IActionResult> CollegeClass()
        {
            var collegeclass =await _context.collegeClasses.ToListAsync();
            var model = new ClassAndListViewModel()
            {
                Classes = collegeclass, 
                CollegeClass = new CollegeClass()
            };
            return View(model);
        }

        [Authorize(Roles = "System Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollegeClass(ClassAndListViewModel viewModel)
        {
            try
            {

                ModelState.Remove("CollegeClass.collegeProgram");
                if (ModelState.IsValid)
                {
                    viewModel.Classes = await _context.collegeClasses.Include(c => c.collegeProgram).ToListAsync();
                    var getClass = await _context.collegeClasses.Where(i =>
                        i.collegeProgramId == viewModel.CollegeClass.collegeProgramId &&
                        i.ClassName == viewModel.CollegeClass.ClassName).FirstOrDefaultAsync();
                    if (getClass != null)
                    {
                        TempData["ErrorMessage"] = "Class already exists.";
                        return View(viewModel);

                    }

                    viewModel.CollegeClass.AddedBy = _dataHelper.GetLoggedInuser();
                    _context.collegeClasses.Add(viewModel.CollegeClass);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "College Class added successfully.";
                    return RedirectToAction("collegeClass");
                }

                viewModel.Classes = await _context.collegeClasses.Include(c => c.collegeProgram).ToListAsync();

            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;

            }

            return View(viewModel);
        }
        



        // POST: Process Bulk Upload from Excel
     /*   [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUploadStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid Excel file.");
                return View();
            }

            var students = new List<Student>();

            try
            {
                // EPPlus license context (required for version 5 and above)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        // Assume data is in the first worksheet.
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        // Assuming the first row is header, start from row 2.
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                // Read Program Name from column 4.
                                var programName = worksheet.Cells[row, 4].Text.Trim();
                                var collegeProgramId = _context.CollegePrograms.Where(i => i.ProgramName == programName && i.IsDeleted==false).Select(i=>i.Id).FirstOrDefault();
                                var student = new Student
                                {
                                    ApplicationNumber = worksheet.Cells[row, 1].Text.Trim(),
                                    Surname = worksheet.Cells[row, 2].Text.Trim(),
                                    OtherNames = worksheet.Cells[row, 3].Text.Trim(),
                                    CollegeProgramId = collegeProgramId,
                                    ContactAddress = worksheet.Cells[row, 5].Text.Trim(),
                                    Religion = worksheet.Cells[row, 6].Text.Trim(),
                                    DisabilityStatus = worksheet.Cells[row, 7].Text.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase),
                                    District = worksheet.Cells[row, 8].Text.Trim(),
                                    HomeTown = worksheet.Cells[row, 9].Text.Trim(),
                                    Gender = worksheet.Cells[row, 10].Text.Trim(),
                                    DateOfBirth = DateTime.ParseExact(worksheet.Cells[row, 11].Text.Trim(), "yyyy/MM/dd", CultureInfo.InvariantCulture),
                                    PlaceOfBirth = worksheet.Cells[row, 12].Text.Trim(),
                                    ReligiousDenom = worksheet.Cells[row, 13].Text.Trim(),
                                    PhoneNo = worksheet.Cells[row, 14].Text.Trim(),
                                    MaritalStatus = worksheet.Cells[row, 15].Text.Trim(),
                                    GhanaianLanguagesSpoken = worksheet.Cells[row, 16].Text.Trim(),
                                    EnrolmentYear = int.Parse(worksheet.Cells[row, 17].Text.Trim()),
                                    LevelOfEntry = int.Parse(worksheet.Cells[row, 18].Text.Trim()),
                                    CurrentLevel = int.Parse(worksheet.Cells[row, 19].Text.Trim()),
                                    ExpectedCompletionYear = int.Parse(worksheet.Cells[row, 20].Text.Trim()),
                                    Email = worksheet.Cells[row, 21].Text.Trim()  // If available
                                };

                                students.Add(student);
                            }
                            catch (Exception ex)
                            {
                                // Optionally log the error for this record.
                                // For now, skip faulty records.
                                continue;
                            }
                        }
                    }
                }

                if (students.Any())
                {
                    _context.Students.AddRange(students);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{students.Count} student records uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No valid student records were found in the file.";
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing the file: " + ex.Message);
                return View();
            }

            return RedirectToAction(nameof(BulkUploadStudents));
        }*/
}