using PWCEPortal.Models.Payment;

namespace PWCEPortal.ViewModel.Dashboard;

public class PendingPaymentsViewModel
{
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public int TotalCount { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string SearchQuery { get; set; }
    
}