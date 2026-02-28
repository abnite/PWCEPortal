using PWCEPortal.Models.StudentInfo;
using PWCEPortal.Models;

namespace PWCEPortal.ViewModel.Student;

public class EditProfileViewModel
{
    
    public Models.StudentInfo.Student Student { get; set; }
    public List<ParentGuardian> ParentsGuardians { get; set; }
    public List<FinancialInfo> FinancialInfos { get; set; }
    public List<EducationHistory> EducationHistories { get; set; }
}