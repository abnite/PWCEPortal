namespace PWCEPortal.ViewModel.Reports;

public class PaginationViewModel
{
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
   // public int PageSize { get; set; }
}