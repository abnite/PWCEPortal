using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Academic;

public class CollegeHall : EntityHelper
{
    [Required]
    public string HallName { get; set; }
}