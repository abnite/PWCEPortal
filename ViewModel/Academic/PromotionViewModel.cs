namespace PWCEPortal.ViewModel.Academic;

public class PromotionViewModel
{
    
    public List<Models.StudentInfo.Student> Students { get; set; }
    public string SearchQuery { get; set; }
    public int? CurrentLevel { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}