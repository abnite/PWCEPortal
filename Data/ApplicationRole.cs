using Microsoft.AspNetCore.Identity;

namespace PWCEPortal.Data;

public class ApplicationRole: IdentityRole
{
    public string Description { get; set; }
}