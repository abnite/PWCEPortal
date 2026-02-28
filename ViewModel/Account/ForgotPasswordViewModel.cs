using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.Account;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}