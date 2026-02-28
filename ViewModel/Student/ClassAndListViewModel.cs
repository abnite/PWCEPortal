using PWCEPortal.Models.Academic;

namespace PWCEPortal.ViewModel.Student;

public class ClassAndListViewModel
{
    public CollegeClass CollegeClass { get; set; }
    public List<CollegeClass> Classes { get; set; }=new List<CollegeClass>();
}