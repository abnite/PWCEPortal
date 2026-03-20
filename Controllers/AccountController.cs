using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.StudentInfo;
using PWCEPortal.ViewModel.Account;

namespace PWCEPortal.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IUserCreation _userCreation;
    private readonly PortalDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(IAccountService accountService, PortalDbContext context, IEmailSender emailSender,UserManager<ApplicationUser> userManager,IUserCreation userCreation,SignInManager<ApplicationUser> signInManager)
    {
        _accountService = accountService;
        _context = context;
        _emailSender = emailSender;
        _userManager = userManager;
        _userCreation = userCreation;
        _signInManager = signInManager;
    }

    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpGet]
    public IActionResult ValidateStudent()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> ValidateStudent(string applicationNumber, DateTime dateOfBirth)
    {
        if (applicationNumber == null)
        {
            TempData["ErrorMessage"] = "Application number is required.";
            return RedirectToAction("ValidateStudent");
        }
           
        var isValid = await _accountService.ValidateStudentAccount(applicationNumber, dateOfBirth);
        if (!isValid)
        {
            ModelState.AddModelError("", "Invalid Application Number or Date of Birth.");
            
            TempData["ErrorMessage"] = "Invalid Application Number or Date of Birth.";
            return View("ValidateStudent");
        }
        
        var getStudentEmail = await _context.Students.Where(i => i.ApplicationNumber == applicationNumber).Select(i => i.Email).FirstOrDefaultAsync();
        var checkaccount = await _context.Users.FirstOrDefaultAsync(x => x.Email == getStudentEmail);
        if (checkaccount != null)
        {
            TempData["ErrorMessage"] = "Account already exists. Login again / Reset Password";
            return View("Login");
        }
        TempData["ApplicationNumber"] = applicationNumber;
        TempData["DateOfBirth"] = dateOfBirth;
        return RedirectToAction("CreateAccount");
    }

    [HttpGet]
    public async Task<IActionResult> CheckEmail(string email)
    {
        var student = new Student
        {
            Email = email
        };
        return View(student);
    }
    
    // GET: Create Account (after validation)
    [HttpGet]
    public async Task<IActionResult> CreateAccount()
    {
        // Retrieve the Application Number from TempData
        if (TempData["ApplicationNumber"] == null)
            return RedirectToAction("ValidateStudent", "Account");

        string applicationNumber = TempData["ApplicationNumber"].ToString();
        DateTime dateOfBirth = Convert.ToDateTime(TempData["DateOfBirth"]);

        // Find the student record
        var student = await _context.Students.FirstOrDefaultAsync(s =>
            s.ApplicationNumber == applicationNumber && s.DateOfBirth == dateOfBirth);

        if (student == null)
            return RedirectToAction("ValidateStudent", "Account");

        return View(student);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(Guid studentId, string email)
    {
        var student = await _context.Students.FindAsync(studentId);
        try
        {
            if (student == null)
            {
                ModelState.AddModelError("", "Student not found.");
                TempData["ErrorMessage"] = "Student not found.";
                return View();
            }

            var success = await _accountService.ActivateStudentAccount(student, email);
            if (!success)
            {
                ModelState.AddModelError("", "Account activation failed.");
                TempData["ErrorMessage"] = "Account activation failed.";
                return View(student);
            }

            TempData["SuccessMessage"] = "Account activated.";

            return RedirectToAction("CheckYourEmail");

        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(student);
        }
        catch (Exception e)
        {
            TempData["ErrorMessage"] = "An unexpected error occurred while activating the account.";
            return View(student);
        }
    }


    // GET: Inform the student to check their email.
    [HttpGet]
    public IActionResult CheckYourEmail()
    {
        return View();
    }

    // GET: Confirm Email (activation link)
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
      
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return View();
        }
       
        if (user.EmailConfirmed)
        {
            TempData["SuccessMessage"] = "Email is already confirmed. Please Login";
            return RedirectToAction("Login");
        }
        
        var success = await _accountService.ConfirmEmail(userId, token);
        if (!success)
        {
            // Regenerate token and resend confirmation email
            var newToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(newToken));
            var confirmationLink = $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendGmailEmailAsync(user.Email, "Confirm Your Email", $"Please confirm your email by clicking this link: {confirmationLink}");

            TempData["ErrorMessage"] = "Invalid token. A new confirmation email has been sent.";
            return View();
        }
       
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedPasswordToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
        TempData["SuccessMessage"] = "Email confirmation succeeded.";

        return RedirectToAction("SetPassword",new {userEmail=user.Email, resetToken = encodedPasswordToken});
    }

    // GET: Display form to set a new password.
    [HttpGet]
    public IActionResult SetPassword(string userEmail, string resetToken)
    {
        var model = new SetPasswordViewModel
        {
            Token =resetToken,
            Email =userEmail
        };
        // If token or email is missing, show an error.
        if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Email))
        {
            TempData["ErrorMessage"] = "Email confirmation failed.";
            return View();
        }
        return View(model);
    }

    // POST: Set the new password.
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        var success = await _accountService.ResetPassword(model.Email, model.Token, model.NewPassword);
        if (!success)
        {
            ModelState.AddModelError("", "Password reset failed.");
            TempData["ErrorMessage"] = "Password reset failed.";
            return View();
        }
        TempData["SuccessMessage"] = "Password reset succeeded.";
        return RedirectToAction("Login", "Account");
    }
    
    [HttpPost]
    public async Task<IActionResult> passwordRecovery(string resetEmail)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(resetEmail);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token=encodedToken }, Request.Scheme);
                
                await SendPasswordResetTokenAsync(user.Email, resetLink,user.FirstName);
                TempData["SuccessMessage"] =" A password reset link has been sent to your email address. Please check your inbox.";
                return RedirectToAction("Login");
            }
            else
            {
                TempData["ErrorMessage"] ="Email Address does not exist";
                
            }
        }
        return View("Login");
    }
    
    private async Task SendPasswordResetTokenAsync(string email, string link, string fName)
    {
        try
        {
            string to = email;
            string subject = "Password Reset Request";
            string message = $@"
                <html>
                <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            line-height: 1.6;
                        }}
                        .email-container {{
                            max-width: 600px;
                            margin: auto;
                            padding: 20px;
                            border: 1px solid #e0e0e0;
                            border-radius: 8px;
                            background-color: #f9f9f9;
                        }}
                        .btn {{
                            display: inline-block;
                            padding: 15px 25px;
                            font-size: 16px;
                            color: #ffffff;
                            background-color: #007bff;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                            text-align: center;
                        }}
                        .btn:hover {{
                            background-color: #0056b3;
                        }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <h2>Password Reset Request</h2>
                        <p>Dear {fName},</p>
                        <p>We received a request to reset your password. You can reset your password by clicking the button below:</p>
                        <p>
                            <a href='{link}' class='btn'>Reset Password</a>
                        </p>
                        <p>Please note that this link will expire in 24 hours. If you did not request a password reset, you can safely ignore this email.</p>
                        <p>Best regards,</p>
                        <p><strong>PWCE</strong></p>
                    </div>
                   <div class='footer'>
                        <p>If you did not request this activation, please ignore this email.</p>
                        <p>Best regards,<br>The PWCEPortal Team</p>
                    </div>
                </body>
                </html>";

            await _emailSender.SendGmailEmailAsync(to, subject, message);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while sending the password reset email: " + ex.Message;
        }
    }
    
    
    [HttpGet]
    public IActionResult ResetPassword(string token, string email)
    {
        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
                var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
                if (result.Succeeded)
                {
                    // Password reset successful, redirect to login or confirmation page
                    TempData["SuccessMessage"] =" Password Changed. Please Login";
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorMsg = "";
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        errorMsg = error.Description;
                    }

                    TempData["ErrorMessage"] = "Password Expired or " + errorMsg;
                }
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Please check your inbox and click on the link again";
        }

        return View(model);
    }
    
    //User Password Reset
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);
    
        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!isCurrentPasswordValid)
        {
            TempData["ErrorMessage"] = "The current password is incorrect.";
            return View(model);
        }
        
        var result = await _userCreation.ChangeUserPasswordAsync(userId, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Password changed successfully";
            return RedirectToAction("ChangePassword", "Account");
        }
        else
        {
            TempData["ErrorMessage"] = "Invalid password change attempt. " + string.Join("; ", result.Errors.Select(e => e.Description));

        }

        return View(model);
    }


    
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }
    
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        
        var success = await _accountService.Login(email, password);
        if (!success)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            TempData["ErrorMessage"] = "Invalid login attempt.";
            return View();
        }
        
        
        var user = await _context.Users.Where(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null)
        {
            TempData["ErrorMessage"] = "User details not found.";
            return View();
        }
        var RoleId=await _context.UserRoles.Where(ur => ur.UserId == user.Id).FirstOrDefaultAsync();
        var RoleName=await _context.Roles.Where(ur => ur.Id == RoleId.RoleId).FirstOrDefaultAsync();

    
     
        
        // Check user role and redirect accordingly
        if (RoleName.Name == "Admin")
        {
            return RedirectToAction("Index", "AdminDashboard");
        }
        else if (RoleName.Name == "Finance Officer")
        {
            return RedirectToAction("Index", "FinanceDashboard");
        }
        else if(RoleName.Name == "Registrar" || RoleName.Name == "Student Records Officer" || RoleName.Name == "Student Records Officer" || RoleName.Name == "Secretary")
        {
            return RedirectToAction("Index", "Reports");
        }
        else if (RoleName.Name == "Student")
        {
            var getStudent= await _context.Students.Where(e => e.Email == email && e.IsDeleted==false).FirstOrDefaultAsync();
            if (getStudent == null)
            {
                ModelState.AddModelError("", "Student Account  Not Active or Deleted. ");
                TempData["ErrorMessage"] = "Student Account  Not Active or Deleted.";
                return View();
            }
            return RedirectToAction("Index", "StudentDashboard");
        }

        return RedirectToAction("index","AdminDashboard");
    }
    
 
    [Authorize(Roles = "System Admin")]
    public async Task<IActionResult> AddUser()
    {
        var users = await _userCreation.GetAllUsersAsync();
        var model = new UserRegistrationAndListViewModel
        {
            Users = users, 
            Registration = new UserRegistrationViewModel()
        };
        return View(model);
    }
    
    [Authorize(Roles = "System Admin")]
    [HttpPost]
    public async Task<IActionResult> AddUser(UserRegistrationAndListViewModel model)
    {
        // Require at least one role
        if (model.Registration?.Roles == null || !model.Registration.Roles.Any(r => !string.IsNullOrWhiteSpace(r)))
            ModelState.AddModelError("Registration.Roles", "Please select at least one role.");

        if (ModelState.IsValid)
        {
            var result = await _userCreation.CreateNewAccount(model.Registration);
            if (result.Succeeded)
            {

                TempData["SuccessMessage"] = "User Added and Role Assigned Successfully";
               // return View(UserList);
               return RedirectToAction("UserList");
            }


            foreach (var error in result.Errors)
            {
                 ModelState.AddModelError("", error.Description);
            }
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            TempData["ErrorMessage"] = "Invalid Input. Please correct the following errors:\n" + string.Join("\n", errorMessages);

        }
        else
        {
            
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            TempData["ErrorMessage"] = "Invalid Input. Please correct the following errors:\n" + string.Join("\n", errorMessages);
      
            //return View();
            
        }
        var users = await _userCreation.GetAllUsersAsync();
        var viewModel = new UserRegistrationAndListViewModel
        {
            Users = users,
            Registration = model.Registration
        };
        
        return RedirectToAction("UserList");
        //return View(viewModel);
    }
    
    [HttpGet]
    [Authorize(Roles = "System Admin")]
    public async Task<IActionResult> UserList()
    {
        var users = await _userCreation.GetAllUsersAsync();
        var model = new UserRegistrationAndListViewModel
        {
            Users = users,
            Registration = new UserRegistrationViewModel()
        };
        return View("AddUser",model);
    }
    
    [Authorize(Roles = "System Admin")]
    [HttpGet]
    public async Task<IActionResult> EditUser(Guid userId)
    {
        // Find the user by ID
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("UserList");
        }

        // Get all current roles
        var userRoles = await _userManager.GetRolesAsync(user);

        // All available roles for the checkboxes
        ViewBag.AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

        // Populate departments for HOD/Unit Head dropdown
        ViewBag.Departments = await _context.Departments
            .Where(d => d.IsDeleted != true)
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();

        // Populate the view model
        var model = new UserRegistrationViewModel
        {
            Id = Guid.Parse(user.Id),
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailAddress = user.Email,
            PhoneNumber = user.PhoneNumber,
            Roles = userRoles.ToList(),
            DepartmentId = user.DepartmentId
        };

        return View(model);
    }
    
    [Authorize(Roles = "System Admin")]
    [HttpPost]
    public async Task<IActionResult> EditUser(UserRegistrationViewModel model)
    {
        // At least one role must be selected
        if (model.Roles == null || !model.Roles.Any(r => !string.IsNullOrWhiteSpace(r)))
            ModelState.AddModelError("Roles", "Please select at least one role.");

        if (ModelState.IsValid)
        {
            var result = await _userCreation.EditUser(model.Id, model);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction("UserList");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        // Reload ViewBag data for re-render
        ViewBag.AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        ViewBag.Departments = await _context.Departments
            .Where(d => d.IsDeleted != true).OrderBy(d => d.DepartmentName).ToListAsync();
        return View(model);
    }

}