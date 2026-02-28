namespace PWCEPortal.Interfaces;

public interface IReportService
{
    Task<byte[]> DownloadFeesPaymentReport(DateTime? startDate, DateTime? endDate, string searchTerm);
    Task<byte[]> DownloadRevenueReport(DateTime? startDate, DateTime? endDate, string grouping);
    Task<byte[]> DownloadOutstandingBalancesReport();
    Task<byte[]> DownloadOverpaymentsReport();
    Task<byte[]> DownloadPaymentSummaryReport(DateTime? startDate, DateTime? endDate, string searchTerm);
    Task<byte[]> DownloadLegacyOutstandingFeesReport(string? searchTerm, string? academicYear, DateTime? startDate, DateTime? endDate);

}