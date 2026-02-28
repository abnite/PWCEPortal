using System.Net;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Services;

public class AccountService:IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly PortalDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    IHttpContextAccessor _httpContextAccessor;

    public AccountService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, PortalDbContext context, IEmailSender emailSender,IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<bool> ValidateStudentAccount(string applicationNumber, DateTime dateOfBirth)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => (s.ApplicationNumber == applicationNumber || s.StudentID==applicationNumber) && s.DateOfBirth == dateOfBirth);

        return student != null;
    }

    public async Task<bool> ActivateStudentAccount(Student student, string email)
    {
        
        var existingStudent = await _context.Students.FindAsync(student.Id);
        if (existingStudent == null) 
            throw new InvalidOperationException("Student not found.");

        if (!string.IsNullOrEmpty(existingStudent.Email) && existingStudent.Email != email) 
            throw new InvalidOperationException("The email provided does not match the email on record for this student account. Please contact the IT support team for assistance.");

        // Assign Email if missing
        existingStudent.Email = email;
        await _context.SaveChangesAsync();
        
        // Create Identity User
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = existingStudent.Surname,
            LastName = existingStudent.Surname,
            AccountType = "Student"
        };
        

        var result = await _userManager.CreateAsync(user, "Temp@1234");
        if (!result.Succeeded) return false;
        
        IdentityResult addRole = _userManager.AddToRoleAsync(user, "Student").Result;

        existingStudent.User = user; //Guid.Parse(user.Id);
        await _context.SaveChangesAsync();

        // Generate Email Confirmation Link
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        
        // Dynamically generate the confirmation link
        var request = _httpContextAccessor.HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationLink = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

        //var confirmationLink = $"https://yourwebsite.com/AccountActivation/ConfirmEmail?userId={user.Id}&token={token}";
        
        //email
        var emailSubject = "Activate Your Student Account";
        var emailBody = $@"
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
            .button {{ 
                background-color: #4CAF50; 
                color: white; 
                padding: 10px 20px; 
                text-align: center; 
                text-decoration: none; 
                display: inline-block; 
                font-size: 16px; 
                margin: 20px 0; 
                cursor: pointer; 
                border-radius: 5px; 
            }}
            .footer {{ font-size: 12px; color: #777; }}
        </style>
    </head>
    <body>
        <h2>Welcome to PWCEPortal!</h2>
        <p>Thank you for registering as a student. </p>
        <p>Your Default Password is <strong>Temp@1234</strong>. Make Sure you change your password</p>

        <p>To activate your account, please click the button below:</p>
        <a href='{confirmationLink}' class='button'>Activate Account</a>
        <p>If the button above does not work, copy and paste the following link into your browser:</p>
        <p><a href='{confirmationLink}'>{confirmationLink}</a></p>
        <div class='footer'>
            <p>If you did not request this activation, please ignore this email.</p>
            <p>Best regards,<br>The PWCEPortal Team</p>
        </div>
    </body>
    </html>";

        await _emailSender.SendGmailEmailAsync(user.Email, emailSubject, emailBody);

        return true;
    }

    public async Task<bool> ConfirmEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        Console.WriteLine($"Token received: {token}"); // Log the token for debugging
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error: {error.Description}"); // Log errors for debugging
            }
        }

        return result.Succeeded;
    }

    public async Task<bool> Login(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
        return result.Succeeded;
    }

    public async Task<bool> ResetPassword(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;
        
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(newPassword));

        var result = await _userManager.ResetPasswordAsync(user, token, decodedToken);
        return result.Succeeded;
    }
}