using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.ViewModel.Account;

public class UserRegistrationViewModel : EntityHelper
{
    /// <summary>
    /// Selected roles for the user. Replaces the old single-Role field.
    /// Bound from checkbox inputs with name="Roles".
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Read-only display string for the user list table (e.g. "Lecturer, Head of Department").
    /// Populated by GetAllUsersAsync; not used for form binding.
    /// </summary>
    public string? Role => Roles.Any() ? string.Join(", ", Roles) : null;

    [Required(ErrorMessage = "Enter User email Address"), Display(Name = "Email Address"), EmailAddress]
    public string EmailAddress { get; set; }

    [Required(ErrorMessage = "Enter First Name"), Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Enter Last Name"), Display(Name = "Last Name")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Phone Number is required")]
    [Display(Name = "Phone Number")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// For HOD and Unit Head users: the department/unit they oversee.
    /// </summary>
    [Display(Name = "Department / Unit")]
    public Guid? DepartmentId { get; set; }
}
