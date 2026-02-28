using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models;
using PWCEPortal.ViewModel;
using PWCEPortal.ViewModel.Account;

namespace PWCEPortal.Services;

public class UserCreationService:IUserCreation
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly PortalDbContext _context;
    private readonly IEmailSender _emailService;
    private readonly IConfiguration _configuration;


    public UserCreationService(IConfiguration configuration,IEmailSender emailService,UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, PortalDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }
    
    public async Task<IdentityResult> CreateNewAccount(UserRegistrationViewModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.EmailAddress,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.EmailAddress,
            PhoneNumber = model.PhoneNumber,
            AccountType = model.Role
        };

        var result = await _userManager.CreateAsync(user, "PASSWORD1"); // Use the provided password
        if (result.Succeeded)
        {
            var role = await _roleManager.FindByNameAsync(model.Role);
            if (role != null)
            {
                var checkUserIsInRole = await _userManager.IsInRoleAsync(user, role.Name);
                if (!checkUserIsInRole)
                {
                    await _userManager.AddToRoleAsync(user, role.Name);
                }
            }
        }

        var Url = _configuration["UrlSetting:Url"];
        var loginUrl = Url + "/Account/";
        var defaultPassword = "PASSWORD1";
        var emailMessage =
            $"Dear {model.FirstName},<br/><br/>" +
            $"We are pleased to inform you that a new account has been created for you in the PWCE Portal System. " +
            $"Your default password is <strong>{defaultPassword}</strong>. " +
            $"Please <a href='{loginUrl}'>click here</a> to log in and change your password at your earliest convenience.<br/><br/>" +
            $"If you have any questions or need assistance, please do not hesitate to contact our support team.<br/><br/>" +
            $"Thank you,<br/>" +
            $"PWCE Portal System";
       // await _emailService.SendEmailAsync(model.EmailAddress, "New Account ", emailMessage);
        await _emailService.SendGmailEmailAsync(model.EmailAddress, "New Account ", emailMessage);

        return result;
    }

    public async Task<IdentityResult> EditUser(Guid userId, UserRegistrationViewModel model)
    {
        // Find the user by ID
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        // Update user properties
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.EmailAddress;
        user.UserName = model.EmailAddress;
        user.PhoneNumber = model.PhoneNumber;
        user.AccountType = model.Role;

        // Update the user
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            // Update the user's role if it has changed
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                // Remove existing roles
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                // Add the new role
                await _userManager.AddToRoleAsync(user, model.Role);
            }
        }

        return result;
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<IdentityResult> SendPasswordResetEmailAsync(string email, Func<string, string> callbackUrlGenerator)
    {
        throw new NotImplementedException();
    }

    public async Task<IdentityResult> SendEmailConfirmationAsync(ApplicationUser user, Func<string, string> callbackUrlGenerator)
    {
        throw new NotImplementedException();
    }

    public async Task<IdentityResult> AddUserToRoleAsync(ApplicationUser user, string roleName)
    {
        throw new NotImplementedException();
    }
    
    public async Task<List<UserRegistrationViewModel>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserRegistrationViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();
            if (role?.ToLower() == "student")
                continue;
            var userViewModel = new UserRegistrationViewModel
            {
                Id =Guid.Parse(user.Id),
                EmailAddress = user. UserName,
                FirstName = user. FirstName,
                LastName = user. LastName,
                PhoneNumber = user. PhoneNumber,
                Role = roles. FirstOrDefault() // Assuming one role per user
            };
            userViewModels.Add(userViewModel);
        }

        return userViewModels;
    }
    
  
    
    public async Task<IdentityResult> ResetUserPasswordByAdmin(string userId, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
    
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result;
    }

    public async Task<IdentityResult> ChangeUserPasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result;
    }

    public async Task SendPasswordResetEmail2Async(string email, Func<string, string> callbackUrlGenerator)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = callbackUrlGenerator(token);
        // Send the email
        var emailMessage =
            $"Dear {user.UserName},<br/><br/>We received a request to reset your password. Please <a href='{callbackUrl}'>click here</a> to reset your password. If you did not request a password reset, please ignore this email.<br/><br/>Thank you,<br/>The Stimulus Team";
        await _emailService.SendEmailAsync(email, "Reset Password", emailMessage);
        await _emailService.SendGmailEmailAsync(email, "Reset Password", emailMessage);
    }

    public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result;
    }


    
}