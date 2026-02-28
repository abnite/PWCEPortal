using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.Models.Payment;

public class RequiredFee:EntityHelper
{
    [Required]
    public decimal RequiredAmount { get; set; } 
    public bool IsActive { get; set; } 
}