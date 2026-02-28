using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.Account;

public class SetPasswordViewModel
{
    [Required]
    public string Email { get; set; }

    // This token is now passed as a hidden field, not entered by the user.
    [Required]
    public string Token { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name="New Password")]
    public string NewPassword { get; set; }
    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
}