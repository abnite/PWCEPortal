using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Models.Staff;

/// <summary>
/// Records for non-teaching staff members (admin, support, etc.)
/// </summary>
public class NonTeachingStaff : EntityHelper
{
    [Required]
    public string StaffId { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Position { get; set; } = string.Empty;

    public string? Department { get; set; }

    /// <summary>FK to the Department model for scoping by Unit Head.</summary>
    [ForeignKey("DepartmentEntity")]
    public Guid? DepartmentId { get; set; }
    public PWCEPortal.Models.Academic.Department? DepartmentEntity { get; set; }

    public string? Email { get; set; }

    public string? PhoneNo { get; set; }

    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public DateTime? DateEmployed { get; set; }

    public string? Qualification { get; set; }

    public bool IsActive { get; set; } = true;

    public string? UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    // Navigation
    public ICollection<NonTeachingAppraisal>? Appraisals { get; set; }
}
