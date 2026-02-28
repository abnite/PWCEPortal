using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Models.Payment;

public class PartPaymentConfig:EntityHelper
{
    [Required]
    [ForeignKey("AcademicYear")]
    public Guid AcademicYearId { get; set; } 
    public AcademicYear AcademicYear { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "Percentage must be between 1 and 100.")]
    public int PartPaymentPercentage { get; set; } // Example: 50% means students need to pay half

   
}