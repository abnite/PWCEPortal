using System.Reflection;

namespace PWCEPortal.ApplicationClass;

/// <summary>
/// Defines all fine-grained permission constants for the PWCE Portal.
/// Permissions are organised by functional area and assigned to roles as claims.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "Permission";

    // ── Module A: Course-Lecturer Assignment ─────────────────────────────────
    public static class CourseLecturer
    {
        public const string View   = "Permissions.CourseLecturer.View";
        public const string Assign = "Permissions.CourseLecturer.Assign";
        public const string Remove = "Permissions.CourseLecturer.Remove";
    }

    // ── Module B: Assessment Structure Configuration ──────────────────────────
    public static class AssessmentStructure
    {
        public const string View      = "Permissions.AssessmentStructure.View";
        public const string Configure = "Permissions.AssessmentStructure.Configure";
    }

    // ── Module C: Marks Entry ────────────────────────────────────────────────
    public static class MarksEntry
    {
        public const string ViewOwn        = "Permissions.MarksEntry.ViewOwn";
        public const string EnterMarks     = "Permissions.MarksEntry.EnterMarks";
        public const string UploadBulk     = "Permissions.MarksEntry.UploadBulk";
        public const string SubmitForReview = "Permissions.MarksEntry.SubmitForReview";
        public const string ViewAllCourses = "Permissions.MarksEntry.ViewAllCourses";
    }

    // ── Module D: Assessment Approval Workflow ───────────────────────────────
    public static class AssessmentApproval
    {
        public const string ViewPending      = "Permissions.AssessmentApproval.ViewPending";
        public const string HODReview        = "Permissions.AssessmentApproval.HODReview";
        public const string HODApprove       = "Permissions.AssessmentApproval.HODApprove";
        public const string HODReject        = "Permissions.AssessmentApproval.HODReject";
        public const string QAReview         = "Permissions.AssessmentApproval.QAReview";
        public const string QAApprove        = "Permissions.AssessmentApproval.QAApprove";
        public const string QAFlag           = "Permissions.AssessmentApproval.QAFlag";
        public const string PrincipalApprove = "Permissions.AssessmentApproval.PrincipalApprove";
        public const string UnlockAssessment = "Permissions.AssessmentApproval.UnlockAssessment";
    }

    // ── Module E: GPA & Grading ──────────────────────────────────────────────
    public static class GradingScale
    {
        public const string View      = "Permissions.GradingScale.View";
        public const string Configure = "Permissions.GradingScale.Configure";
    }

    // ── Module F: Student Results ────────────────────────────────────────────
    public static class StudentResults
    {
        public const string ViewOwn         = "Permissions.StudentResults.ViewOwn";
        public const string ViewAll         = "Permissions.StudentResults.ViewAll";
        public const string ViewByDepartment = "Permissions.StudentResults.ViewByDepartment";
        public const string Publish         = "Permissions.StudentResults.Publish";
        public const string Withhold        = "Permissions.StudentResults.Withhold";
    }

    // ── Module G: Transcripts ────────────────────────────────────────────────
    public static class Transcripts
    {
        public const string GenerateOwn      = "Permissions.Transcripts.GenerateOwn";
        public const string GenerateOfficial = "Permissions.Transcripts.GenerateOfficial";
        public const string ViewLog          = "Permissions.Transcripts.ViewLog";
    }

    // ── Module H: Teaching Staff Appraisal ──────────────────────────────────
    public static class TeachingAppraisal
    {
        public const string ViewOwn        = "Permissions.TeachingAppraisal.ViewOwn";
        public const string ConductAsHOD   = "Permissions.TeachingAppraisal.ConductAsHOD";
        public const string ViewDepartment = "Permissions.TeachingAppraisal.ViewDepartment";
        public const string ViewAll        = "Permissions.TeachingAppraisal.ViewAll";
        public const string ConfigureTemplate = "Permissions.TeachingAppraisal.ConfigureTemplate";
    }

    // ── Module I: Non-Teaching Staff ─────────────────────────────────────────
    public static class NonTeachingStaff
    {
        public const string View      = "Permissions.NonTeachingStaff.View";
        public const string Manage    = "Permissions.NonTeachingStaff.Manage";
        public const string Appraise  = "Permissions.NonTeachingStaff.Appraise";
        public const string ViewReports = "Permissions.NonTeachingStaff.ViewReports";
        public const string ConfigureTemplate = "Permissions.NonTeachingStaff.ConfigureTemplate";
    }

    // ── Reports & Dashboards ─────────────────────────────────────────────────
    public static class Reports
    {
        public const string ViewStudentMetrics   = "Permissions.Reports.ViewStudentMetrics";
        public const string ViewFinanceReports   = "Permissions.Reports.ViewFinanceReports";
        public const string ViewAcademicReports  = "Permissions.Reports.ViewAcademicReports";
        public const string ViewQAReports        = "Permissions.Reports.ViewQAReports";
        public const string ViewExecutiveDashboard = "Permissions.Reports.ViewExecutiveDashboard";
    }

    // ── User & System Administration ─────────────────────────────────────────
    public static class UserManagement
    {
        public const string ViewUsers    = "Permissions.UserManagement.ViewUsers";
        public const string CreateUser   = "Permissions.UserManagement.CreateUser";
        public const string EditUser     = "Permissions.UserManagement.EditUser";
        public const string DeleteUser   = "Permissions.UserManagement.DeleteUser";
        public const string ResetPassword = "Permissions.UserManagement.ResetPassword";
    }

    public static class RoleManagement
    {
        public const string ViewRoles    = "Permissions.RoleManagement.ViewRoles";
        public const string ManagePermissions = "Permissions.RoleManagement.ManagePermissions";
    }

    public static class AuditLogs
    {
        public const string View = "Permissions.AuditLogs.View";
    }

    // ── Default permission sets per role ─────────────────────────────────────

    public static IReadOnlyList<string> ForRole(string roleName) => roleName switch
    {
        RoleNames.Lecturer => LecturerPermissions,
        RoleNames.HOD => HODPermissions,
        RoleNames.QAOfficer => QAOfficerPermissions,
        RoleNames.AcademicAffairsOfficer => AcademicAffairsPermissions,
        RoleNames.Principal => PrincipalPermissions,
        RoleNames.VicePrincipal => PrincipalPermissions,
        RoleNames.Student => StudentPermissions,
        RoleNames.HROfficer => HROfficerPermissions,
        RoleNames.SystemAdmin => SystemAdminPermissions,
        RoleNames.FinanceOfficer => FinanceOfficerPermissions,
        RoleNames.Registrar => RegistrarPermissions,
        RoleNames.StudentRecordsOfficer => StudentRecordsPermissions,
        _ => Array.Empty<string>()
    };

    public static readonly IReadOnlyList<string> LecturerPermissions = new[]
    {
        CourseLecturer.View,
        MarksEntry.ViewOwn,
        MarksEntry.EnterMarks,
        MarksEntry.UploadBulk,
        MarksEntry.SubmitForReview,
        AssessmentApproval.ViewPending,
        TeachingAppraisal.ViewOwn,
    };

    public static readonly IReadOnlyList<string> HODPermissions = new[]
    {
        CourseLecturer.View,
        CourseLecturer.Assign,
        CourseLecturer.Remove,
        MarksEntry.ViewAllCourses,
        AssessmentApproval.ViewPending,
        AssessmentApproval.HODReview,
        AssessmentApproval.HODApprove,
        AssessmentApproval.HODReject,
        TeachingAppraisal.ConductAsHOD,
        TeachingAppraisal.ViewDepartment,
        Reports.ViewAcademicReports,
        StudentResults.ViewByDepartment,
    };

    public static readonly IReadOnlyList<string> QAOfficerPermissions = new[]
    {
        AssessmentApproval.ViewPending,
        AssessmentApproval.QAReview,
        AssessmentApproval.QAApprove,
        AssessmentApproval.QAFlag,
        Reports.ViewQAReports,
        Reports.ViewAcademicReports,
        StudentResults.ViewAll,
    };

    public static readonly IReadOnlyList<string> AcademicAffairsPermissions = new[]
    {
        AssessmentStructure.View,
        AssessmentStructure.Configure,
        GradingScale.View,
        GradingScale.Configure,
        StudentResults.ViewAll,
        StudentResults.Publish,
        StudentResults.Withhold,
        Transcripts.GenerateOfficial,
        Transcripts.ViewLog,
        Reports.ViewAcademicReports,
        Reports.ViewStudentMetrics,
    };

    public static readonly IReadOnlyList<string> PrincipalPermissions = new[]
    {
        AssessmentApproval.ViewPending,
        AssessmentApproval.PrincipalApprove,
        AssessmentApproval.UnlockAssessment,
        StudentResults.ViewAll,
        Transcripts.ViewLog,
        TeachingAppraisal.ViewAll,
        NonTeachingStaff.ViewReports,
        Reports.ViewAcademicReports,
        Reports.ViewExecutiveDashboard,
        Reports.ViewStudentMetrics,
        Reports.ViewFinanceReports,
        Reports.ViewQAReports,
    };

    public static readonly IReadOnlyList<string> StudentPermissions = new[]
    {
        StudentResults.ViewOwn,
        Transcripts.GenerateOwn,
    };

    public static readonly IReadOnlyList<string> HROfficerPermissions = new[]
    {
        NonTeachingStaff.View,
        NonTeachingStaff.Manage,
        NonTeachingStaff.Appraise,
        NonTeachingStaff.ViewReports,
        NonTeachingStaff.ConfigureTemplate,
        TeachingAppraisal.ConfigureTemplate,
        Reports.ViewStudentMetrics,
    };

    public static readonly IReadOnlyList<string> SystemAdminPermissions = GetAll();

    public static readonly IReadOnlyList<string> FinanceOfficerPermissions = new[]
    {
        Reports.ViewFinanceReports,
        Reports.ViewStudentMetrics,
    };

    public static readonly IReadOnlyList<string> RegistrarPermissions = new[]
    {
        Reports.ViewStudentMetrics,
        Transcripts.GenerateOfficial,
        Transcripts.ViewLog,
    };

    public static readonly IReadOnlyList<string> StudentRecordsPermissions = new[]
    {
        Reports.ViewStudentMetrics,
    };

    // ── Utility: enumerate every permission constant ──────────────────────────

    public static IReadOnlyList<string> GetAll()
    {
        return typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>Returns a human-readable display name for a permission constant.</summary>
    public static string DisplayName(string permission)
    {
        var parts = permission.Split('.');
        return parts.Length >= 3 ? $"{parts[^2]}: {SplitCamelCase(parts[^1])}" : permission;
    }

    private static string SplitCamelCase(string input) =>
        System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1").Trim();
}

/// <summary>Centralised role name constants used throughout the application.</summary>
public static class RoleNames
{
    // Phase 1 roles
    public const string SystemAdmin          = "System Admin";
    public const string Student              = "Student";
    public const string Administrator        = "Administrator";
    public const string FinanceOfficer       = "Finance Officer";
    public const string Secretary            = "Secretary";
    public const string Registrar            = "Registrar";
    public const string StudentRecordsOfficer = "Student Records Officer";

    // Phase 2 roles
    public const string Lecturer             = "Lecturer";
    public const string HOD                  = "Head of Department";
    public const string QAOfficer            = "QA Officer";
    public const string AcademicAffairsOfficer = "Academic Affairs Officer";
    public const string Principal            = "Principal";
    public const string VicePrincipal        = "Vice Principal";
    public const string HROfficer            = "HR Officer";
}
