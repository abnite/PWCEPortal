using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.Student;

public class FinancialInfoViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "SSNIT Number")]
    public string SSNITNumber { get; set; }

    [Display(Name = "EZwich Account Name")]
    public string EZwichAccountName { get; set; }

    [Display(Name = "EZwich Account Number")]
    public string EZwichAccountNumber { get; set; }

    // Foreign Key to Student
    public Guid StudentId { get; set; }
}