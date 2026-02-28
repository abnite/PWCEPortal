using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Payment;

public class LegacyOutstandingFee:EntityHelper
{
    [ForeignKey("StudentId")]
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = default!;

    [MaxLength(20)]
    public string? AcademicYear { get; set; } // e.g. "2022/2023"

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}