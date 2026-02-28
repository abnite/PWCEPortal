using System.Collections.Generic;
using PWCEPortal.Models.Payment;

namespace PWCEPortal.ViewModel.Dashboard
{
    public class FinanceDashboardViewModel
    {
        public int PendingPaymentsCount { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public List<Payment> PendingPayments { get; set; } = new List<Payment>();
        public string ActiveAcademicYear { get; set; }
        
        // Monthly fee collection data
        public List<string> MonthlyLabels { get; set; } = new List<string>();
        public List<decimal> MonthlyData { get; set; } = new List<decimal>();
        
        // Recent payments
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
        
        // Level-wise outstanding fees
        public Dictionary<int, decimal> OutstandingByLevel { get; set; } = new Dictionary<int, decimal>();
        public int StudentsWithArrearsCount { get; set; }
    }
}