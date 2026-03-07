using System.ComponentModel.DataAnnotations;
using PWCEPortal.CommonEntity;

namespace PWCEPortal.ViewModel.Account;

public class UserRegistrationViewModel : EntityHelper
{
 
    
    [Required(ErrorMessage = "Select Role"), Display(Name = "Role")]
    public string Role { get; set; }

    [Required(ErrorMessage = "Enter User email Address"), Display(Name = "Email Address"), EmailAddress]
    public string EmailAddress { get; set; }
    [Required(ErrorMessage = "Enter First Name"), Display(Name = "First Name")]
    public string FirstName { get; set; }
    //[Required, Display(Name = "Last Name")]
    [Required(ErrorMessage = "Enter Last Name"), Display(Name = "Last Name")]
    public string LastName { get; set; }
    [Required(ErrorMessage = "Phone Number is required")]
    [Display(Name = "Phone Number")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// For HOD and Unit Head users: the department/unit they oversee.
    /// Set by System Admin so the user is scoped to that department.
    /// </summary>
    [Display(Name = "Department / Unit")]
    public Guid? DepartmentId { get; set; }
}