using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Payment;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Services;

public class FeeService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly PortalDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    IHttpContextAccessor _httpContextAccessor;

    public FeeService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, PortalDbContext context, IEmailSender emailSender,IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }
    private async Task CreateFeeAssignmentForStudent(Student student)
    {
        // Retrieve active academic year
        var activeAcademicYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeAcademicYear == null)
        {
            // No active academic year means nothing to assign.
            return;
        }

        // Look up the FeeStructure for the student's current level and active academic year.
        var feeStructure = await _context.FeeStructures
            .FirstOrDefaultAsync(f => f.Level == student.CurrentLevel && f.AcademicYearId == activeAcademicYear.Id);
    
        if (feeStructure == null)
        {
            // If no fee structure exists, skip or handle accordingly.
            return;
        }
        
        var previousFeeAssignment = await _context.StudentFeeAssignments
            .Where(fa => fa.StudentId == student.Id && fa.AcademicYearId != activeAcademicYear.Id)
            .OrderByDescending(fa => fa.DateAdded)
            .FirstOrDefaultAsync();

        // Create a new fee assignment. If the student has previous arrears, you could retrieve them 
        // (for example, from a previous StudentFeeAssignment) and add them.
        decimal arrears = previousFeeAssignment.OutstandingFee; // Assuming OutstandingFees holds arrears from last year
        var fullFee = feeStructure.FullFee;
        var totalDue = fullFee + arrears;

        var feeAssignment = new StudentFeeAssignment
        {
            StudentId = student.Id,
            AcademicYearId = activeAcademicYear.Id,
            FullFee = fullFee,
            PaymentMade = 0,  // Initially zero payments made
            OutstandingFee = totalDue,  // Total due is the new fee plus carried arrears
            PaymentStatus = "Not Paid"
        };

        _context.StudentFeeAssignments.Add(feeAssignment);
        await _context.SaveChangesAsync();
    }

}