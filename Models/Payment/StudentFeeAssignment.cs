using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Payment;

public class StudentFeeAssignment:EntityHelper
{
    [Required]
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
        
    [Required]
    [ForeignKey("AcademicYear")]
    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; }
        
    [Required]
    public decimal FullFee { get; set; }  // The fee assigned for the academic year
        
    [Required]
    public decimal PaymentMade { get; set; }  // Total payments made during the academic year
        
    [Required]
    public decimal OutstandingFee { get; set; }  // FullFee - PaymentMade
        
    // Optionally, you can add a status or notes field:
    public string PaymentStatus { get; set; }  // e.g., "Paid", "Partially Paid", "Pending"
        
    // Navigation properties
}