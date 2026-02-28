using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.Models;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.Payment;
using PWCEPortal.Models.Staff;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Data;

public class PortalDbContext:IdentityDbContext<ApplicationUser,ApplicationRole,string>
{
    private readonly IHttpContextAccessor accessor;
    public UserManager<ApplicationUser> userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PortalDbContext(IHttpContextAccessor httpContextAccessor,DbContextOptions<PortalDbContext> o, IHttpContextAccessor accessor):base(o)
    {
        this.accessor = accessor;
        _httpContextAccessor = httpContextAccessor;
        
    }
    
    // Student Information
    public DbSet<Student> Students { get; set; }
    public DbSet<ParentGuardian> ParentGuardians { get; set; }
    public DbSet<EducationHistory> EducationHistories { get; set; }
    public DbSet<FinancialInfo> FinancialInfos { get; set; }
    public DbSet<CollegeHall> CollegeHalls { get; set; }
    public DbSet<CollegeClass> collegeClasses { get; set; }

    // Academic Management
    public DbSet<AcademicYear> AcademicYears { get; set; }
    public DbSet<CollegeProgram> CollegePrograms { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<StudentCourseRegistration> StudentCourseRegistrations { get; set; }
    public DbSet<AcademicSemester> AcademicSemesters { get; set; }

    // Payment Management
    public DbSet<FeeStructure> FeeStructures { get; set; }
    public DbSet<PartPaymentConfig> PartPaymentConfigs { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<StudentFeeAssignment> StudentFeeAssignments { get; set; }
    
    public DbSet<LegacyOutstandingFee> LegacyOutstandingFees { get; set; }
    
    public DbSet<RequiredFee> RequiredFees { get; set; }

    // Phase 2 – Course-Lecturer Assignment (Module A)
    public DbSet<Department> Departments { get; set; }
    public DbSet<Lecturer> Lecturers { get; set; }
    public DbSet<CourseLecturerAssignment> CourseLecturerAssignments { get; set; }

    // Phase 2 – Assessment Structure (Module B)
    public DbSet<AssessmentStructure> AssessmentStructures { get; set; }
    public DbSet<AssessmentComponent> AssessmentComponents { get; set; }
    public DbSet<GradingScale> GradingScales { get; set; }
    public DbSet<GradeDefinition> GradeDefinitions { get; set; }

    // Phase 2 – Marks Entry (Module C)
    public DbSet<StudentMark> StudentMarks { get; set; }

    // Phase 2 – Assessment Approval Workflow (Module D)
    public DbSet<AssessmentSubmission> AssessmentSubmissions { get; set; }
    public DbSet<AssessmentApprovalLog> AssessmentApprovalLogs { get; set; }

    // Phase 2 – Results (Module E & F)
    public DbSet<SemesterResult> SemesterResults { get; set; }
    public DbSet<CumulativeResult> CumulativeResults { get; set; }

    // Phase 2 – Transcripts (Module G)
    public DbSet<TranscriptRequest> TranscriptRequests { get; set; }

    // Phase 2 – Teaching Appraisals (Module H)
    public DbSet<AppraisalTemplate> AppraisalTemplates { get; set; }
    public DbSet<AppraisalCriterion> AppraisalCriteria { get; set; }
    public DbSet<TeachingAppraisal> TeachingAppraisals { get; set; }
    public DbSet<TeachingAppraisalScore> TeachingAppraisalScores { get; set; }

    // Phase 2 – Non-Teaching Staff (Module I)
    public DbSet<NonTeachingStaff> NonTeachingStaffMembers { get; set; }
    public DbSet<NonTeachingAppraisal> NonTeachingAppraisals { get; set; }
    public DbSet<NonTeachingAppraisalScore> NonTeachingAppraisalScores { get; set; }

    // System Configuration & Logs
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<DeletedRecordsLog> DeletedRecordsLogs { get; set; }
    
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            LogAuditEntries();
            return await base.SaveChangesAsync(cancellationToken);
        }

    private void LogAuditEntries()
    {
        // Retrieve current user and IP address from HTTP context
        string currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        string ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        // Get entries that are Added, Modified, or Deleted
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added
                        || e.State == EntityState.Modified
                        || e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            // Skip logging for AuditLog entries to avoid recursion.
            if (entry.Entity is AuditLog)
                continue;

            string action = entry.State.ToString(); // "Added", "Modified", or "Deleted"
            string entityName = entry.Entity.GetType().Name;

            var oldValues = new Dictionary<string, object>();
            var newValues = new Dictionary<string, object>();

            if (entry.State == EntityState.Modified)
            {
                foreach (var property in entry.OriginalValues.Properties)
                {
                    var originalValue = entry.OriginalValues[property]?.ToString();
                    var currentValue = entry.CurrentValues[property]?.ToString();
                    if (originalValue != currentValue)
                    {
                        oldValues[property.Name] = originalValue;
                        newValues[property.Name] = currentValue;
                    }
                }
            }
            else if (entry.State == EntityState.Added)
            {
                foreach (var property in entry.CurrentValues.Properties)
                {
                    newValues[property.Name] = entry.CurrentValues[property];
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var property in entry.OriginalValues.Properties)
                {
                    oldValues[property.Name] = entry.OriginalValues[property];
                }
            }
            // Serialize dictionaries to JSON; use empty string if no values.
            string serializedOldValues = oldValues.Any() ? JsonSerializer.Serialize(oldValues) : "";
            string serializedNewValues = newValues.Any() ? JsonSerializer.Serialize(newValues) : "";

            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                ActionPerformed = $"{action} on {entityName}",
                IPAddress = ipAddress,
                DateAdded = DateTime.UtcNow,
                OldValues = serializedOldValues,
                NewValues = serializedNewValues
            };

            AuditLogs.Add(auditLog);
        }
    }


}