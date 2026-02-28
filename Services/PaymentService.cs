using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Payment;

namespace PWCEPortal.Services;

public class PaymentService:IPaymentService
{
 
    private readonly ILogger<PaymentService> _logger;
    private readonly PortalDbContext _context;
    private readonly IAcademicService _academicService;
    private readonly DataHelper _dataHelper;
    private readonly IEmailSender _emailSender;

    public PaymentService(IEmailSender emailSender,PortalDbContext context, IAcademicService academicService, DataHelper dataHelper, ILogger<PaymentService> logger)
    {
        _context = context;
        _academicService = academicService;
        _dataHelper = dataHelper;
        _emailSender = emailSender;
      
        _logger = logger;
    }
    public async Task<FeeStructure> GetFeeStructureAsync(Guid studentId, Guid academicYearId)
    {
        var getStudentLevel= await _context.Students.Where(s=>s.Id==studentId).FirstOrDefaultAsync();
        return await _context.FeeStructures.FirstOrDefaultAsync(fs => fs.AcademicYearId == academicYearId && fs.Level==getStudentLevel.CurrentLevel);
    }

    public async Task<List<Payment>> GetPaymentsByStudentIdAsync(Guid studentId)
    {
        // Fetch all payments made by the student
        return await _context.Payments
            .Where(p => p.StudentId == studentId && p.IsDeleted == false)
            .ToListAsync();
    }
    public async Task<List<Payment>> GetCurrentUnVerifiedPaymentsByStudentIdAsync(Guid studentId)
    {
        // Fetch all payments made by the student
        var CurrentYear = await _context.AcademicYears.Where(i => i.IsActive).FirstOrDefaultAsync();
        return await _context.Payments.Where(p => p.StudentId == studentId && p.IsVerified==false && p.CurrentAcademicYearId==CurrentYear.Id && p.IsDeleted == false).ToListAsync();
    }
    
    public async Task<List<Payment>> GetVerifiedPaymentsByStudentIdAsync(Guid studentId)
    {
        // Fetch all payments made by the student
        return await _context.Payments
            .Where(p => p.StudentId == studentId && p.IsVerified && p.IsDeleted == false).ToListAsync();
    }

    public async Task<List<Payment>> GetPendingPaymentsAsync()
    {
        return await _context.Payments
            .Include(p => p.Student)
            .Include(p => p.CurrentAcademicYear)
            .Where(p => !p.IsVerified && p.IsDeleted == false)
            .ToListAsync();
    }

    public async Task<Payment> GetPaymentByIdAsync(Guid paymentId)
    {
        return await _context.Payments
            .Include(p => p.Student)
            .Include(p => p.CurrentAcademicYear)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task UpdatePaymentAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePaymentAsync(Guid paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<StudentFeeAssignment> GetStudentFeeAssignmentAsync(Guid studentId, Guid academicYearId)
    {
        
        return await _context.StudentFeeAssignments.FirstOrDefaultAsync(sfa => sfa.StudentId == studentId && sfa.AcademicYearId == academicYearId);
    }

    public async Task UpdateStudentFeeAssignmentAsync(StudentFeeAssignment feeAssignment)
    {
        _context.StudentFeeAssignments.Update(feeAssignment);
        await _context.SaveChangesAsync();
    }

    public async Task VerifyPaymentAsync(Guid paymentId)
    {
        var payment = await _context.Payments.Include(i=>i.Student).FirstOrDefaultAsync(i=>i.Id == paymentId && i.IsDeleted==false && i.IsVerified==false) 
                      ?? throw new ArgumentException("Payment not found");
        var AcademicYear = await _academicService.GetCurrentAcademicYearAsync();
        var feeAssignment = await GetStudentFeeAssignmentAsync(payment.StudentId, AcademicYear.Id);
        if (payment != null)
        {
            //feeAssignment.OutstandingFee = feeAssignment.OutstandingFee - payment.AmountPaid;
            //feeAssignment.PaymentMade = feeAssignment.PaymentMade+ payment.AmountPaid;
            feeAssignment.OutstandingFee -= payment.AmountPaid;
            feeAssignment.PaymentMade += payment.AmountPaid;
            payment.VerifiedBy = _dataHelper.GetLoggedInuser();
            payment.IsVerified = true;
            payment.VerifiedDate = DateTime.Now;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            await SendPaymentVerifiedEmail(payment);
        }
    }
    
    public async Task VerifyPaymentNoUploadAsync(Guid paymentId, string bankReference, string accountantReceipt)
    {
        var payment = await _context.Payments
                          .Include(i => i.Student)
                          .FirstOrDefaultAsync(i => i.Id == paymentId && i.IsDeleted == false && i.IsVerified == false) 
                      ?? throw new ArgumentException("Payment not found");
        
        var AcademicYear = await _academicService.GetCurrentAcademicYearAsync();
        var feeAssignment = await GetStudentFeeAssignmentAsync(payment.StudentId, AcademicYear.Id);
    
        if (payment != null)
        {
            // Update payment details
            payment.BankReferenceNumber = bankReference;
            payment.AccountantReceipt = accountantReceipt;
            feeAssignment.OutstandingFee -= payment.AmountPaid;
            feeAssignment.PaymentMade += payment.AmountPaid;
            payment.VerifiedBy = _dataHelper.GetLoggedInuser();
            payment.IsVerified = true;
            payment.VerifiedDate = DateTime.UtcNow;
        
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            await SendPaymentVerifiedEmailNoUpload(payment);
        }
    }

    public async Task RejectPaymentAsync(Guid paymentId, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Rejection reason is required");
        
        var payment = await _context.Payments.Include(i=>i.Student).FirstOrDefaultAsync(i=>i.Id == paymentId && i.IsDeleted==false && i.IsVerified==false)
                      ?? throw new ArgumentException("Payment not found");;
        if (payment != null)
        {
            payment.IsVerified = false;
            payment.ReceiptFilePath = null;
            payment.BankReferenceNumber = null;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }
        await SendPaymentRejectedEmail(payment, rejectionReason);
    }

    public async Task<List<Payment>> GetCurrentYearVerifiedPaymentsByStudentIdAsync(Guid studentId)
    {
        // Fetch all payments made by the student
        var CurrentYear = await _context.AcademicYears.Where(i => i.IsActive).FirstOrDefaultAsync();
        return await _context.Payments.Where(p => p.StudentId == studentId && p.IsVerified && p.CurrentAcademicYearId==CurrentYear.Id).ToListAsync();
    }
    
    // Fetch pending payments with pagination and search
    public async Task<(List<Payment> Payments, int TotalCount)> GetPendingPaymentsAsync(
        string searchQuery = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Payments
            .Include(p => p.Student)
            .Include(p => p.CurrentAcademicYear)
            .Where(p => !p.IsVerified && p.ReceiptFilePath!=null);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(p =>
                p.Student.Surname.Contains(searchQuery) ||
                p.Student.OtherNames.Contains(searchQuery) ||
                p.Student.ApplicationNumber.Contains(searchQuery) ||
                p.Student.StudentID.Contains(searchQuery) ||
                p.PaymentReference.Contains(searchQuery) ||
                p.BankReferenceNumber.Contains(searchQuery));
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (payments, totalCount);
    }
    
    public async Task<(List<Payment> Payments, int TotalCount)> GetPendingPaymentsNoUploadAsync(
        string searchQuery = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Payments
            .Include(p => p.Student)
            .Include(p => p.CurrentAcademicYear)
            .Where(p => !p.IsVerified && p.IsDeleted==false);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(p =>
                p.Student.Surname.Contains(searchQuery) ||
                p.Student.OtherNames.Contains(searchQuery) ||
                p.Student.ApplicationNumber.Contains(searchQuery) ||
                p.Student.StudentID.Contains(searchQuery) ||
                p.PaymentReference.Contains(searchQuery) ||
                p.BankReferenceNumber.Contains(searchQuery));
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (payments, totalCount);
    }
    
    // Fetch fees collected with pagination and search
    public async Task<(List<Payment> Payments, int TotalCount)> GetFeesCollectedAsync(
        string searchQuery = null,
        int? level = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Payments
            .Include(p => p.Student)
            .Include(p => p.CurrentAcademicYear)
            .Where(p => p.IsVerified && p.IsDeleted == false);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(p =>
                p.Student.Surname.Contains(searchQuery) ||
                p.Student.OtherNames.Contains(searchQuery) ||
                p.Student.ApplicationNumber.Contains(searchQuery) ||
                p.Student.StudentID.Contains(searchQuery) ||
                p.PaymentReference.Contains(searchQuery) ||
                p.BankReferenceNumber.Contains(searchQuery));
        }

        // Filter by level
        if (level.HasValue)
        {
            query = query.Where(p => p.Student.CurrentLevel == level.Value);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (payments, totalCount);
    }

    // Fetch outstanding fees with pagination and search
    public async Task<(List<StudentFeeAssignment> OutstandingFees, int TotalCount)> GetOutstandingFeesAsync(
        string searchQuery = null,
        int? level = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.StudentFeeAssignments
            .Include(sfa => sfa.Student)
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.OutstandingFee > 0);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(sfa =>
                sfa.Student.Surname.Contains(searchQuery) ||
                sfa.Student.OtherNames.Contains(searchQuery) ||
                sfa.Student.StudentID.Contains(searchQuery) ||
                sfa.Student.ApplicationNumber.Contains(searchQuery));
        }

        // Filter by level
        if (level.HasValue)
        {
            query = query.Where(sfa => sfa.Student.CurrentLevel == level.Value);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var outstandingFees = await query
            .OrderByDescending(sfa => sfa.AcademicYear.Year)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (outstandingFees, totalCount);
    }

    public async Task<(List<StudentFeeAssignment> Overpayment, int TotalCount)> GetOverpaymentFeesAsync(string searchQuery = null, int? level = null, int page = 1, int pageSize = 10)
    {
        var query = _context.StudentFeeAssignments
            .Include(sfa => sfa.Student)
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.OutstandingFee < 0);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(sfa =>
                sfa.Student.Surname.Contains(searchQuery) ||
                sfa.Student.OtherNames.Contains(searchQuery) ||
                sfa.Student.StudentID.Contains(searchQuery) ||
                sfa.Student.ApplicationNumber.Contains(searchQuery));
        }

        // Filter by level
        if (level.HasValue)
        {
            query = query.Where(sfa => sfa.Student.CurrentLevel == level.Value);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var outstandingFees = await query
            .OrderByDescending(sfa => sfa.AcademicYear.Year)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (outstandingFees, totalCount);
    }

    // Download fees collected to Excel
    public async Task<byte[]> DownloadFeesCollectedAsync(string searchQuery = null, int? level = null)
    {
        var query = _context.Payments
            .Include(p => p.Student)
            .Include(p => p.CurrentAcademicYear)
            .Where(p => p.IsVerified && p.IsDeleted == false);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(p =>
                p.Student.Surname.Contains(searchQuery) ||
                p.Student.OtherNames.Contains(searchQuery) ||
                p.Student.ApplicationNumber.Contains(searchQuery) ||
                p.Student.StudentID.Contains(searchQuery) ||
                p.PaymentReference.Contains(searchQuery) ||
                p.BankReferenceNumber.Contains(searchQuery));
        }

        // Filter by level
        if (level.HasValue)
        {
            query = query.Where(p => p.Student.CurrentLevel == level.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        // Convert payments to Excel (using a library like EPPlus or ClosedXML)
        return GenerateExcel(payments);
    }

    // Download outstanding fees to Excel
    public async Task<byte[]> DownloadOutstandingFeesAsync(string searchQuery = null, int? level = null)
    {
        var query = _context.StudentFeeAssignments
            .Include(sfa => sfa.Student)
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.OutstandingFee > 0);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(sfa =>
                sfa.Student.Surname.Contains(searchQuery) ||
                sfa.Student.OtherNames.Contains(searchQuery) ||
                sfa.Student.StudentID.Contains(searchQuery) ||
                sfa.Student.ApplicationNumber.Contains(searchQuery));
        }

        // Filter by level
        if (level.HasValue)
        {
            query = query.Where(sfa => sfa.Student.CurrentLevel == level.Value);
        }

        var outstandingFees = await query
            .OrderByDescending(sfa => sfa.AcademicYear.Year)
            .ToListAsync();

        // Convert outstanding fees to Excel (using a library like EPPlus or ClosedXML)
        return GenerateExcel(outstandingFees);
    }
    
    public async Task<byte[]> DownloadOverpaymentFeesAsync(string searchQuery = null, int? level = null)
    {
        var query = _context.StudentFeeAssignments
            .Include(sfa => sfa.Student)
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.OutstandingFee < 0);

        // Apply search filters
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(sfa =>
                sfa.Student.Surname.Contains(searchQuery) ||
                sfa.Student.OtherNames.Contains(searchQuery) ||
                sfa.Student.StudentID.Contains(searchQuery) ||
                sfa.Student.ApplicationNumber.Contains(searchQuery));
        }

        // Filter by level
        if (level.HasValue)
        {
            query = query.Where(sfa => sfa.Student.CurrentLevel == level.Value);
        }

        var outstandingFees = await query
            .OrderByDescending(sfa => sfa.AcademicYear.Year)
            .ToListAsync();

        // Convert outstanding fees to Excel (using a library like EPPlus or ClosedXML)
        return GenerateExcel(outstandingFees);
    }

    private byte[] GenerateExcel<T>(List<T> data)
    {
        // Set the EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Data");

            // Add headers based on the type of data
            if (typeof(T) == typeof(Payment))
            {
                // Add headers for Payments
                worksheet.Cells[1, 1].Value = "Payment Reference";
                worksheet.Cells[1, 2].Value = "Student Name";
                worksheet.Cells[1, 3].Value = "Application No.";
                worksheet.Cells[1, 4].Value = "Student ID No";
                worksheet.Cells[1, 5].Value = "Amount Paid";
                worksheet.Cells[1, 6].Value = "Payment Date";
                worksheet.Cells[1, 7].Value = "Bank Reference";
                worksheet.Cells[1, 8].Value = "Receipt File Path";

                // Add data for Payments
                var payments = data as List<Payment>;
                for (int i = 0; i < payments.Count; i++)
                {
                    var payment = payments[i];
                    worksheet.Cells[i + 2, 1].Value = payment.PaymentReference;
                    worksheet.Cells[i + 2, 2].Value = $"{payment.Student.Surname} {payment.Student.OtherNames}";
                    worksheet.Cells[i + 2, 3].Value = payment.Student.ApplicationNumber;
                    worksheet.Cells[i + 2, 4].Value = payment.Student.StudentID??"N/A";
                    worksheet.Cells[i + 2, 5].Value = payment.AmountPaid;
                    worksheet.Cells[i + 2, 6].Value = payment.PaymentDate.ToString("dd MMM yyyy HH:mm");
                    worksheet.Cells[i + 2, 7].Value = payment.BankReferenceNumber;
                    worksheet.Cells[i + 2, 8].Value = payment.ReceiptFilePath;
                }
            }
            else if (typeof(T) == typeof(StudentFeeAssignment))
            {
                var assignments = data as List<StudentFeeAssignment>;

                if (assignments.Any(a => a.OutstandingFee > 0))
                {
                    // Outstanding Fees
                    worksheet.Cells[1, 1].Value = "Student Name";
                    worksheet.Cells[1, 2].Value = "Application No.";
                    worksheet.Cells[1, 3].Value = "Student ID No.";
                    worksheet.Cells[1, 4].Value = "Academic Year";
                    worksheet.Cells[1, 5].Value = "Full Fee";
                    worksheet.Cells[1, 6].Value = "Payment Made";
                    worksheet.Cells[1, 7].Value = "Outstanding Fee";

                    for (int i = 0; i < assignments.Count; i++)
                    {
                        var fee = assignments[i];
                        worksheet.Cells[i + 2, 1].Value = $"{fee.Student.Surname} {fee.Student.OtherNames}";
                        worksheet.Cells[i + 2, 2].Value = fee.Student.ApplicationNumber;
                        worksheet.Cells[i + 2, 3].Value = fee.Student.StudentID??"N/A";
                        worksheet.Cells[i + 2, 4].Value = fee.AcademicYear.Year;
                        worksheet.Cells[i + 2, 5].Value = fee.FullFee;
                        worksheet.Cells[i + 2, 6].Value = fee.PaymentMade;
                        worksheet.Cells[i + 2, 7].Value = fee.OutstandingFee;
                    }
                }
                else if (assignments.Any(a => a.OutstandingFee < 0))
                {
                    // Overpaid Fees
                    worksheet.Cells[1, 1].Value = "Student Name";
                    worksheet.Cells[1, 2].Value = "Application No.";
                    worksheet.Cells[1, 3].Value = "Student ID No.";
                    worksheet.Cells[1, 4].Value = "Academic Year";
                    worksheet.Cells[1, 5].Value = "Full Fee";
                    worksheet.Cells[1, 6].Value = "Payment Made";
                    worksheet.Cells[1, 7].Value = "Overpaid Amount";

                    for (int i = 0; i < assignments.Count; i++)
                    {
                        var fee = assignments[i];
                        worksheet.Cells[i + 2, 1].Value = $"{fee.Student.Surname} {fee.Student.OtherNames}";
                        worksheet.Cells[i + 2, 2].Value = fee.Student.ApplicationNumber;
                        worksheet.Cells[i + 2, 3].Value = fee.Student.StudentID??"N/A";
                        worksheet.Cells[i + 2, 4].Value = fee.AcademicYear.Year;
                        worksheet.Cells[i + 2, 5].Value = fee.FullFee;
                        worksheet.Cells[i + 2, 6].Value = fee.PaymentMade;
                        worksheet.Cells[i + 2, 7].Value = Math.Abs(fee.OutstandingFee); // Show positive overpaid value
                    }
                }
            }


            // Auto-fit columns for better readability
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Convert the Excel package to a byte array
            return package.GetAsByteArray();
        }
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
                        
                        <p>Dear {student.OtherNames +" "+student.OtherNames},</p>
                        
                        <p>We are pleased to inform you that your payment has been successfully verified.</p>
                        
                        <div class='details'>
                            <p><strong>Payment Details:</strong></p>
                            <p>Amount: GHS {payment.AmountPaid.ToString("N2")}</p>
                            <p>Reference: {payment.PaymentReference}</p>
                            <p>Date Verified: {DateTime.UtcNow.ToString("dd MMMM yyyy")}</p>
                        </div>
                        
                        <p>You may now proceed with your registration:</p>
                    
                        
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
            _logger.LogError(ex, "Error sending payment verification email");
            var ErrorMsg = ex.Message + " Error sending payment verification email";
        }
    }

    private async Task SendPaymentVerifiedEmailNoUpload(Payment payment)
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
                    .important-note {{
                        background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107;
                        margin: 15px 0; border-radius: 4px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Payment Verified</h2>
                    </div>
                    
                    <p>Dear {student.Surname + " " + student.OtherNames},</p>
                    
                    <p>We are pleased to inform you that your payment has been successfully verified by our Finance Office.</p>
                    
                    <div class='details'>
                        <p><strong>Payment Details:</strong></p>
                        <p>Amount: GHS {payment.AmountPaid.ToString("N2")}</p>
                        <p>Payment Reference: {payment.PaymentReference}</p>
                        <p>Bank Reference: {payment.BankReferenceNumber}</p>
                        <p>Accountant General's Receipt: {payment.AccountantReceipt}</p>
                        <p>Date Verified: {payment.VerifiedDate?.ToString("dd MMMM yyyy") ?? DateTime.UtcNow.ToString("dd MMMM yyyy")}</p>
                    </div>
                    
                    <div class='important-note'>
                        <p><strong>Important:</strong> Please ensure you:</p>
                        <ol>
                            <li>Keep a copy of your Accountant General's receipt for your records</li>
                            <li>Upload the receipt to your student portal for documentation</li>
                            <li>Present the original receipt when required for verification</li>
                        </ol>
                    </div>
                    
                    <p>You may now proceed with your registration:</p>
                
                    <div class='footer'>
                        <p>If you have any questions, please contact the Finance Office.</p>
                        <p>© {DateTime.UtcNow.Year} PWCE Portal. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

            await _emailSender.SendGmailEmailAsync(student.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment verification email");
            var ErrorMsg = ex.Message + " Error sending payment verification email";
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
                            
                            <p>Dear {student.OtherNames +" "+student.OtherNames},</p>
                            
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
            _logger.LogError(ex, "Error sending payment rejection email");
            var ErrorMsg = ex.Message + " Error sending payment rejection email";
        }
    }
    
}