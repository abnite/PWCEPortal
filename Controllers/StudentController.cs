using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.StudentInfo;
using PWCEPortal.ViewModel.Student;

namespace PWCEPortal.Controllers;

[Authorize]
public class StudentController : Controller
{
    public readonly IStudentService _studentService;
    public readonly PortalDbContext _context;

    public StudentController(IStudentService studentService, PortalDbContext context)
    {
        _studentService = studentService;
        _context = context;
    }
    // Student
    [Authorize(Roles = "System Admin, Student Records Officer, Registrar")]
    public async Task<IActionResult> Index( string searchQuery = null, int? level = null, string status = null, int page = 1, int pageSize = 10)
    {
            var (students, totalCount) = await _studentService.GetStudentsAsync(searchQuery, level,status, page, pageSize);

            ViewBag.SearchQuery = searchQuery;
            ViewBag.Level = level;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
        return View(students);
    }
    // Download action
    [Authorize(Roles = "System Admin, Student Records Officer, Registrar")]
    public async Task<IActionResult> Download(string searchQuery = null, int? level = null)
    {
        var fileContents = await _studentService.DownloadStudentsAsync(searchQuery, level);
        return File(fileContents, "text/csv", "Students.csv");
    }
    
    // GET: Student/Details/5
    [Authorize(Roles = "System Admin, Student Records Officer, Registrar")]
    public async Task<IActionResult> Details(Guid id)
    {
        var student = await _studentService.GetStudent_ProgramByIdAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        // Fetch related data
        var parents = await _studentService.GetParentsByStudentIdAsync(id);
        var financialInfo = await _studentService.GetFinancialInfoByStudentIdAsync(id);
        var educationHistories = await _studentService.GetEducationHistoriesByStudentIdAsync(id);

        // Pass data to the view
        ViewBag.Parents = parents;
        ViewBag.FinancialInfo = financialInfo;
        ViewBag.EducationHistories = educationHistories;

        return View(student);
    }
    
    // GET: Student/Create
    [Authorize(Roles = "System Admin, Registrar")]
    public async Task<IActionResult> Create()
    {
        var viewModel = new StudentViewModel
        {
            CollegePrograms = await GetCollegeProgramsAsync()
        };
        return View(viewModel);
    }
    
    // POST: Student/Create
    [Authorize(Roles = "System Admin, Registrar")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentViewModel model)
    {
        if (ModelState.IsValid)
        {
            var checkStudent = await _context.Students.Where(i => i.ApplicationNumber == model.ApplicationNumber).FirstOrDefaultAsync();
            if (checkStudent != null)
            {
                TempData["ErrorMessage"] = "Student Application number / ID number already exit";
                model.CollegePrograms = await GetCollegeProgramsAsync();
                return RedirectToAction(nameof(Index));
            }
            
            string certificatePath = null;
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                // Verify that the file has a .pdf extension
                var extension = Path.GetExtension(model.CertificateFile.FileName);
                if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("CertificateFile", "Only PDF files are allowed.");
                    model.CollegePrograms = await GetCollegeProgramsAsync();
                    return RedirectToAction(nameof(Index));
                }
                
                // Define the target folder (e.g., wwwroot/Certificates)
                var certificatesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Certificates");
                if (!Directory.Exists(certificatesFolder))
                    Directory.CreateDirectory(certificatesFolder);
                    
                // Use ApplicationNumber and current timestamp for a unique file name.
                //string extension = Path.GetExtension(model.CertificateFile.FileName);
                string fileName = $"{model.ApplicationNumber}_Certificate_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}{extension}";
                string fullPath = Path.Combine(certificatesFolder, fileName);
                    
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await model.CertificateFile.CopyToAsync(stream);
                }
                    
                // Save relative path
                certificatePath = "/Certificates/" + fileName;
            }
            var student = new Student
            {
                ApplicationNumber = model.ApplicationNumber,
                Title = model.Title,
                Surname = model.Surname,
                OtherNames = model.OtherNames,
                CollegeProgramId = model.CollegeProgramId,
                Email = model.Email,
                PhoneNo = model.PhoneNo,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                EnrolmentYear = model.EnrolmentYear,
                CurrentLevel = model.CurrentLevel,
                LevelOfEntry = model.EntryLevel,
                StudentID = model.StudentId,
                
                CertificateFilePath = certificatePath
                
            };
            
            await _studentService.AddStudentAsync(student);
            TempData["SuccessMessage"] = "Student created";
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = "Error in Creating Student";
        model.CollegePrograms = await GetCollegeProgramsAsync();
        return View(model);
    }
    
    // GET: Student/Edit/5
    [Authorize(Roles = "System Admin, Student Records Officer, Registrar")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);
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
            CertificateFilePath = student.CertificateFilePath
            
        };

        return View(viewModel);
    }

    // POST: Student/Edit/5
    [Authorize(Roles = "System Admin, Student Records Officer, Registrar")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StudentViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
               TempData["ErrorMessage"] = "Student not found";
               return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                
                var student =await _context.Students.Where(i => i.Id == viewModel.Id).FirstOrDefaultAsync();
                
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found";
                    return RedirectToAction("Index");
                }
                
                if (viewModel.CertificateFile != null && viewModel.CertificateFile.Length > 0)
                {
                    // Verify that the file has a .pdf extension
                    var extension = Path.GetExtension(viewModel.CertificateFile.FileName);
                    if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("CertificateFile", "Only PDF files are allowed.");
                        viewModel.CollegePrograms = await GetCollegeProgramsAsync();
                        return RedirectToAction(nameof(Index));
                    }
                
                    // Define the target folder (e.g., wwwroot/Certificates)
                    var certificatesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Certificates");
                    if (!Directory.Exists(certificatesFolder))
                    {
                        Directory.CreateDirectory(certificatesFolder);
                    }
                    
                    // Delete old file if it exists
                    if (!string.IsNullOrEmpty(student.CertificateFilePath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.CertificateFilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }


                    // Use ApplicationNumber and current timestamp for a unique file name.
                    //string extension = Path.GetExtension(model.CertificateFile.FileName);
                    string fileName = $"{viewModel.ApplicationNumber}_Certificate_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}{extension}";
                    string fullPath = Path.Combine(certificatesFolder, fileName);
                    
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await viewModel.CertificateFile.CopyToAsync(stream);
                    }
                    
                    // Save relative path
                    student.CertificateFilePath = "/Certificates/" + fileName;
                }
                
                
                /*var student = new Student
                {
                    Id = viewModel.Id,
                    ApplicationNumber = viewModel.ApplicationNumber,
                    Title = viewModel.Title,
                    Surname = viewModel.Surname,
                    OtherNames = viewModel.OtherNames,
                    CollegeProgramId = viewModel.CollegeProgramId,
                    Email = viewModel.Email,
                    PhoneNo = viewModel.PhoneNo,
                    DateOfBirth = viewModel.DateOfBirth,
                    Gender = viewModel.Gender,
                    EnrolmentYear = viewModel.EnrolmentYear,
                    CurrentLevel = viewModel.CurrentLevel,
                    LevelOfEntry = viewModel.EntryLevel,
                    ContactAddress = getStudent.ContactAddress,
                    Religion = getStudent.Religion,
                    ReligiousDenom = getStudent.ReligiousDenom,
                    DisabilityStatus = getStudent.DisabilityStatus,
                    District = getStudent.District,
                    HomeTown = getStudent.HomeTown,
                    PlaceOfBirth = getStudent.PlaceOfBirth,
                    OutstandingFees = getStudent.OutstandingFees,
                    MaritalStatus = getStudent.MaritalStatus,
                    GhanaianLanguagesSpoken = getStudent.GhanaianLanguagesSpoken,
                };*/
                // Update the properties of the existing entity
                student.ApplicationNumber = viewModel.ApplicationNumber;
                student.StudentID=viewModel.StudentId;
                student.Title = viewModel.Title;
                student.Surname = viewModel.Surname;
                student.OtherNames = viewModel.OtherNames;
                student.CollegeProgramId = viewModel.CollegeProgramId;
                student.Email = viewModel.Email;
                student.PhoneNo = viewModel.PhoneNo;
                student.DateOfBirth = viewModel.DateOfBirth;
                student.Gender = viewModel.Gender;
                student.EnrolmentYear = viewModel.EnrolmentYear;
                student.CurrentLevel = viewModel.CurrentLevel;
                student.LevelOfEntry = viewModel.EntryLevel;
                
                
                await _studentService.UpdateStudentAsync(student);
                TempData["SuccessMessage"] = "Student updated successfully";
                return RedirectToAction(nameof(Index));
            }
           
            // Repopulate dropdown if validation fails
            viewModel.CollegePrograms = await GetCollegeProgramsAsync();
            return View(viewModel);
        }

    // GET: Student/Delete/5
    [Authorize(Roles = "System Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Student not found";
        }
        var viewModel = new StudentViewModel
        {
            Id = student.Id,
            ApplicationNumber = student.ApplicationNumber,
            StudentId = student.StudentID,
            Title = student.Title,
            Surname = student.Surname,
            OtherNames = student.OtherNames,
            Email = student.Email,
            PhoneNo = student.PhoneNo,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            CollegeProgramId = student.CollegeProgramId,
            EnrolmentYear = student.EnrolmentYear,
            EntryLevel = student.LevelOfEntry,
            CurrentLevel = student.CurrentLevel,
            CollegePrograms = await GetCollegeProgramsAsync()
        };

        return View(viewModel);
    }

    // POST: Student/Delete/5
    [Authorize(Roles = "System Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _studentService.DeleteStudentAsync(id);
        TempData["SuccessMessage"] = "Student deleted";
        return RedirectToAction(nameof(Index));
    }
    
    //Parent and Guardians
     // GET: Student/ParentGuardians/5
     [Authorize(Roles = "System Admin, Registrar")]
        public async Task<IActionResult> ParentGuardians(Guid studentId)
        {
            var parents = await _studentService.GetParentsByStudentIdAsync(studentId);
            ViewBag.StudentId = studentId;
            return View(parents);
        }

        // GET: Student/AddParentGuardian/5
        [Authorize(Roles = "System Admin, Registrar")]
        public IActionResult AddParentGuardian(Guid studentId)
        {
            var viewModel = new ParentGuardianViewModel
            {
                StudentId = studentId
            };
            return View(viewModel);
        }

        // POST: Student/AddParentGuardian/5
        [Authorize(Roles = "System Admin, Registrar")]
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
                return RedirectToAction(nameof(ParentGuardians), new { studentId =viewModel.StudentId});
            }
            TempData["ErrorMessage"] = "Parent Guardians not added";
            ViewBag.StudentId = viewModel.StudentId;
            return RedirectToAction("ParentGuardians", new { studentId = viewModel.StudentId });
        }

        // GET: Student/EditParentGuardian/5
        [Authorize(Roles = "System Admin, Registrar")]
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

        // POST: Student/EditParentGuardian/5
        [Authorize(Roles = "System Admin, Registrar")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParentGuardian(Guid id, ParentGuardianViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
               TempData["ErrorMessage"] = "ParentGuardian not found";
               return RedirectToAction(nameof(ParentGuardians), new { studentId = viewModel.StudentId });
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
                return RedirectToAction(nameof(ParentGuardians), new { studentId = parentGuardian.StudentId });
            }
            return View(viewModel);
        }

        // GET: Student/DeleteParentGuardian/5
        [Authorize(Roles = "System Admin, Registrar")]
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

        // POST: Student/DeleteParentGuardian/5
        [Authorize(Roles = "System Admin, Registrar")]
        [HttpPost, ActionName("DeleteParentGuardian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParentGuardianConfirmed(Guid id)
        {
            var parentGuardian = await _studentService.GetParentGuardianByIdAsync(id);
            if (parentGuardian != null)
            {
                await _studentService.DeleteParentGuardianAsync(id);
                TempData["SuccessMessage"] = "Parent Guardian deleted";
                return RedirectToAction(nameof(ParentGuardians), new { studentId = parentGuardian.StudentId });
            }
            TempData["ErrorMessage"] = "Parent Guardian not found";
            return RedirectToAction(nameof(Index));
        }
        
        //FinancialInfo
        [Authorize(Roles = "System Admin, Registrar")]
         public async Task<IActionResult> FinancialInfo(Guid studentId)
        {
            var financialInfo = await _studentService.GetFinancialInfoByStudentIdAsync(studentId);
            ViewBag.StudentId = studentId;
            return View(financialInfo);
        }

        // GET: Student/AddFinancialInfo/5
        [Authorize(Roles = "System Admin, Registrar")]
        public IActionResult AddFinancialInfo(Guid studentId)
        {
            var viewModel = new FinancialInfoViewModel
            {
                StudentId = studentId // Set the StudentId from the route parameter
            };
            return View(viewModel);
        }

        // POST: Student/AddFinancialInfo/5
        [Authorize(Roles = "System Admin, Registrar")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFinancialInfo(FinancialInfoViewModel viewModel)
        {
            var getStudentFinancialInfo = await _studentService.GetFinancialInfoByStudentIdAsync(viewModel.StudentId);
            if (getStudentFinancialInfo.Count>0)
            {
                TempData["ErrorMessage"] = "Financial Info already Created";
                return RedirectToAction(nameof(FinancialInfo), new { studentId = viewModel.StudentId });
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
                return RedirectToAction(nameof(FinancialInfo), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }

        // GET: Student/EditFinancialInfo/5
        [Authorize(Roles = "System Admin, Registrar")]
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
        [Authorize(Roles = "System Admin, Registrar")]
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
                return RedirectToAction(nameof(FinancialInfo), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }
        
        
        // GET: Student/DeleteFinancialInfo/5
        [Authorize(Roles = "System Admin, Registrar")]
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
        [Authorize(Roles = "System Admin, Registrar")]
        [HttpPost, ActionName("DeleteFinancialInfo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFinancialInfoConfirmed(Guid id)
        {
            var financialInfo = await _studentService.GetFinancialInfoByIdAsync(id);
            if (financialInfo != null)
            {
                await _studentService.DeleteFinancialInfoAsync(id);
                TempData["SuccessMessage"] = "Financial Info deleted successfully.";
                return RedirectToAction(nameof(FinancialInfo), new { studentId = financialInfo.StudentId });
            }

            return NotFound();
        }
        
        //Education Histories
        // GET: Student/EducationHistories/5
        [Authorize(Roles = "System Admin, Registrar")]
        public async Task<IActionResult> EducationHistories(Guid studentId)
        {
            var educationHistories = await _studentService.GetEducationHistoriesByStudentIdAsync(studentId);
            ViewBag.StudentId = studentId;
            return View(educationHistories);
        }

        // GET: Student/AddEducationHistory/5
        [Authorize(Roles = "System Admin, Registrar")]
        public IActionResult AddEducationHistory(Guid studentId)
        {
            var viewModel = new EducationHistoryViewModel
            {
                StudentId = studentId // Set the StudentId from the route parameter
            };
            return View(viewModel);
        }

        // POST: Student/AddEducationHistory/5
        [Authorize(Roles = "System Admin, Registrar")]
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
                return RedirectToAction(nameof(EducationHistories), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }

        // GET: Student/EditEducationHistory/5
        [Authorize(Roles = "System Admin, Registrar")]
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
        [Authorize(Roles = "System Admin, Registrar")]
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
                return RedirectToAction(nameof(EducationHistories), new { studentId = viewModel.StudentId });
            }

            return View(viewModel);
        }

        // GET: Student/DeleteEducationHistory/5
        [Authorize(Roles = "System Admin, Registrar")]
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
        [Authorize(Roles = "System Admin, Registrar")]
        [HttpPost, ActionName("DeleteEducationHistory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducationHistoryConfirmed(Guid id)
        {
            var educationHistory = await _studentService.GetEducationHistoryByIdAsync(id);
            if (educationHistory != null)
            {
                await _studentService.DeleteEducationHistoryAsync(id);
                TempData["SuccessMessage"]="Education history has been deleted";
                return RedirectToAction(nameof(EducationHistories), new { studentId = educationHistory.StudentId });
            } 
            TempData["ErrorMessage"]=$"Unable to delete education history. Please try again.";
           return RedirectToAction(nameof(EducationHistories), new { studentId = educationHistory.StudentId });
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
        
        // GET: StudentDashboard/UploadStudents
        [Authorize(Roles = "System Admin")]
        public IActionResult UploadStudents()
        {
            return View();
        }
        
        [HttpPost]
        [Authorize(Roles = "System Admin")]
        public async Task<IActionResult> UploadStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Please select a file." });
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Only Excel files (.xlsx) are allowed." });
            }

            using (var stream = file.OpenReadStream())
            {
                var (successCount, errorCount, errors) = await _studentService.UploadStudentsFromExcelAsync(stream);

                return Json(new
                {
                    success = true,
                    successCount,
                    errorCount,
                    errors
                });
            }
        }
        
        [Authorize(Roles = "System Admin, Registrar")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusModel model)
        {
            try
            {
                var student = await _context.Students.FindAsync(model.studentId);
                if (student == null)
                {
                    return NotFound("Student not found");
                }

                student.Status = model.status;
                student.StatusChangeDate = DateTime.Now;

                switch (model.status)
                {
                    case "Transferred":
                        student.TransferInstitution = model.transferInstitution;
                        student.StatusReason = model.reason;
                        student.StatusChangeDate=model.statusDate;
                        break;
                    case "Deferred":
                        student.StatusReason = model.reason;
                        student.StatusChangeDate=model.statusDate;
                        break;
                    case "Dropped Out":
                        student.StatusReason = model.reason;
                        student.StatusChangeDate=model.statusDate;
                        break;
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
}