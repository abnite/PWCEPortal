using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.Account;

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Enter Password"), DataType(DataType.Password)]
        public string? Password { get; set; }
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
