using PWCEPortal.Models.Academic;

namespace PWCEPortal.ViewModel.Student;

public class HallAndListViewModel
{
    public CollegeHall collegeHall { get; set; }
    public List<CollegeHall> Halls { get; set; }=new List<CollegeHall>();
}