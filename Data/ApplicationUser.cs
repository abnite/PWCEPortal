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
    
    public ApplicationUser()
    {
        //IsConfirmed = false;
    }
}