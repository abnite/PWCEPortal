using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Models.Payment;

public class FeeStructure:EntityHelper
{
    [Required]
    [ForeignKey("AcademicYear")]
    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; }

    [Required]
    [Range(100, 400, ErrorMessage = "Level must be between 100 and 400.")]
    public int Level { get; set; } // 100, 200, 300, 400
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "FullFee must be a positive number.")]
    public decimal FullFee { get; set; } // Example: GHS 3,000

   
}