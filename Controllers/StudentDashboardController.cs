using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Payment;
using PWCEPortal.Models.StudentInfo;
using PWCEPortal.ViewModel.Dashboard;
using PWCEPortal.ViewModel.PaymentVM;
using PWCEPortal.ViewModel.Student;

namespace PWCEPortal.Controllers;

[Authorize(Roles = "Student")]
public class StudentDashboardController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;
    private readonly IPaymentService _paymentService;
    private readonly DataHelper _dataHelper;
    private readonly PortalDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    private readonly DataHelper _myHelper;

    public StudentDashboardController(IStudentService studentService, IAcademicService academicService, IPaymentService paymentService, DataHelper dataHelper, PortalDbContext context, IEmailSender emailSender,
        IConfiguration configuration, DataHelper myHelper)
    {
        _studentService = studentService;
        _academicService = academicService;
        _paymentService = paymentService;
        _dataHelper = dataHelper;
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
        _myHelper = myHelper;
        
    }
    
    // GET
    public async Task<IActionResult> Index()
    {
        decimal requiredFeeAmount = 0; 
        var requiredFee = await _context.RequiredFees.Where(r => r.IsActive).FirstOrDefaultAsync();
        var studentEmail = _dataHelper.GetLoggedInuser().Email;
        var studentId = await _context.Students.Where(i => i.Email == studentEmail).Select(i => i.Id)
            .FirstOrDefaultAsync();
        if (studentId == Guid.Empty)
        {
            return RedirectToAction("Login", "Account"); // Redirect to login if not authenticated
        }
        // Fetch student details
        var student = await _studentService.GetStudent_ProgramByIdAsync(studentId);
        if (student == null)
        {
            return NotFound();
        }

       
        
        var currentSemester = await _context.AcademicSemesters.Include(s => s.AcademicYear).Where(s => s.IsRegistrationActive && s.RegistrationStartDate <= DateTime.Now && 
                        s.RegistrationEndDate >= DateTime.Now).FirstOrDefaultAsync();

       // Fetch academic information
        var currentAcademicYear = await _academicService.GetCurrentAcademicYearAsync();
        var registeredCourses = await _academicService.GetRegisteredCoursesAsync(studentId);
        var availableCourses = await _academicService.GetAvailableCoursesAsync(studentId);

        // Fetch payment information
        var feeStructure = await _paymentService.GetFeeStructureAsync(studentId, currentAcademicYear.Id);
        var paymentHistory = await _paymentService.GetPaymentsByStudentIdAsync(studentId);
        var verifiedPaymentHistory = await _paymentService.GetCurrentYearVerifiedPaymentsByStudentIdAsync(studentId);
        var UnverifiedPaymentHistory = await _paymentService.GetCurrentUnVerifiedPaymentsByStudentIdAsync(studentId);
        var totalFee = feeStructure?.FullFee ?? 0;
        var paymentMade = verifiedPaymentHistory.Sum(p => p.AmountPaid);
        var UnverifiedPaymentMade = UnverifiedPaymentHistory.Sum(p => p.AmountPaid);
        var feeAssignment= await _context.StudentFeeAssignments.Where(a => a.StudentId == studentId && a.AcademicYearId == currentAcademicYear.Id).FirstOrDefaultAsync();
       // var outstandingFee = totalFee - paymentMade;
       var outstandingFee = feeAssignment != null ? feeAssignment.OutstandingFee : 0;
       
       // Required Fee percent
       if (requiredFee != null)
       {
           decimal feeStructureAmount= feeStructure?.FullFee ?? 0;
           requiredFeeAmount = (requiredFee.RequiredAmount/100)*feeStructureAmount;
       }

        // Fetch profile information
        var parentsGuardians = await _studentService.GetParentsByStudentIdAsync(studentId);
        var financialInfos = await _studentService.GetFinancialInfoByStudentIdAsync(studentId);
        var educationHistories = await _studentService.GetEducationHistoriesByStudentIdAsync(studentId);

        // Fetch notifications
        var notifications = await _academicService.GetNotificationsAsync(studentId);

        // Add prompts for missing data
        if (!parentsGuardians.Any())
        {
            notifications.Add("Please update your parent/guardian information.");
        }
        if (!financialInfos.Any())
        {
            notifications.Add("Please update your financial information.");
        }
        if (!educationHistories.Any())
        {
            notifications.Add("Please update your educational history.");
        }

        // Create the ViewModel
        var viewModel = new StudentDashboardViewModel
        {
            Student = student,
            Program = student.CollegeProgram,
            CurrentAcademicYear = currentAcademicYear,
            RegisteredCourses = registeredCourses,
            AvailableCourses = availableCourses,
            FeeStructure = feeStructure,
            PaymentHistory = paymentHistory,
            TotalFee = totalFee,
            UnVerifiedPaymentMade = UnverifiedPaymentMade,
            PaymentMade = paymentMade,
            OutstandingFee = outstandingFee,
            ParentsGuardians = parentsGuardians,
            FinancialInfos = financialInfos,
            EducationHistories = educationHistories,
            Notifications = notifications,
          //  RequiredFeeAmount = requiredFee != null ? requiredFee.RequiredAmount : 0,
            RequiredFeeAmount = requiredFeeAmount,
            CurrentSemester = currentSemester,
            PassportPicturePath = student.PassportPicturePath,
        };

        return View(viewModel);
    }
    
    public async Task<IActionResult> EditProfile(Guid studentId)
    {
        // Fetch student details
        var student = await _studentService.GetStudent_ProgramByIdAsync(studentId);
        if (student == null)
        {
            return NotFound();
        }

        // Fetch parent/guardian information
        var parentsGuardians = await _studentService.GetParentsByStudentIdAsync(studentId);

        // Fetch financial information
        var financialInfos = await _studentService.GetFinancialInfoByStudentIdAsync(studentId);

        // Fetch educational history
        var educationHistories = await _studentService.GetEducationHistoriesByStudentIdAsync(studentId);

        // Create a ViewModel to pass all data to the view
        var viewModel = new EditProfileViewModel
        {
            Student = student,
            ParentsGuardians = parentsGuardians,
            FinancialInfos = financialInfos,
            EducationHistories = educationHistories
        };

        return View(viewModel);
    }
    
    public async Task<IActionResult> EditStudent(Guid studentId)
    {
        var student = await _studentService.GetStudentByIdAsync(studentId);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Student not found";
            return RedirectToAction("Index");
        }
        
        var viewModel = new StudentViewModel
        {
            Id = student.Id,
            ApplicationNumber = student.ApplicationNumber,
            StudentId = student.StudentID,
            Title = student.Title,
            Surname = student.Surname,
            OtherNames = student.OtherNames,
            CollegeProgramId = student.CollegeProgramId,
            Email = student.Email,
            PhoneNo = student.PhoneNo,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            EnrolmentYear = student.EnrolmentYear,
            CurrentLevel = student.CurrentLevel,
            CollegePrograms = await GetCollegeProgramsAsync(),
            EntryLevel = student.LevelOfEntry,
            ContactAddress = student.ContactAddress,
            Religion = student.Religion,
            ReligiousDenomination = student.ReligiousDenom,
            District = student.District,
            HomeTown = student.HomeTown,
            DisabilityStatus = student.DisabilityStatus.HasValue ? student.DisabilityStatus.Value : false,
            PlaceofBirth = student.PlaceOfBirth,
            MaritalStatus = student.MaritalStatus,
            GhanaianlanguagesSpoken = student.GhanaianLanguagesSpoken,
            CollegeClassId = student.CollegeClassId,
            CollegeHallId = student.CollegeHallId,
            ExistingPassportPicturePath = student.PassportPicturePath
            
        };

        return View(viewModel);
    }

// POST: StudentDashboard/EditStudent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStudent(Guid id, StudentViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            TempData["ErrorMessage"] = "Student not found";
            return RedirectToAction("Index");
        }

        if (ModelState.IsValid)
        {
            var student = await _studentService.GetStudentByIdAsync(viewModel.Id);
            
            // Handle file upload
            if (viewModel.PassportPicture != null && viewModel.PassportPicture.Length > 0)
            {
                // Delete old file if exists
                if (!string.IsNullOrEmpty(student.PassportPicturePath))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.PassportPicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "passports");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{viewModel.PassportPicture.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.PassportPicture.CopyToAsync(fileStream);
                }

                // Update the path (relative to wwwroot)
                student.PassportPicturePath = $"/uploads/passports/{uniqueFileName}";
            }
            /*var student = new Student
            {
                Id = viewModel.Id,
                ApplicationNumber = viewModel.ApplicationNumber,
                Title = viewModel.Title,
                Surname = viewModel.Surname,
                OtherNames = viewModel.OtherNames,
                CollegeProgramId = viewModel.CollegeProgramId,
              ContactAddress  = viewModel.ContactAddress,
                Email = viewModel.Email,
                PhoneNo = viewModel.PhoneNo,
                DateOfBirth = viewModel.DateOfBirth,
                Gender = viewModel.Gender,
                EnrolmentYear = viewModel.EnrolmentYear, 
                Religion= viewModel.Religion,
                ReligiousDenom = viewModel.ReligiousDenomination,
                DisabilityStatus = viewModel.DisabilityStatus,
                District = viewModel.District,
                HomeTown = viewModel.HomeTown,
                
                PlaceOfBirth = viewModel.PlaceofBirth,
                CurrentLevel = getStudent.CurrentLevel,
                LevelOfEntry = getStudent.LevelOfEntry,
                OutstandingFees = getStudent.OutstandingFees,
                MaritalStatus = getStudent.MaritalStatus,
                GhanaianLanguagesSpoken = getStudent.GhanaianLanguagesSpoken,
                
            };*/
            
            // Update the properties of the existing entity
            student.ApplicationNumber = viewModel.ApplicationNumber;
            student.StudentID = viewModel.StudentId;
            student.Title = viewModel.Title;
            student.Surname = viewModel.Surname;
            student.OtherNames = viewModel.OtherNames;
            student.CollegeProgramId = viewModel.CollegeProgramId;
            student.ContactAddress = viewModel.ContactAddress;
            student.Email = viewModel.Email;
            student.PhoneNo = viewModel.PhoneNo;
            student.DateOfBirth = viewModel.DateOfBirth;
            student.Gender = viewModel.Gender;
            student.EnrolmentYear = viewModel.EnrolmentYear;
            student.Religion = viewModel.Religion;
            student.ReligiousDenom = viewModel.ReligiousDenomination;
            student.DisabilityStatus = viewModel.DisabilityStatus;
            student.District = viewModel.District;
            student.HomeTown = viewModel.HomeTown;
            student.PlaceOfBirth = viewModel.PlaceofBirth;
            student.MaritalStatus = viewModel.MaritalStatus;
            student.GhanaianLanguagesSpoken = viewModel.GhanaianlanguagesSpoken;
            student.CollegeClassId = viewModel.CollegeClassId;
            student.CollegeHallId = viewModel.CollegeHallId;
            
            await _studentService.UpdateStudentAsync(student);
            TempData["SuccessMessage"] = "Student updated successfully";
            return RedirectToAction(nameof(Index));
        }
       
        // Repopulate dropdown if validation fails
        viewModel.CollegePrograms = await GetCollegeProgramsAsync();
        return View(viewModel);
    }
    
        // GET: StudentDashboard/AddParentGuardian
    public IActionResult AddParentGuardian(Guid studentId)
    {
        var viewModel = new ParentGuardianViewModel
        {
            StudentId = studentId
        };
        return View(viewModel);
    }

    // POST: StudentDashboard/AddParentGuardian
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddParentGuardian(ParentGuardianViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var parentGuardian = new ParentGuardian
            {
                FullName = viewModel.FullName,
                Email = viewModel.Email,
                Telephone = viewModel.Telephone,
                Occupation = viewModel.Occupation,
                ContactAddress = viewModel.ContactAddress,
                Relationship = viewModel.Relationship,
                StudentId = viewModel.StudentId // Use the StudentId from the viewModel
            };
               
            await _studentService.AddParentGuardianAsync(parentGuardian);
            TempData["SuccessMessage"] = "Parent Guardian added";
            return RedirectToAction(nameof(EditProfile), new { studentId =viewModel.StudentId});
        }
        TempData["ErrorMessage"] = "Parent Guardians not added";
        ViewBag.StudentId = viewModel.StudentId;
        return RedirectToAction("EditProfile", new { studentId = viewModel.StudentId });
    }

    // GET: StudentDashboard/EditParentGuardian
    public async Task<IActionResult> EditParentGuardian(Guid id)
    {
        var parentGuardian = await _studentService.GetParentGuardianByIdAsync(id);
        if (parentGuardian == null)
        {
            TempData["ErrorMessage"] = "Parent Guardian not found";
            return RedirectToAction(nameof(Index));
        }
        var viewModel = new ParentGuardianViewModel
        {
            Id = parentGuardian.Id,
            FullName = parentGuardian.FullName,
            Email = parentGuardian.Email,
            Telephone = parentGuardian.Telephone,
            Occupation = parentGuardian.Occupation,
            ContactAddress = parentGuardian.ContactAddress,
            Relationship = parentGuardian.Relationship,
            StudentId = parentGuardian.StudentId // Pass the StudentId
        };

        return View(viewModel);
    }

    // POST: StudentDashboard/EditParentGuardian
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditParentGuardian(Guid id, ParentGuardianViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            TempData["ErrorMessage"] = "ParentGuardian not found";
            return RedirectToAction(nameof(EditProfile), new { studentId = viewModel.StudentId });
        }

        if (ModelState.IsValid)
        {
            var parentGuardian = new ParentGuardian
            {
                Id = viewModel.Id,
                FullName = viewModel.FullName,
                Email = viewModel.Email,
                Telephone = viewModel.Telephone,
                Occupation = viewModel.Occupation,
                ContactAddress = viewModel.ContactAddress,
                Relationship = viewModel.Relationship,
                StudentId = viewModel.StudentId // Use the StudentId from the viewModel
            };

            await _studentService.UpdateParentGuardianAsync(parentGuardian);
            TempData["SuccessMessage"] = "Parent Guardian updated";
            return RedirectToAction(nameof(EditProfile), new { studentId = parentGuardian.StudentId });
        }
        return View(viewModel);
    }

    // GET: StudentDashboard/DeleteParentGuardian
    public async Task<IActionResult> DeleteParentGuardian(Guid id)
    {
        var parentGuardian = await _studentService.GetParentGuardianByIdAsync(id);
        if (parentGuardian == null)
        {
            TempData["ErrorMessage"] = "ParentGuardian not found";
            return RedirectToAction(nameof(Index));
        } var viewModel = new ParentGuardianViewModel
        {
            Id = parentGuardian.Id,
            FullName = parentGuardian.FullName,
            Email = parentGuardian.Email,
            Telephone = parentGuardian.Telephone,
            Occupation = parentGuardian.Occupation,
            ContactAddress = parentGuardian.ContactAddress,
            Relationship = parentGuardian.Relationship,
            StudentId = parentGuardian.StudentId // Pass the StudentId
        };

        return View(viewModel);
    }

    // POST: StudentDashboard/DeleteParentGuardian
    [HttpPost, ActionName("DeleteParentGuardian")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteParentGuardianConfirmed(Guid id)
    {
        var parentGuardian = await _studentService.GetParentGuardianByIdAsync(id);
        if (parentGuardian != null)
        {
            await _studentService.DeleteParentGuardianAsync(id);
            TempData["SuccessMessage"] = "Parent Guardian deleted";
            return RedirectToAction(nameof(EditProfile), new { studentId = parentGuardian.StudentId });
        }
        TempData["ErrorMessage"] = "Parent Guardian not found";
        return RedirectToAction(nameof(EditProfile), new { studentId = parentGuardian.StudentId });
    }
    
     // GET: Student/AddFinancialInfo/5
        public IActionResult AddFinancialInfo(Guid studentId)
        {
            var viewModel = new FinancialInfoViewModel
            {
                StudentId = studentId // Set the StudentId from the route parameter
            };
            return View(viewModel);
        }

        // POST: Student/AddFinancialInfo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFinancialInfo(FinancialInfoViewModel viewModel)
        {
            var getStudentFinancialInfo = await _studentService.GetFinancialInfoByStudentIdAsync(viewModel.StudentId);
            if (getStudentFinancialInfo.Count>0)
            {
                TempData["ErrorMessage"] = "Financial Info already Created";
                return RedirectToAction(nameof(EditProfile), new { studentId = viewModel.StudentId });
            }
            if (ModelState.IsValid)
            {
                var financialInfo = new FinancialInfo
                {
                    SSNITNumber = viewModel.SSNITNumber,
                    EZwichAccountName = viewModel.EZwichAccountName,
                    EZwichAccountNumber = viewModel.EZwichAccountNumber,
                    StudentId = viewModel.StudentId // Use the StudentId from the viewModel
                };

                await _studentService.AddFinancialInfoAsync(financialInfo);
                TempData["SuccessMessage"] = "Financial Info added successfully.";
                return RedirectToAction(nameof(EditProfile), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }

        // GET: Student/EditFinancialInfo/5
        public async Task<IActionResult> EditFinancialInfo(Guid id)
        {
            var financialInfo = await _studentService.GetFinancialInfoByIdAsync(id);
            if (financialInfo == null)
            {
                return NotFound();
            }

            var viewModel = new FinancialInfoViewModel
            {
                Id = financialInfo.Id,
                SSNITNumber = financialInfo.SSNITNumber,
                EZwichAccountName = financialInfo.EZwichAccountName,
                EZwichAccountNumber = financialInfo.EZwichAccountNumber,
                StudentId = financialInfo.StudentId // Pass the StudentId
            };

            return View(viewModel);
        }

        // POST: Student/EditFinancialInfo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinancialInfo(Guid id, FinancialInfoViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var financialInfo = new FinancialInfo
                {
                    Id = viewModel.Id,
                    SSNITNumber = viewModel.SSNITNumber,
                    EZwichAccountName = viewModel.EZwichAccountName,
                    EZwichAccountNumber = viewModel.EZwichAccountNumber,
                    StudentId = viewModel.StudentId // Use the StudentId from the viewModel
                };

                await _studentService.UpdateFinancialInfoAsync(financialInfo);
                TempData["SuccessMessage"] = "Financial Info updated successfully.";
                return RedirectToAction(nameof(EditProfile), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }
        
        
        // GET: Student/DeleteFinancialInfo/5
        public async Task<IActionResult> DeleteFinancialInfo(Guid id)
        {
            var financialInfo = await _studentService.GetFinancialInfoByIdAsync(id);
            if (financialInfo == null)
            {
                return NotFound();
            }

            var viewModel = new FinancialInfoViewModel
            {
                Id = financialInfo.Id,
                SSNITNumber = financialInfo.SSNITNumber,
                EZwichAccountName = financialInfo.EZwichAccountName,
                EZwichAccountNumber = financialInfo.EZwichAccountNumber,
                StudentId = financialInfo.StudentId // Pass the StudentId
            };

            return View(viewModel);
        }
        
        // POST: Student/DeleteFinancialInfo/5
        [HttpPost, ActionName("DeleteFinancialInfo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFinancialInfoConfirmed(Guid id)
        {
            var financialInfo = await _studentService.GetFinancialInfoByIdAsync(id);
            if (financialInfo != null)
            {
                await _studentService.DeleteFinancialInfoAsync(id);
                TempData["SuccessMessage"] = "Financial Info deleted successfully.";
                return RedirectToAction(nameof(EditProfile), new { studentId = financialInfo.StudentId });
            }

            return NotFound();
        }
        
         public IActionResult AddEducationHistory(Guid studentId)
        {
            var viewModel = new EducationHistoryViewModel
            {
                StudentId = studentId // Set the StudentId from the route parameter
            };
            return View(viewModel);
        }

        // POST: Student/AddEducationHistory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEducationHistory(EducationHistoryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var educationHistory = new EducationHistory
                {
                    SchoolName = viewModel.SchoolName,
                    FromDate = viewModel.FromDate,
                    ToDate = viewModel.ToDate,
                    OfficeHeld = viewModel.OfficeHeld,
                    StudentId = viewModel.StudentId // Use the StudentId from the viewModel
                };

                await _studentService.AddEducationHistoryAsync(educationHistory);
                TempData["SuccessMessage"] = "Education History added successfully.";
                return RedirectToAction(nameof(EditProfile), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }

        // GET: Student/EditEducationHistory/5
        public async Task<IActionResult> EditEducationHistory(Guid id)
        {
            var educationHistory = await _studentService.GetEducationHistoryByIdAsync(id);
            if (educationHistory == null)
            {
                return NotFound();
            }

            var viewModel = new EducationHistoryViewModel
            {
                Id = educationHistory.Id,
                SchoolName = educationHistory.SchoolName,
                FromDate = educationHistory.FromDate,
                ToDate = educationHistory.ToDate,
                OfficeHeld = educationHistory.OfficeHeld,
                StudentId = educationHistory.StudentId // Pass the StudentId
            };

            return View(viewModel);
        }

        // POST: Student/EditEducationHistory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEducationHistory(Guid id, EducationHistoryViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var educationHistory = new EducationHistory
                {
                    Id = viewModel.Id,
                    SchoolName = viewModel.SchoolName,
                    FromDate = viewModel.FromDate,
                    ToDate = viewModel.ToDate,
                    OfficeHeld = viewModel.OfficeHeld,
                    StudentId = viewModel.StudentId // Use the StudentId from the viewModel
                };

                await _studentService.UpdateEducationHistoryAsync(educationHistory);
                TempData["SuccessMessage"] = "Education History updated successfully.";
                return RedirectToAction(nameof(EditProfile), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }

        // GET: Student/DeleteEducationHistory/5
        public async Task<IActionResult> DeleteEducationHistory(Guid id)
        {
            var educationHistory = await _studentService.GetEducationHistoryByIdAsync(id);
            if (educationHistory == null)
            {
               TempData["ErrorMessage"] = "The EducationHistory doesn't exist.";
               return RedirectToAction(nameof(Index));
            }

            var viewModel = new EducationHistoryViewModel
            {
                Id = educationHistory.Id,
                SchoolName = educationHistory.SchoolName,
                FromDate = educationHistory.FromDate,
                ToDate = educationHistory.ToDate,
                OfficeHeld = educationHistory.OfficeHeld,
                StudentId = educationHistory.StudentId // Pass the StudentId
            };

            return View(viewModel);
        }
        
        // POST: Student/DeleteEducationHistory/5
        [HttpPost, ActionName("DeleteEducationHistory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducationHistoryConfirmed(Guid id)
        {
            var educationHistory = await _studentService.GetEducationHistoryByIdAsync(id);
            if (educationHistory != null)
            {
                await _studentService.DeleteEducationHistoryAsync(id);
                TempData["SuccessMessage"]="Education history has been deleted";
                return RedirectToAction(nameof(EditProfile), new { studentId = educationHistory.StudentId });
            } 
            TempData["ErrorMessage"]=$"Unable to delete education history. Please try again.";
           return RedirectToAction(nameof(EditProfile), new { studentId = educationHistory.StudentId });
        }
        
        private async Task<List<SelectListItem>> GetCollegeProgramsAsync()
        {
            var programs = await _studentService.GetCollegeProgramsAsync();
            return programs.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.ProgramName
            }).ToList();
        }
        
        /// Payment and Slip Generatetion
        public async Task<IActionResult> MakePayment(Guid studentId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == studentId && s.IsDeleted == false);
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student record not found.";
                return RedirectToAction("Index", "StudentDashboard");
            }
            
            // Retrieve active academic year
            var activeAcademicYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
            if (activeAcademicYear == null)
            {
                TempData["ErrorMessage"] = "No active academic year found.";
                return RedirectToAction("Index", "StudentDashboard");
            }
            
            // Retrieve the fee assignment for the active academic year
            var feeAssignment = await _context.StudentFeeAssignments
                .FirstOrDefaultAsync(fa => fa.StudentId == student.Id && fa.AcademicYearId == activeAcademicYear.Id);
            
            decimal outstandingFee = feeAssignment != null ? feeAssignment.OutstandingFee : 0;
            
            var model = new MakePaymentViewModel
            {
                StudentId = student.Id,
                StudentName = $"{student.Surname} {student.OtherNames}",
                OutstandingFee = outstandingFee,
                AmountToPay = 0
            };
            
            return View(model);
        }
        
        // POST: /StudentDashboard/MakePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePayment(MakePaymentViewModel model)
        {
            ModelState.Remove("StudentName");
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            // Check if student has any pending payments that are not deleted
            if (await HasPendingPayment(model.StudentId))
            {
                var pendingPayment = await GetPendingPayment(model.StudentId);
                TempData["ErrorMessage"] = $"You already have a pending payment with reference: {pendingPayment.PaymentReference}. " +
                                           "Please wait for it to be verified or delete it before creating a new payment.";
                return View(model);
            }
            
            // Retrieve student record based on the view model's StudentId
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == model.StudentId);
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student record not found.";
                return RedirectToAction("Index", "StudentDashboard");
            }
            
            // Retrieve active academic year
            var activeAcademicYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
            if (activeAcademicYear == null)
            {
                TempData["ErrorMessage"] = "No active academic year found.";
                return RedirectToAction("Index", "StudentDashboard");
            }
            
            // Generate a unique payment reference that includes the academic year.
            string academicYearCode = activeAcademicYear.Year.Replace("/", "");  // e.g., "20242025"
            string paymentReference = $"{academicYearCode}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            
            var payment = new Payment
            {
                StudentId = student.Id,
                CurrentAcademicYearId = activeAcademicYear.Id,
                AmountPaid = model.AmountToPay,
                PaymentDate = DateTime.UtcNow,
                IsVerified = false,
                AddedBy = _dataHelper.GetLoggedInuser(),
                PaymentReference = paymentReference,
                PaymentMethod = "Bank Payment"
            };
            
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Payment initiated successfully. Your payment reference is {paymentReference}.";
            // Redirect to the Payment Slip view
            return RedirectToAction("PaymentSlip", new { paymentId = payment.Id });
        }
        
        // GET: /StudentDashboard/PaymentSlip?paymentId={id}
        public async Task<IActionResult> PaymentSlip(Guid paymentId)
        {
            var payment = await _context.Payments.Include(p => p.Student).Include(i=>i.Student.CollegeProgram).FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null)
                return NotFound();
            return View(payment);
        }
        
        public async Task<IActionResult> PaymentReceipt(Guid paymentId)
        {
            var payment = await _context.Payments.Include(p => p.Student).Include(i=>i.Student.CollegeProgram).Include(i=>i.VerifiedBy).Where(i=>i.IsVerified).FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment record not found.";
            }
            return View(payment);
        }
        
        // GET: /StudentDashboard/Payments
        public async Task<IActionResult> Payments(Guid studentId)
        {
            string email = User.Identity.Name;
            var student = await _studentService.GetStudentByIdAsync(studentId); 
            //_context.Students.FirstOrDefaultAsync(s => s.Email == email);
            
            if (student == null)
                return RedirectToAction("Login", "Account");
                
            var payments = await _context.Payments
                .Where(p => p.StudentId == student.Id && p.IsDeleted==false)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            return View(payments);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePayment(Guid paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    return Json(new { success = false, message = "Payment not found." });
                }

                // Only allow deletion of pending payments
                if (payment.IsVerified)
                {
                    return Json(new { success = false, message = "Cannot delete verified payments." });
                }

                payment.IsDeleted = true;
                payment.DateDeleted = DateTime.UtcNow;
                payment.DeletedBy = _myHelper.GetLoggedInuser();

                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment deleted successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception
              //  _logger.LogError(ex, "Error deleting payment {PaymentId}", paymentId);
                return Json(new { success = false, message = "An error occurred while deleting the payment." });
            }
        }
        
        // GET: /StudentDashboard/UploadReceipt?paymentId={id}
        public IActionResult UploadReceipt(Guid paymentId)
        {
           // var getpayment= _context.Payments.FindAsync(paymentId);
            var getpayment=_context.Payments.Where(i=>i.Id==paymentId).FirstOrDefault();
            
            var student =  _context.Students.Where(s => s.Id == getpayment.StudentId && s.IsDeleted == false).FirstOrDefault();
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student record not found.";
                return RedirectToAction("Index", "StudentDashboard");
            }
            
            ViewBag.PaymentId = paymentId;
            ViewBag.studentId = getpayment.StudentId;
            return View();
        }
        
        // POST: /StudentDashboard/UploadReceipt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadReceipt(Guid paymentId, IFormFile receiptFile, string bankReferenceNo)
        {
            if (receiptFile == null || receiptFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid file.");
                ViewBag.PaymentId = paymentId;
                return View();
            }
            
            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(receiptFile.FileName).ToLowerInvariant();
    
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", "Only PDF, JPG, JPEG, and PNG files are allowed.");
                ViewBag.PaymentId = paymentId;
                return View();
            }
            
            var payment = await _context.Payments.Include(i=>i.Student).Include(i=>i.Student.CollegeProgram).FirstOrDefaultAsync(i=>i.Id== paymentId);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment record not found.";
                return RedirectToAction("index");
            }

            if (payment.IsVerified)
            {
                TempData["ErrorMessage"] = "Payment is already verified.";
                return RedirectToAction("Payments", new { studentId = payment.StudentId });
            }
            
            //check Bank reference if already in detabase
            var checkBankReference= await _context.Payments.Where(i=>i.BankReferenceNumber==bankReferenceNo && i.IsDeleted==false).FirstOrDefaultAsync();
            if (checkBankReference != null)
            {
                TempData["ErrorMessage"]="This Bank Reference Number "+bankReferenceNo+" has already been uploaded.";
                return RedirectToAction("index"); 
            }
            
            string applicationNumber = payment.Student.ApplicationNumber;
            var activeAcademicYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
            string academicYearCode = activeAcademicYear != null ? activeAcademicYear.Year.Replace("/", "") : "NA";
            
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string extension = Path.GetExtension(receiptFile.FileName);
            string fileName = applicationNumber+"_"+academicYearCode+"_"+timestamp+extension;


            
            // Define the upload folder path (ensure this folder exists in wwwroot/receipts)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "receipts");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            
            //var fileName = Guid.NewGuid().ToString() + Path.GetExtension(receiptFile.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await receiptFile.CopyToAsync(stream);
            }
               
            
            // Save the relative path in the Payment record.
            payment.ReceiptFilePath = "/receipts/" + fileName;
            payment.BankReferenceNumber = bankReferenceNo;
            payment.uploadReceiptDate = DateTime.UtcNow;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            
            await SendReceiptUploadNotification(payment,bankReferenceNo);
            
            TempData["SuccessMessage"] = "Receipt uploaded successfully. Await verification.";
            return RedirectToAction("Payments",new{studentId = payment.StudentId});
        }


        private async Task SendReceiptUploadNotification(Payment payment, string bankReferenceNo)
        {
            var accountOfficerEmail = _configuration["AccountEmail:Email"];
            try
            {

                var student = payment.Student;
                var paymentDetails = new
                {
                    StudentName = student.Surname + " " + student.OtherNames,
                    ApplicationNumber = student.ApplicationNumber,
                    ProgramName = student.CollegeProgram?.ProgramName ?? "Not specified",
                    Amount = payment.AmountPaid.ToString("C"),
                    PaymentDate = payment.PaymentDate.ToString("dd MMMM yyyy"),
                    BankReference = bankReferenceNo,
                    UploadDate = DateTime.UtcNow.ToString("dd MMMM yyyy HH:mm"),
                    ReceiptUrl = $"{Request.Scheme}://{Request.Host}{payment.ReceiptFilePath}"
                };

                var emailSubject =
                    $"[Action Required] Payment Receipt Uploaded - {paymentDetails.StudentName} ({paymentDetails.ApplicationNumber})";

                var emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 20px auto; padding: 20px; }}
                        .header {{ color: #2c3e50; border-bottom: 1px solid #eee; padding-bottom: 10px; }}
                        .details {{ background: #f9f9f9; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .detail-row {{ margin-bottom: 10px; }}
                        .label {{ font-weight: 600; color: #2c3e50; }}
                        .footer {{ font-size: 0.9em; color: #777; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
                        .button {{ 
                            display: inline-block; padding: 10px 15px; background: #3498db; 
                            color: white; text-decoration: none; border-radius: 4px; margin-top: 10px;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Payment Receipt Notification</h2>
                        </div>
                        
                        <p>Dear Account Officer,</p>
                        
                        <p>A student has uploaded a payment receipt that requires your verification:</p>
                        
                        <div class='details'>
                            <div class='detail-row'><span class='label'>Student Name:</span> {paymentDetails.StudentName}</div>
                            <div class='detail-row'><span class='label'>Student ID Number:</span> {paymentDetails.ApplicationNumber}</div>
                            <div class='detail-row'><span class='label'>Program:</span> {paymentDetails.ProgramName}</div>
                            <div class='detail-row'><span class='label'>Amount Paid:</span> {paymentDetails.Amount}</div>
                            <div class='detail-row'><span class='label'>Payment Date:</span> {paymentDetails.PaymentDate}</div>
                            <div class='detail-row'><span class='label'>Bank Reference:</span> {paymentDetails.BankReference}</div>
                            <div class='detail-row'><span class='label'>Uploaded On:</span> {paymentDetails.UploadDate} (UTC)</div>
                        </div>
                        
                        <p>Please verify the payment details at your earliest convenience:</p>
                        <a href='{paymentDetails.ReceiptUrl}' class='button'>View Receipt</a>
                        
                        <div class='footer'>
                            <p>This is an automated notification. Please do not reply to this email.</p>
                            <p>© {DateTime.UtcNow.Year} PWCE Portal. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                await _emailSender.SendGmailEmailAsync(accountOfficerEmail, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Error sending payment receipt notification email");
                var ErrorMsg = ex.Message + " Error sending payment receipt notification email";
                TempData["ErrorMessage"] = ErrorMsg;
            }
        }
        
        private async Task<bool> HasPendingPayment(Guid studentId)
        {
            return await _context.Payments
                .AnyAsync(p => p.StudentId == studentId && 
                               !p.IsVerified && 
                               p.IsDeleted==false);
        }

        private async Task<Payment> GetPendingPayment(Guid studentId)
        {
            return await _context.Payments
                .Where(p => p.StudentId == studentId && 
                            !p.IsVerified && 
                            p.IsDeleted==false).FirstOrDefaultAsync();
        }

}