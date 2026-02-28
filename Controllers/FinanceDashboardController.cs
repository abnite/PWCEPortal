using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Payment;
using PWCEPortal.ViewModel.Dashboard;

namespace PWCEPortal.Controllers;

[Authorize]
public class FinanceDashboardController : Controller
{
    private readonly PortalDbContext _context;
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;
    private readonly IPaymentService _paymentService;
    private readonly DataHelper _dataHelper;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;

    public FinanceDashboardController(PortalDbContext context, IStudentService studentService,
        IAcademicService academicService, IPaymentService paymentService, DataHelper dataHelper,
        IEmailSender emailSender, UserManager<ApplicationUser> userManager)
    {
        _studentService = studentService;
        _academicService = academicService;
        _paymentService = paymentService;
        _dataHelper = dataHelper;
        _context = context;
        _emailSender = emailSender;
        _userManager = userManager;

    }

    // GET
    public async Task<IActionResult> Index()
    {
        var academicYear = await _academicService.GetCurrentAcademicYearAsync();

        // Retrieve pending payments
        var pendingPayments_old = await _context.Payments
            .Include(p => p.Student)
            .Where(p => !p.IsVerified && p.ReceiptFilePath != null)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
        var pendingPayments = await _context.Payments
            .Include(p => p.Student)
            .Where(p => !p.IsVerified && p.IsDeleted == false)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        int pendingCount = pendingPayments.Count;

        // Total fees collected
        decimal totalFeesCollected = await _context.Payments
            .Where(p => p.IsVerified && p.IsDeleted == false)
            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

        // Total outstanding amount
        decimal totalOutstanding = await _context.StudentFeeAssignments
            .Include(i => i.AcademicYear)
            .Where(p => p.AcademicYear.IsActive)
            .SumAsync(p => (decimal?)p.OutstandingFee) ?? 0;

        // Monthly fee collection for current academic year
        var monthlyPayments = await _context.Payments
            .Include(p => p.CurrentAcademicYear)
            .Where(p => p.IsVerified && p.CurrentAcademicYear.IsActive)
            .GroupBy(p => new { p.PaymentDate.Month })
            .Select(g => new
            {
                Month = g.Key.Month,
                TotalAmount = g.Sum(p => p.AmountPaid)
            })
            .OrderBy(g => g.Month)
            .ToListAsync();

        // Prepare month names and amounts
        var monthNames = System.Globalization.DateTimeFormatInfo.CurrentInfo.MonthNames.Take(12).ToList();
        var monthlyData = new decimal[12];
        foreach (var payment in monthlyPayments)
        {
            monthlyData[payment.Month - 1] = payment.TotalAmount;
        }

        // Recent verified payments (last 5)
        var recentPayments = await _context.Payments
            .Include(p => p.Student)
            .Where(p => p.IsVerified && p.IsDeleted == false)
            .OrderByDescending(p => p.PaymentDate)
            .Take(5)
            .ToListAsync();

        // Outstanding fees by level
        var outstandingByLevel = await _context.StudentFeeAssignments
            .Include(sfa => sfa.Student)
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.AcademicYear.IsActive && sfa.OutstandingFee > 0)
            .GroupBy(sfa => sfa.Student.CurrentLevel)
            .Select(g => new
            {
                Level = g.Key,
                TotalOutstanding = g.Sum(sfa => sfa.OutstandingFee)
            })
            .ToDictionaryAsync(x => x.Level, x => x.TotalOutstanding);

        // Count actual students with arrears
        int studentsWithArrears = await _context.StudentFeeAssignments
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.AcademicYear.IsActive && sfa.OutstandingFee > 0)
            .Select(sfa => sfa.StudentId)
            .Distinct()
            .CountAsync();

        var model = new FinanceDashboardViewModel
        {
            PendingPaymentsCount = pendingCount,
            TotalFeesCollected = totalFeesCollected,
            TotalOutstanding = totalOutstanding,
            PendingPayments = pendingPayments,
            ActiveAcademicYear = academicYear.Year.ToString(),
            MonthlyLabels = monthNames,
            MonthlyData = monthlyData.ToList(),
            RecentPayments = recentPayments,
            OutstandingByLevel = outstandingByLevel,
            StudentsWithArrearsCount = studentsWithArrears
        };

        return View(model);
    }

    public async Task<IActionResult> PendingPayments(string searchQuery = null, int page = 1, int pageSize = 10)
    {
        var (payments, totalCount) = await _paymentService.GetPendingPaymentsAsync(searchQuery, page, pageSize);

        var viewModel = new PendingPaymentsViewModel
        {
            Payments = payments,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            SearchQuery = searchQuery
        };

        return View(viewModel);
    }

    public async Task<IActionResult> PendingPaymentsNoUpload(string searchQuery = null, int page = 1, int pageSize = 10)
    {
        var (payments, totalCount) = await _paymentService.GetPendingPaymentsNoUploadAsync(searchQuery, page, pageSize);

        var viewModel = new PendingPaymentsViewModel
        {
            Payments = payments,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            SearchQuery = searchQuery
        };

        return View(viewModel);
    }


    // POST: FinanceDashboard/VerifyPayment
    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    public async Task<IActionResult> VerifyPayment(Guid paymentId)
    {
        try
        {
            var getpayment = await _context.Payments.FindAsync(paymentId);
            var getcurrentYear = await _context.AcademicYears.Where(i => i.IsActive).FirstOrDefaultAsync();
            var getCurrentFees = await _context.StudentFeeAssignments
                .Where(i => i.StudentId == getpayment.StudentId && i.AcademicYear.Year == getcurrentYear.Year)
                .FirstOrDefaultAsync();
            if (getCurrentFees == null)
            {
                TempData["ErrorMessage"] = "No Fees has been Assigned to this student. Assign Fees and Continue";
                return RedirectToAction(nameof(PendingPayments));
            }

            await _paymentService.VerifyPaymentAsync(paymentId);
            TempData["SuccessMessage"] = "Payment verified successfully! Student has been notified via email.";
        }
        catch (Exception e)
        {
            //_logger.LogError(ex, "Error verifying payment");
            var ErrorMessage = e.Message + " Failed to verify payment. Please try again.";
            TempData["ErrorMessage"] = ErrorMessage;
        }

        return RedirectToAction(nameof(PendingPayments));
    }

    // POST: FinanceDashboard/VerifyPayment
    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    public async Task<IActionResult> VerifyPaymentNoUpload(Guid paymentId, string bankReference,
        string accountantReceipt)
    {
        try
        {
            var getpayment = await _context.Payments.FindAsync(paymentId);
            var getcurrentYear = await _context.AcademicYears.Where(i => i.IsActive).FirstOrDefaultAsync();
            var getCurrentFees = await _context.StudentFeeAssignments
                .Where(i => i.StudentId == getpayment.StudentId && i.AcademicYear.Year == getcurrentYear.Year)
                .FirstOrDefaultAsync();

            if (getCurrentFees == null)
            {
                TempData["ErrorMessage"] = "No Fees has been Assigned to this student. Assign Fees and Continue";
                return RedirectToAction(nameof(PendingPaymentsNoUpload));
            }

            if (string.IsNullOrWhiteSpace(bankReference) || string.IsNullOrWhiteSpace(accountantReceipt))
            {
                TempData["ErrorMessage"] = "Both Bank Reference and Account Receipt numbers are required";
                return RedirectToAction(nameof(PendingPaymentsNoUpload));
            }

            await _paymentService.VerifyPaymentNoUploadAsync(paymentId, bankReference, accountantReceipt);
            TempData["SuccessMessage"] = "Payment verified successfully! Student has been notified via email.";
        }
        catch (Exception e)
        {
            var ErrorMessage = e.Message + " Failed to verify payment. Please try again.";
            TempData["ErrorMessage"] = ErrorMessage;
        }

        return RedirectToAction(nameof(PendingPaymentsNoUpload));
    }

    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    // POST: FinanceDashboard/RejectPayment
    [HttpPost]
    public async Task<IActionResult> RejectPayment(Guid paymentId, string rejectionReason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Rejection reason is required.";
                return RedirectToAction(nameof(PendingPayments));
            }

            await _paymentService.RejectPaymentAsync(paymentId, rejectionReason);
            TempData["SuccessMessage"] = "Payment rejected successfully! Student has been notified with your feedback.";
        }
        catch (Exception e)
        {
            // _logger.LogError(ex, "Error rejecting payment");
            var ErrorMessage = e.Message + " Failed to reject payment. Please try again.";
            TempData["ErrorMessage"] = ErrorMessage;
        }

        return RedirectToAction(nameof(PendingPayments));
    }

    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    public async Task<IActionResult> RejectPaymentNoUpload(Guid paymentId, string rejectionReason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Rejection reason is required.";
                return RedirectToAction(nameof(PendingPaymentsNoUpload));
            }

            await _paymentService.RejectPaymentAsync(paymentId, rejectionReason);
            TempData["SuccessMessage"] = "Payment rejected successfully! Student has been notified with your feedback.";
        }
        catch (Exception e)
        {
            // _logger.LogError(ex, "Error rejecting payment");
            var ErrorMessage = e.Message + " Failed to reject payment. Please try again.";
            TempData["ErrorMessage"] = ErrorMessage;
        }

        return RedirectToAction(nameof(PendingPaymentsNoUpload));
    }

    // GET: FinanceDashboard/FeesCollected
    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    public async Task<IActionResult> FeesCollected(string searchQuery = null, int? level = null, int page = 1,
        int pageSize = 10)
    {
        var (payments, totalCount) = await _paymentService.GetFeesCollectedAsync(searchQuery, level, page, pageSize);

        var viewModel = new FeesCollectedViewModel
        {
            Payments = payments,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            SearchQuery = searchQuery,
            Level = level
        };

        return View(viewModel);
    }

    // GET: FinanceDashboard/OutstandingFees
    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    public async Task<IActionResult> OutstandingFees(string searchQuery = null, int? level = null, int page = 1,
        int pageSize = 10)
    {
        var (outstandingFees, totalCount) =
            await _paymentService.GetOutstandingFeesAsync(searchQuery, level, page, pageSize);

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

    // POST: FinanceDashboard/DownloadFeesCollected
    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    public async Task<IActionResult> DownloadFeesCollected(string searchQuery = null, int? level = null)
    {
        var excelBytes = await _paymentService.DownloadFeesCollectedAsync(searchQuery, level);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "FeesCollected.xlsx");
    }

    // POST: FinanceDashboard/DownloadOutstandingFees
    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    public async Task<IActionResult> DownloadOutstandingFees(string searchQuery = null, int? level = null)
    {
        var excelBytes = await _paymentService.DownloadOutstandingFeesAsync(searchQuery, level);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "OutstandingFees.xlsx");
    }

    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpGet]
    public IActionResult UploadPreviousOutstandingFees()
    {
        return View();
    }

    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpGet]
    public IActionResult DownloadPreviousOutstandingTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage())
        {
            var ws = package.Workbook.Worksheets.Add("Template");

            ws.Cells[1, 1].Value = "ApplicationNumber";
            ws.Cells[1, 2].Value = "AcademicYear (optional)";
            ws.Cells[1, 3].Value = "OutstandingAmount";
            ws.Cells[1, 4].Value = "Description (optional)";

            ws.Cells[1, 1, 1, 4].Style.Font.Bold = true;
            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PreviousOutstandingTemplate.xlsx");
        }
    }

    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPreviousOutstandingFees(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select an Excel file to upload.";
            return View();
        }

        var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
        {
            TempData["ErrorMessage"] = "Only Excel files (.xlsx, .xls) are allowed.";
            return View();
        }

        int totalRows = 0;
        int successCount = 0;
        var errors = new List<string>();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var stream = new MemoryStream())
        {
            await excelFile.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    TempData["ErrorMessage"] = "The Excel file does not contain any worksheet.";
                    return View();
                }

                int row = 2; // assuming row 1 is header
                while (true)
                {
                    var appNo = ws.Cells[row, 1].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(appNo))
                    {
                        // Stop when first completely empty ApplicationNumber is found
                        break;
                    }

                    totalRows++;

                    try
                    {
                        var academicYear = ws.Cells[row, 2].Text?.Trim();
                        var amountText = ws.Cells[row, 3].Text?.Trim();
                        var description = ws.Cells[row, 4].Text?.Trim();

                        if (!decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture,
                                out var amount) ||
                            amount <= 0)
                        {
                            errors.Add($"Row {row}: Invalid amount '{amountText}'.");
                            row++;
                            continue;
                        }

                        // Find student by ApplicationNumber
                        var student = await _context.Students
                            .FirstOrDefaultAsync(s =>
                                s.ApplicationNumber == appNo &&
                                s.IsDeleted == false);

                        var getOutstandingFees = await _context.StudentFeeAssignments
                            .Where(i => i.StudentId == student.Id).OrderByDescending(i => i.DateAdded)
                            .FirstOrDefaultAsync();

                        if (student == null)
                        {
                            errors.Add($"Row {row}: Student with ApplicationNumber '{appNo}' not found.");
                            row++;
                            continue;
                        }

                        // Create legacy outstanding entry
                        var legacyFee = new LegacyOutstandingFee
                        {
                            StudentId = student.Id,
                            AcademicYear = academicYear,
                            Amount = amount,
                            Description = string.IsNullOrWhiteSpace(description)
                                ? "Legacy outstanding balance"
                                : description,
                            AddedBy = _dataHelper.GetLoggedInuser()
                        };

                        // Update running outstanding on Student
                        getOutstandingFees.OutstandingFee += amount;

                        _context.LegacyOutstandingFees.Add(legacyFee);
                        _context.StudentFeeAssignments.Update(getOutstandingFees);

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row}: {ex.Message}");
                    }

                    row++;
                }

                await _context.SaveChangesAsync();
            }
        }

        TempData["SuccessMessage"] =
            $"Upload completed. {successCount} of {totalRows} rows imported successfully.";

        ViewBag.Errors = errors;

        return View();
    }


    //Remove or Delete Legacy Fess
    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpGet]
    public async Task<IActionResult> DeleteLegacyOutstandingFee(Guid id)
    {
        var legacyFee = await _context.LegacyOutstandingFees
            .Include(l => l.Student)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (legacyFee == null)
        {
            return NotFound();
        }

        return View(legacyFee);
    }

    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLegacyOutstandingFeeConfirmed(Guid id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var legacyFee = await _context.LegacyOutstandingFees
                .Include(l => l.Student)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (legacyFee == null)
            {
                return NotFound();
            }

            // Find the most recent fee assignment for the student
            var latestFeeAssignment = await _context.StudentFeeAssignments
                .Where(f => f.StudentId == legacyFee.StudentId)
                .OrderByDescending(f => f.DateAdded)
                .FirstOrDefaultAsync();

            if (latestFeeAssignment != null)
            {
                // Deduct the legacy fee amount from the student's outstanding balance
                latestFeeAssignment.OutstandingFee -= legacyFee.Amount;

                // If you want to track this adjustment, you might want to add a note
                //  latestFeeAssignment.Notes = $"Legacy outstanding fee (ID: {legacyFee.Id}) removed. Amount: {legacyFee.Amount:C}";

                _context.StudentFeeAssignments.Update(latestFeeAssignment);
            }

            // Remove the legacy fee
            _context.LegacyOutstandingFees.Remove(legacyFee);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Legacy outstanding fee of {legacyFee.Amount:C} for student {legacyFee.Student?.ApplicationNumber} has been deleted successfully.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Error deleting legacy fee: {ex.Message}";
        }

        return RedirectToAction("LegacyOutstandingFeesList");
    }


    [Authorize(Roles = "System Admin, Finance Officer, Administrator")]
    [HttpGet]
    public async Task<IActionResult> LegacyOutstandingFeesList(string searchString, string academicYear,
        DateTime? startDate, DateTime? endDate, int? pageIndex)
    {
        ViewBag.CurrentFilter = searchString;
        ViewBag.AcademicYear = academicYear;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;

        // Get distinct academic years for filter dropdown
        ViewBag.AcademicYears = await _context.LegacyOutstandingFees
            .Where(l => l.AcademicYear != null)
            .Select(l => l.AcademicYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        var query = _context.LegacyOutstandingFees
            .Include(l => l.Student).Include(i=>i.AddedBy)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(l =>
                l.Student.ApplicationNumber.Contains(searchString) ||
                (l.Student.Surname + " " + l.Student.OtherNames).Contains(searchString) ||
                l.Description.Contains(searchString));
        }

        if (!string.IsNullOrEmpty(academicYear))
        {
            query = query.Where(l => l.AcademicYear == academicYear);
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.DateAdded >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endDateValue = endDate.Value.AddDays(1);
            query = query.Where(l => l.DateAdded < endDateValue);
        }

        // Calculate total for summary card
        ViewBag.TotalLegacyOutstanding = await query.SumAsync(l => l.Amount);

        // Order by most recent first
        query = query.OrderByDescending(l => l.DateAdded);

        int pageSize = 20;
        return View(await PaginatedList<LegacyOutstandingFee>.CreateAsync(
            query.AsNoTracking(), pageIndex ?? 1, pageSize));
    }

    private async Task SendPaymentVerifiedEmail(Payment payment)
    {
        try
        {
            var student = payment.Student;
            var emailSubject = $"Payment Verified - {student.ApplicationNumber}";

            var emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 20px auto; padding: 20px; }}
                        .header {{ color: #2c3e50; border-bottom: 1px solid #eee; padding-bottom: 10px; }}
                        .details {{ background: #f9f9f9; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ font-size: 0.9em; color: #777; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
                        .button {{ 
                            display: inline-block; padding: 10px 15px; background: #28a745; 
                            color: white; text-decoration: none; border-radius: 4px; margin-top: 10px;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Payment Verified</h2>
                        </div>
                        
                        <p>Dear {student.OtherNames + " " + student.OtherNames},</p>
                        
                        <p>We are pleased to inform you that your payment has been successfully verified.</p>
                        
                        <div class='details'>
                            <p><strong>Payment Details:</strong></p>
                            <p>Amount: GHS {payment.AmountPaid.ToString("N2")}</p>
                            <p>Reference: {payment.PaymentReference}</p>
                            <p>Date Verified: {DateTime.UtcNow.ToString("dd MMMM yyyy")}</p>
                        </div>
                        
                        <p>You may now proceed with your registration:</p>
                        <a href='{Url.Action("Index", "Registration", null, "https")}' class='button'>Continue Registration</a>
                        
                        <div class='footer'>
                            <p>If you have any questions, please contact the admissions office.</p>
                            <p>© {DateTime.UtcNow.Year} PWCE Portal. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailSender.SendGmailEmailAsync(student.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Error sending payment verification email");
            var ErrorMsg = ex.Message + " Error sending payment verification email";
            TempData["ErrorMessage"] = ErrorMsg;
        }
    }

    private async Task SendPaymentRejectedEmail(Payment payment, string rejectionReason)
    {
        try
        {
            var student = payment.Student;
            var emailSubject = $"Payment Rejected - {student.ApplicationNumber}";

            var emailBody = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 20px auto; padding: 20px; }}
                            .header {{ color: #2c3e50; border-bottom: 1px solid #eee; padding-bottom: 10px; }}
                            .details {{ background: #f9f9f9; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                            .rejection {{ background: #fff3f3; padding: 15px; border-left: 4px solid #dc3545; margin: 15px 0; }}
                            .footer {{ font-size: 0.9em; color: #777; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
                            .button {{ 
                                display: inline-block; padding: 10px 15px; background: #0d6efd; 
                                color: white; text-decoration: none; border-radius: 4px; margin-top: 10px;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>Payment Rejected</h2>
                            </div>
                            
                            <p>Dear {student.OtherNames + " " + student.OtherNames},</p>
                            
                            <p>We regret to inform you that your payment could not be verified.</p>
                            
                            <div class='details'>
                                <p><strong>Payment Details:</strong></p>
                                <p>Amount: GHS {payment.AmountPaid.ToString("N2")}</p>
                                <p>Reference: {payment.PaymentReference}</p>
                                <p>Bank Reference: {payment.BankReferenceNumber}</p>
                            </div>
                            
                            <div class='rejection'>
                                <p><strong>Reason for Rejection:</strong></p>
                                <p>{rejectionReason}</p>
                            </div>
                            
                            <p>Please review the reason above and upload a new payment receipt or update your receipt:</p>
                            <a href='{Url.Action("Payments", "StudentDashboard", new { studentId = student.Id }, "https")}' class='button'>Upload New Receipt</a>
                            
                            <div class='footer'>
                                <p>If you need assistance, please contact the finance office.</p>
                                <p>© {DateTime.UtcNow.Year} PWCE Portal. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

            await _emailSender.SendEmailAsync(student.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error sending payment rejection email");
            var ErrorMsg = ex.Message + " Error sending payment rejection email";
            TempData["ErrorMessage"] = ErrorMsg;
        }
    }


}