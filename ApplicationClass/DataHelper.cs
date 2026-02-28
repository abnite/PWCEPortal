using System.Text;
using Microsoft.AspNetCore.Identity;
using PWCEPortal.Data;

namespace PWCEPortal.ApplicationClass;

public class DataHelper
{
    public UserManager<ApplicationUser> userManager;
    public SignInManager<ApplicationUser> SignInManager;
    public readonly RoleManager<ApplicationRole> RoleManager;
    public readonly PortalDbContext _context;
    private IHttpContextAccessor accessor;
    
    public DataHelper(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> SignInManager,
        RoleManager<ApplicationRole> RoleManager, PortalDbContext context, IHttpContextAccessor accessor)
    {
        this.SignInManager = SignInManager;
        this.RoleManager = RoleManager;
        this.userManager = userManager;
        this._context = context;
        this.accessor = accessor;

    }
    public ApplicationUser GetLoggedInuser()
    {
        var User = accessor.HttpContext.User.Identity.Name;
        return userManager.FindByNameAsync(User).Result;
        // return null;

    }
    public static string CapitalizeWords(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (value.Length == 0)
            return value;
        StringBuilder result = new StringBuilder(value);
        result[0] = char.ToUpper(result[0]);
        for (int i = 1; i < result.Length; ++i)
        {
            if (char.IsWhiteSpace(result[i - 1]))
                result[i] = char.ToUpper(result[i]);
        }

        return result.ToString();
    }
    
}