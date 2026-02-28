using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.StudentInfo;

public class FinancialInfo:EntityHelper
{
    [Required]
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student Student { get; set; }

    public string? SSNITNumber { get; set; }
    public string? EZwichAccountName { get; set; }
    public string? EZwichAccountNumber { get; set; }

    
}