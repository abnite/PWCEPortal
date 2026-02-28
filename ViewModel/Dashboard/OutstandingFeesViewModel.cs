using PWCEPortal.Models.Payment;

namespace PWCEPortal.ViewModel.Dashboard;

public class OutstandingFeesViewModel
{
    public List<StudentFeeAssignment> OutstandingFees { get; set; } = new List<StudentFeeAssignment>();
    public int TotalCount { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string SearchQuery { get; set; }
    public int? Level { get; set; }
}