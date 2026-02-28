using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class CollegeProgram:EntityHelper
{
    [Required]
    public string ProgramName { get; set; }
}