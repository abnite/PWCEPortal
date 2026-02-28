using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class CollegeClass:EntityHelper
{
    [ForeignKey("collegeProgram")]
    [Required]
    public Guid collegeProgramId { get; set; }
    public CollegeProgram collegeProgram { get; set; }
    public string ClassName { get; set; }
}