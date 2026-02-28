using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.ApplicationClass;

public class seed
{
    private PortalDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public seed(PortalDbContext context, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public void InsertDB()
    {
        SeedRoles();
        SeedDefaultUser();
        SeedRolePermissions();
        SeedPrograms();
        SeedStudentSample();
    }

    // ── Roles ────────────────────────────────────────────────────────────────

    private void SeedRoles()
    {
        var rolesWithDescriptions = new Dictionary<string, string>
        {
            // Phase 1 roles
            { RoleNames.SystemAdmin,           "Full system administration — unrestricted access to all modules." },
            { RoleNames.Student,               "Student self-service portal access." },
            { RoleNames.Administrator,         "General administration role." },
            { RoleNames.FinanceOfficer,        "Payment verification and financial reporting." },
            { RoleNames.Secretary,             "Administrative secretarial duties." },
            { RoleNames.Registrar,             "Responsible for editing and updating student details." },
            { RoleNames.StudentRecordsOfficer, "Responsible for editing and updating student records." },

            // Phase 2 roles
            { RoleNames.Lecturer,               "Enter and manage marks for assigned courses; submit assessments for review." },
            { RoleNames.HOD,                    "Assign lecturers to courses; review and approve or reject assessment submissions; conduct departmental appraisals." },
            { RoleNames.QAOfficer,              "Review HOD-approved assessments for policy compliance; flag assessments for revision." },
            { RoleNames.AcademicAffairsOfficer, "Configure assessment structures and grading scales; validate and publish approved results." },
            { RoleNames.Principal,              "Provide final institutional approval on assessments; authorise post-approval corrections; access executive dashboards." },
            { RoleNames.VicePrincipal,          "Provide final institutional approval on assessments; access executive dashboards." },
            { RoleNames.HROfficer,              "Manage non-teaching staff records; conduct and record annual appraisals." },
        };

        foreach (var role in rolesWithDescriptions)
        {
            if (!_context.Roles.Any(r => r.Name == role.Key))
            {
                var roleStore = new RoleStore<IdentityRole>(_context);
                var applicationRole = new ApplicationRole
                {
                    Name = role.Key,
                    NormalizedName = role.Key.ToUpper(),
                    Description = role.Value,
                };
                roleStore.CreateAsync(applicationRole).GetAwaiter().GetResult();
            }
        }
    }

    // ── Default admin user ───────────────────────────────────────────────────

    private void SeedDefaultUser()
    {
        if (!_context.Users.Any(u => u.UserName == "abnite@gmail.com"))
        {
            var newUser = new ApplicationUser
            {
                UserName = "abnite@gmail.com",
                LastName = "Administrator",
                FirstName = "Administrator",
                Email = "abnite@gmail.com",
                EmailConfirmed = true,
            };
            _userManager.CreateAsync(newUser, "Password1").GetAwaiter().GetResult();
            _userManager.AddToRoleAsync(newUser, RoleNames.SystemAdmin).GetAwaiter().GetResult();
        }
    }

    // ── Default role permissions ──────────────────────────────────────────────

    /// <summary>
    /// Seeds the default permission claims for every role based on the Phase 2 proposal.
    /// Permissions are stored as role claims of type <c>"Permission"</c> in ASP.NET Identity.
    /// Only adds missing permissions — does not overwrite existing custom configurations.
    /// </summary>
    private void SeedRolePermissions()
    {
        var roleDefaults = new Dictionary<string, IReadOnlyList<string>>
        {
            { RoleNames.SystemAdmin,           Permissions.SystemAdminPermissions },
            { RoleNames.Student,               Permissions.StudentPermissions },
            { RoleNames.FinanceOfficer,        Permissions.FinanceOfficerPermissions },
            { RoleNames.Registrar,             Permissions.RegistrarPermissions },
            { RoleNames.StudentRecordsOfficer, Permissions.StudentRecordsPermissions },
            { RoleNames.Lecturer,              Permissions.LecturerPermissions },
            { RoleNames.HOD,                   Permissions.HODPermissions },
            { RoleNames.QAOfficer,             Permissions.QAOfficerPermissions },
            { RoleNames.AcademicAffairsOfficer, Permissions.AcademicAffairsPermissions },
            { RoleNames.Principal,             Permissions.PrincipalPermissions },
            { RoleNames.VicePrincipal,         Permissions.PrincipalPermissions },
            { RoleNames.HROfficer,             Permissions.HROfficerPermissions },
        };

        foreach (var (roleName, defaultPerms) in roleDefaults)
        {
            var role = _roleManager.FindByNameAsync(roleName).GetAwaiter().GetResult();
            if (role is null) continue;

            var existingClaims = _roleManager.GetClaimsAsync(role).GetAwaiter().GetResult();
            var existingPermValues = existingClaims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var perm in defaultPerms)
            {
                if (!existingPermValues.Contains(perm))
                {
                    _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, perm))
                        .GetAwaiter().GetResult();
                }
            }
        }
    }

    // ── Reference data ────────────────────────────────────────────────────────

    private void SeedPrograms()
    {
        string[] programs = { "Primary Education", "Early Grade Education", "JHS Education (Home Economics)" };
        foreach (var program in programs)
        {
            if (!_context.CollegePrograms.Any(r => r.ProgramName == program))
            {
                _context.CollegePrograms.Add(new CollegeProgram { ProgramName = program });
                _context.SaveChanges();
            }
        }
    }

    private void SeedStudentSample()
    {
        if (!_context.Students.Any(s => s.ApplicationNumber == "APP123456"))
        {
            var getProgram = _context.CollegePrograms.FirstOrDefault(s => s.ProgramName == "Primary Education");
            var student = new Student
            {
                ApplicationNumber = "APP123456",
                Surname = "Doe",
                OtherNames = "John",
                CollegeProgramId = getProgram!.Id,
                ContactAddress = "123 Main St",
                Religion = "Christian",
                DisabilityStatus = false,
                District = "Some District",
                HomeTown = "Some Town",
                Gender = "Male",
                Email = "john.doe@example.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                PlaceOfBirth = "Some City",
                ReligiousDenom = "Some Denomination",
                PhoneNo = "0246757078",
                MaritalStatus = "Single",
                GhanaianLanguagesSpoken = "Twi, Ga",
                EnrolmentYear = 2023,
                LevelOfEntry = 100,
                CurrentLevel = 100,
                ExpectedCompletionYear = 2027,
            };

            _context.Students.Add(student);
            _context.SaveChanges();
        }
    }
}
