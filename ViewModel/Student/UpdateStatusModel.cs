namespace PWCEPortal.ViewModel.Student;

public class UpdateStatusModel
{
    public Guid studentId { get; set; }
    public string status { get; set; }
    public string transferInstitution { get; set; }
    public string reason { get; set; }
    public DateTime statusDate { get; set; }
}