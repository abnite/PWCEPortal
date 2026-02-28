namespace PWCEPortal.ViewModel.Reports;

public class RevenueReportViewModel
{
    public string Period { get; set; }
    public DateTime PaymentDate { get; set; }
    public string StudentName { get; set; }
    public string ApplicationNumber { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentReference { get; set; }
    public string BankReference { get; set; }
}