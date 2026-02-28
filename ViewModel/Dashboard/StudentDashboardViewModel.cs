using PWCEPortal.Models.Academic;
using PWCEPortal.Models.Payment;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.ViewModel.Dashboard;

public class StudentDashboardViewModel
{
        // Student Information
        public Models.StudentInfo.Student Student { get; set; }
        public CollegeProgram Program { get; set; }
        public string? PassportPicturePath { get; set; }

        // Academic Information
        public AcademicYear CurrentAcademicYear { get; set; }
        public List<Course> RegisteredCourses { get; set; }
        public List<Course> AvailableCourses { get; set; }
        public AcademicSemester CurrentSemester { get; set; }

        // Payment Information
        public FeeStructure FeeStructure { get; set; }
        public List<Models.Payment.Payment> PaymentHistory { get; set; }
        public decimal TotalFee { get; set; }
        public decimal UnVerifiedPaymentMade { get; set; }
        public decimal PaymentMade { get; set; }
        public decimal OutstandingFee { get; set; }
        public decimal RequiredFeeAmount { get; set; }

        // Profile Information
        public List<ParentGuardian> ParentsGuardians { get; set; }
        public List<FinancialInfo> FinancialInfos { get; set; }
        public List<EducationHistory> EducationHistories { get; set; }

        // Notifications
        public List<string> Notifications { get; set; }
    
}