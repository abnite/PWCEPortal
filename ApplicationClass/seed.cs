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
    
         public void  InsertDB()
        {
          //  string[] Roles = new string[] { "System Admin", "Student" , "Administrator", "Finance Officer", "Secretary"};
          var rolesWithDescriptions = new Dictionary<string, string>
          {
              { "System Admin", "This is the system admin role" },
              { "Student", "This is the student role" },
              { "Administrator", "This is the administrator role" },
              { "Finance Officer", "This is the finance officer role" },
              { "Secretary", "This is the secretary role" },
              { "Registrar", "Responsible for editing and updating student details " },
              { "Student Records Officer", "Responsible for editing and updating student records" }

          };

            foreach (var Role in rolesWithDescriptions)
            {
                var roleStore = new RoleStore<IdentityRole>(_context);
                if (!_context.Roles.Any(r=>r.Name==Role.Key))
                {
                    ApplicationRole applicationRole = new ApplicationRole();
                    applicationRole.Name = Role.Key;
                    applicationRole.NormalizedName = Role.Key.ToUpper();
                    applicationRole.Description = Role.Value;
                    IdentityResult identityResult = roleStore.CreateAsync(applicationRole).Result;
                }
            }

            if (!_context.Users.Any(u => u.UserName == "abnite@gmail.com"))
            {
                ApplicationUser newUser = new ApplicationUser
                {
                    UserName = "abnite@gmail.com",
                    LastName = "Administrator",
                    FirstName = "Administrator",
                    Email = "abnite@gmail.com",
                    EmailConfirmed = true
                };
                IdentityResult results = _userManager.CreateAsync(newUser, "Password1").Result;
                IdentityResult addRole = _userManager.AddToRoleAsync(newUser, "System Admin").Result;

            }
            
            string[] Programs = new string[] { "Primary Education", "Early Grade Education" , "JHS Education (Home Economics)"};
            foreach (var program in Programs)
            {
                if (!_context.CollegePrograms.Any(r=>r.ProgramName==program))
                {
                    var collegeparogram = new CollegeProgram
                    {
                        ProgramName = program
                    };
                    
                    _context.CollegePrograms.Add(collegeparogram);
                    _context.SaveChanges();
                }
            }
            
            // Seed a student record
            if (!_context.Students.Any(s => s.ApplicationNumber == "APP123456"))
            {
                var getProgram= _context.CollegePrograms.FirstOrDefault(s => s.ProgramName == "Primary Education");
                var student = new Student
                {
                    ApplicationNumber = "APP123456",
                    Surname = "Doe",
                    OtherNames = "John",
                    CollegeProgramId = getProgram.Id,
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