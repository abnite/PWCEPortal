using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.StudentInfo;

public class ParentGuardian:EntityHelper
{
    [Required]
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    [Required]
    public string FullName { get; set; }
    public string? Email { get; set; }
    public string Telephone { get; set; }
    public string? Occupation { get; set; }
    public string? ContactAddress { get; set; }
    public string Relationship { get; set; } // Father, Mother, Uncle, etc.

}