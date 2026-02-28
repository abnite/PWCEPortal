using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class AcademicYear:EntityHelper
{
    [Required]
    public string Year { get; set; } // "2024/2025"
    public bool IsActive { get; set; } // Only one academic year can be active
}