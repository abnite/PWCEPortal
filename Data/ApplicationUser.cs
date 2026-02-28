using Microsoft.AspNetCore.Identity;

namespace PWCEPortal.Data;

public class ApplicationUser: IdentityUser
{
    public string? FirstName { get; set; } 
    public string? LastName { get; set; }
    public Boolean? IsDeleted { get; set; }
    //public Boolean? IsConfirmed{ get; set; }
    
    public DateTime? LastLoginTime { get; set; }
    
    public string? AccountType { get; set; }

    /// <summary>
    /// For HOD and Unit Head users: the department/unit they oversee.
    /// Used to scope course assignments and non-teaching staff appraisals.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    public ApplicationUser()
    {
        //IsConfirmed = false;
    }
}