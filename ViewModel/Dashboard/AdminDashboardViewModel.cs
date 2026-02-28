using PWCEPortal.Models.Payment;

namespace PWCEPortal.ViewModel.Dashboard;

public class AdminDashboardViewModel
{
    public int TotalStudents { get; set; }
    public decimal TotalFeesCollected { get; set; }
    public decimal OutstandingFees { get; set; }
    public int PendingVerifications { get; set; }
    public string ActiveAcademicYear { get; set; }
    
    
    // Chart Data
    public List<string> MonthlyPaymentLabels { get; set; } = new List<string>();
    public List<decimal> MonthlyPaymentData { get; set; } = new List<decimal>();
    public List<string> LevelLabels { get; set; } = new List<string>();
    public List<decimal> LevelData { get; set; } = new List<decimal>();
        
    // Recent Activities
    public List<Payment> RecentPayments { get; set; } = new List<Payment>();
}