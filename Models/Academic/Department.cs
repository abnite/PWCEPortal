using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class Department : EntityHelper
{
    [Required]
    public string DepartmentName { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation
    public ICollection<Lecturer>? Lecturers { get; set; }
}
