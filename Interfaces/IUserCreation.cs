using Microsoft.AspNetCore.Identity;
using PWCEPortal.Data;
using PWCEPortal.ViewModel;
using PWCEPortal.ViewModel.Account;

namespace PWCEPortal.Interfaces;

public interface IUserCreation
{
    Task<IdentityResult> CreateNewAccount(UserRegistrationViewModel model);
    Task<IdentityResult> EditUser(Guid userId, UserRegistrationViewModel model);
    Task<IdentityResult> DeleteUserAsync(string userId);
    Task<IdentityResult> SendPasswordResetEmailAsync(string email,Func<string, string> callbackUrlGenerator);
    Task<IdentityResult> SendEmailConfirmationAsync(ApplicationUser user,Func<string, string> callbackUrlGenerator);
    Task<IdentityResult> AddUserToRoleAsync(ApplicationUser user, string roleName);
     Task<List<UserRegistrationViewModel>> GetAllUsersAsync();
    // Task<List<PrepaidViewModel>> GetAllPrepaidCode();
   // Task<bool> CreatePrepaidCodes(PrepaidViewModel model);
    Task<IdentityResult> ResetUserPasswordByAdmin(string userId, string newPassword);
    Task<IdentityResult> ChangeUserPasswordAsync(string userId, string currentPassword, string newPassword);
    Task SendPasswordResetEmail2Async(string email, Func<string, string> callbackUrlGenerator);
    Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword);




}