using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Payment;
using PWCEPortal.ViewModel.Reports;

namespace PWCEPortal.Services;

public class ReportService:IReportService
{
    private readonly PortalDbContext _context;
    public ReportService(PortalDbContext context)
    {
        _context = context;
    }
    public async Task<byte[]> DownloadFeesPaymentReport(DateTime? startDate, DateTime? endDate, string searchTerm)
    {
        var query = _context.Payments.Include(i=>i.VerifiedBy).Include(p => p.Student).Where(i=>i.IsDeleted==false).AsQueryable();
            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => p.PaymentReference.Contains(searchTerm) || p.Student.Surname.Contains(searchTerm) ||p.Student.OtherNames.Contains(searchTerm)||p.BankReferenceNumber.Contains(searchTerm));
            query = query.OrderByDescending(p => p.PaymentDate);
            
            var data = await query.ToListAsync();
            return GenerateExcel(data);
    }

    public async Task<byte[]> DownloadRevenueReport(DateTime? startDate, DateTime? endDate, string grouping)
    {
         grouping = grouping?.ToLower() ?? "daily";
            var query = _context.Payments.Where(p => p.IsVerified).Include(p => p.Student).AsQueryable();
            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);
            
            IQueryable<RevenueReportViewModel> detailQuery;
            if (grouping == "daily")
            {
                detailQuery = query.Select(p => new RevenueReportViewModel {
                    Period = p.PaymentDate.ToString("dd MMM yyyy"),
                    PaymentDate = p.PaymentDate,
                    StudentName = p.Student.Surname + " " + p.Student.OtherNames,
                    ApplicationNumber = p.Student.ApplicationNumber,
                    AmountPaid = p.AmountPaid,
                    PaymentReference = p.PaymentReference,
                    BankReference = p.BankReferenceNumber
                });
            }
            else if (grouping == "weekly")
            {
                detailQuery = query.Select(p => new RevenueReportViewModel {
                    Period = p.PaymentDate.Year + " - Week " + (EF.Functions.DateDiffWeek(new DateTime(p.PaymentDate.Year, 1, 1), p.PaymentDate) + 1).ToString(),
                    PaymentDate = p.PaymentDate,
                    StudentName = p.Student.Surname + " " + p.Student.OtherNames,
                    ApplicationNumber = p.Student.ApplicationNumber,
                    AmountPaid = p.AmountPaid,
                    PaymentReference = p.PaymentReference,
                    BankReference = p.BankReferenceNumber
                });
            }
            else if (grouping == "monthly")
            {
                detailQuery = query.Select(p => new RevenueReportViewModel {
                    Period = p.PaymentDate.Year + "-" + p.PaymentDate.Month.ToString("D2"),
                    PaymentDate = p.PaymentDate,
                    StudentName = p.Student.Surname + " " + p.Student.OtherNames,
                    ApplicationNumber = p.Student.ApplicationNumber,
                    AmountPaid = p.AmountPaid,
                    PaymentReference = p.PaymentReference,
                    BankReference = p.BankReferenceNumber
                });
            }
            else if (grouping == "quarterly")
            {
                detailQuery = query.Select(p => new RevenueReportViewModel {
                    Period = p.PaymentDate.Year + " Q" + (((p.PaymentDate.Month - 1) / 3) + 1).ToString(),
                    PaymentDate = p.PaymentDate,
                    StudentName = p.Student.Surname + " " + p.Student.OtherNames,
                    ApplicationNumber = p.Student.ApplicationNumber,
                    AmountPaid = p.AmountPaid,
                    PaymentReference = p.PaymentReference,
                    BankReference = p.BankReferenceNumber
                });
            }
            else
            {
                detailQuery = query.Select(p => new RevenueReportViewModel {
                    Period = p.PaymentDate.ToString("dd MMM yyyy"),
                    PaymentDate = p.PaymentDate,
                    StudentName = p.Student.Surname + " " + p.Student.OtherNames,
                    ApplicationNumber = p.Student.ApplicationNumber,
                    AmountPaid = p.AmountPaid,
                    PaymentReference = p.PaymentReference,
                    BankReference = p.BankReferenceNumber
                });
            }
            
            var data = await detailQuery.OrderBy(x => x.PaymentDate).ToListAsync();
            return GenerateExcel(data);
    }

    public async Task<byte[]> DownloadOutstandingBalancesReport()
    {
        var data = await _context.StudentFeeAssignments
            .Include(sfa => sfa.Student)
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.OutstandingFee > 0)
            .Select(sfa => new OutstandingReportViewModel
            {
                StudentName = sfa.Student.Surname + " " + sfa.Student.OtherNames,
                ApplicationNumber = sfa.Student.ApplicationNumber,
                AcademicYear = sfa.AcademicYear.Year,
                OutstandingAmount = sfa.OutstandingFee
            })
            .OrderByDescending(r => r.OutstandingAmount)
            .ToListAsync();
        return GenerateExcel(data);
    }

    public async Task<byte[]> DownloadOverpaymentsReport()
    {
        var data = await _context.StudentFeeAssignments
            .Include(sfa => sfa.Student)
            .Include(sfa => sfa.AcademicYear)
            .Where(sfa => sfa.OutstandingFee < 0)
            .Select(sfa => new OverpaymentReportViewModel
            {
                StudentName = sfa.Student.Surname + " " + sfa.Student.OtherNames,
                ApplicationNumber = sfa.Student.ApplicationNumber,
                AcademicYear = sfa.AcademicYear.Year,
                OverpaymentAmount = Math.Abs(sfa.OutstandingFee)
            })
            .OrderByDescending(r => r.OverpaymentAmount)
            .ToListAsync();
        return GenerateExcel(data);
    }

    public async Task<byte[]> DownloadPaymentSummaryReport(DateTime? startDate, DateTime? endDate, string searchTerm)
    {
        var query = _context.Payments.Include(p => p.Student).AsQueryable();
        if (startDate.HasValue)
            query = query.Where(p => p.PaymentDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(p => p.PaymentDate <= endDate.Value);
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(p => p.PaymentReference.Contains(searchTerm) || p.Student.Surname.Contains(searchTerm));
        query = query.OrderByDescending(p => p.PaymentDate);
        var data = await query.ToListAsync();
        return GenerateExcel(data);
    }
    
    // Generic helper method to generate Excel file using EPPlus
    private byte[] GenerateExcel<T>(List<T> data)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Data");

            if (typeof(T) == typeof(Payment))
            {
                worksheet.Cells[1, 1].Value = "Payment Date";
                worksheet.Cells[1, 2].Value = "Student Name";
                worksheet.Cells[1, 3].Value = "Application Number";
                worksheet.Cells[1, 4].Value = "Amount Paid";
                worksheet.Cells[1, 5].Value = "Payment Reference";
                worksheet.Cells[1, 6].Value = "Bank Reference";
                worksheet.Cells[1, 7].Value = "Status";
                worksheet.Cells[1, 8].Value = "Verified By";
                worksheet.Cells[1, 9].Value = "Date Verified";

                var payments = data as List<Payment>;
                for (int i = 0; i < payments.Count; i++)
                {
                    var payment = payments[i];
                    worksheet.Cells[i + 2, 1].Value = payment.PaymentDate.ToString("dd MMM yyyy");
                    worksheet.Cells[i + 2, 2].Value = $"{payment.Student?.Surname} {payment.Student?.OtherNames}";
                    worksheet.Cells[i + 2, 3].Value = payment.Student?.ApplicationNumber;
                    worksheet.Cells[i + 2, 4].Value = payment.AmountPaid;
                    worksheet.Cells[i + 2, 5].Value = payment.PaymentReference;
                    worksheet.Cells[i + 2, 6].Value = payment.BankReferenceNumber;
                    worksheet.Cells[i + 2, 7].Value = payment.IsVerified ? "Verified" : "Pending";
                    worksheet.Cells[i + 2, 8].Value = payment.IsVerified ? payment.VerifiedBy.FirstName +" " +payment.VerifiedBy.LastName  : "NA";
                    worksheet.Cells[i + 2, 9].Value = payment.IsVerified ? payment.VerifiedDate?.ToString("dd MMMM yyyy"): "NA";
                }
            }
            else if (typeof(T) == typeof(RevenueReportViewModel))
            {
                worksheet.Cells[1, 1].Value = "Period";
                worksheet.Cells[1, 2].Value = "Payment Date";
                worksheet.Cells[1, 3].Value = "Student Name";
                worksheet.Cells[1, 4].Value = "Application Number";
                worksheet.Cells[1, 5].Value = "Amount Paid";
                worksheet.Cells[1, 6].Value = "Payment Reference";
                worksheet.Cells[1, 7].Value = "Bank Reference";

                var details = data as List<RevenueReportViewModel>;
                for (int i = 0; i < details.Count; i++)
                {
                    var item = details[i];
                    worksheet.Cells[i + 2, 1].Value = item.Period;
                    worksheet.Cells[i + 2, 2].Value = item.PaymentDate.ToString("dd MMM yyyy HH:mm");
                    worksheet.Cells[i + 2, 3].Value = item.StudentName;
                    worksheet.Cells[i + 2, 4].Value = item.ApplicationNumber;
                    worksheet.Cells[i + 2, 5].Value = item.AmountPaid;
                    worksheet.Cells[i + 2, 6].Value = item.PaymentReference;
                    worksheet.Cells[i + 2, 7].Value = item.BankReference;
                }
            }
            else if (typeof(T) == typeof(OutstandingReportViewModel))
            {
                worksheet.Cells[1, 1].Value = "Student Name";
                worksheet.Cells[1, 2].Value = "Application Number";
                worksheet.Cells[1, 3].Value = "Academic Year";
                worksheet.Cells[1, 4].Value = "Outstanding Amount (GHS)";

                var outstanding = data as List<OutstandingReportViewModel>;
                for (int i = 0; i < outstanding.Count; i++)
                {
                    var item = outstanding[i];
                    worksheet.Cells[i + 2, 1].Value = item.StudentName;
                    worksheet.Cells[i + 2, 2].Value = item.ApplicationNumber;
                    worksheet.Cells[i + 2, 3].Value = item.AcademicYear;
                    worksheet.Cells[i + 2, 4].Value = item.OutstandingAmount;
                }
            }
            else if (typeof(T) == typeof(OverpaymentReportViewModel))
            {
                worksheet.Cells[1, 1].Value = "Student Name";
                worksheet.Cells[1, 2].Value = "Application Number";
                worksheet.Cells[1, 3].Value = "Academic Year";
                worksheet.Cells[1, 4].Value = "Overpayment Amount (GHS)";

                var overpayments = data as List<OverpaymentReportViewModel>;
                for (int i = 0; i < overpayments.Count; i++)
                {
                    var item = overpayments[i];
                    worksheet.Cells[i + 2, 1].Value = item.StudentName;
                    worksheet.Cells[i + 2, 2].Value = item.ApplicationNumber;
                    worksheet.Cells[i + 2, 3].Value = item.AcademicYear;
                    worksheet.Cells[i + 2, 4].Value = item.OverpaymentAmount;
                }
            }
            else if (typeof(T) == typeof(Payment))
            {
                // This branch is already handled above.
            }
            else
            {
                throw new NotSupportedException("Export for this type is not supported.");
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }
        
        
    }
    
    
    //Legacy Outstanding Fees Report
    public async Task<byte[]> DownloadLegacyOutstandingFeesReport(string? searchTerm, string? academicYear, DateTime? startDate, DateTime? endDate)
    {
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

        var data = await query
            .OrderBy(l => l.Student.Surname)
            .ThenBy(l => l.Student.OtherNames)
            .ThenBy(l => l.AcademicYear)
            .ToListAsync();

        return GenerateLegacyOutstandingExcel(data);
    }

    private byte[] GenerateLegacyOutstandingExcel(List<LegacyOutstandingFee> data)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage())
        {
            var ws = package.Workbook.Worksheets.Add("Legacy Outstanding");

            // Headers
            ws.Cells[1, 1].Value = "Student Name";
            ws.Cells[1, 2].Value = "Application Number";
            ws.Cells[1, 3].Value = "Academic Year";
            ws.Cells[1, 4].Value = "Outstanding Amount";
            ws.Cells[1, 5].Value = "Description";
            ws.Cells[1, 6].Value = "Date Added";

            ws.Cells[1, 1, 1, 6].Style.Font.Bold = true;

            // Data
            int row = 2;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = $"{item.Student.Surname} {item.Student.OtherNames}";
                ws.Cells[row, 2].Value = item.Student.ApplicationNumber;
                ws.Cells[row, 3].Value = item.AcademicYear;
                ws.Cells[row, 4].Value = item.Amount;
                ws.Cells[row, 5].Value = item.Description;
                ws.Cells[row, 6].Value = item.DateAdded?.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
    
    
}