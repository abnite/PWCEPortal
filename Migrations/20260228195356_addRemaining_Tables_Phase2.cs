using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class addRemainingTablesPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppraisalTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppraisalTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppraisalTemplates_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppraisalTemplates_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentStructures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollegeProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicableLevel = table.Column<int>(type: "int", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentStructures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentStructures_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentStructures_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentStructures_CollegePrograms_CollegeProgramId",
                        column: x => x.CollegeProgramId,
                        principalTable: "CollegePrograms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CumulativeResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CGPA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCreditHoursEarned = table.Column<int>(type: "int", nullable: false),
                    TotalCreditHoursAttempted = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CumulativeResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CumulativeResults_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CumulativeResults_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CumulativeResults_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CumulativeResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Departments_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GradingScales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScaleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingScales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradingScales_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GradingScales_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NonTeachingStaffMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateEmployed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qualification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonTeachingStaffMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonTeachingStaffMembers_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonTeachingStaffMembers_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonTeachingStaffMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SemesterResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicSemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalWeightedScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GPA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCreditHours = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsWithheld = table.Column<bool>(type: "bit", nullable: false),
                    WithholdReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemesterResults_AcademicSemesters_AcademicSemesterId",
                        column: x => x.AcademicSemesterId,
                        principalTable: "AcademicSemesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SemesterResults_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SemesterResults_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SemesterResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TranscriptType = table.Column<int>(type: "int", nullable: false),
                    RequestedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptRequests_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TranscriptRequests_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TranscriptRequests_AspNetUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TranscriptRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppraisalCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppraisalTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppraisalCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppraisalCriteria_AppraisalTemplates_AppraisalTemplateId",
                        column: x => x.AppraisalTemplateId,
                        principalTable: "AppraisalTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppraisalCriteria_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppraisalCriteria_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentComponents_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentComponents_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentComponents_AssessmentStructures_AssessmentStructureId",
                        column: x => x.AssessmentStructureId,
                        principalTable: "AssessmentStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lecturers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StaffId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Specialisation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lecturers_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Lecturers_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Lecturers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lecturers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GradeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradingScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeLetter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GradePoint = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeDefinitions_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GradeDefinitions_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GradeDefinitions_GradingScales_GradingScaleId",
                        column: x => x.GradingScaleId,
                        principalTable: "GradingScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NonTeachingAppraisals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppraisalTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NonTeachingStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConductedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxPossibleScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OverallRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonTeachingAppraisals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisals_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisals_AppraisalTemplates_AppraisalTemplateId",
                        column: x => x.AppraisalTemplateId,
                        principalTable: "AppraisalTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisals_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisals_AspNetUsers_ConductedById",
                        column: x => x.ConductedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisals_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisals_NonTeachingStaffMembers_NonTeachingStaffId",
                        column: x => x.NonTeachingStaffId,
                        principalTable: "NonTeachingStaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseLecturerAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LecturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicSemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseLecturerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseLecturerAssignments_AcademicSemesters_AcademicSemesterId",
                        column: x => x.AcademicSemesterId,
                        principalTable: "AcademicSemesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseLecturerAssignments_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseLecturerAssignments_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseLecturerAssignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseLecturerAssignments_Lecturers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Lecturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeachingAppraisals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppraisalTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LecturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConductedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxPossibleScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OverallRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingAppraisals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisals_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisals_AppraisalTemplates_AppraisalTemplateId",
                        column: x => x.AppraisalTemplateId,
                        principalTable: "AppraisalTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisals_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeachingAppraisals_AspNetUsers_ConductedById",
                        column: x => x.ConductedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisals_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeachingAppraisals_Lecturers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Lecturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "NonTeachingAppraisalScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NonTeachingAppraisalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppraisalCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonTeachingAppraisalScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisalScores_AppraisalCriteria_AppraisalCriterionId",
                        column: x => x.AppraisalCriterionId,
                        principalTable: "AppraisalCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisalScores_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisalScores_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonTeachingAppraisalScores_NonTeachingAppraisals_NonTeachingAppraisalId",
                        column: x => x.NonTeachingAppraisalId,
                        principalTable: "NonTeachingAppraisals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseLecturerAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HODReviewerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    HODReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HODComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QAReviewerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    QAReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QAComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrincipalReviewerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PrincipalReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrincipalComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentSubmissions_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentSubmissions_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentSubmissions_AspNetUsers_HODReviewerId",
                        column: x => x.HODReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentSubmissions_AspNetUsers_PrincipalReviewerId",
                        column: x => x.PrincipalReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentSubmissions_AspNetUsers_QAReviewerId",
                        column: x => x.QAReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentSubmissions_CourseLecturerAssignments_CourseLecturerAssignmentId",
                        column: x => x.CourseLecturerAssignmentId,
                        principalTable: "CourseLecturerAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentMarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseLecturerAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMarks_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentMarks_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentMarks_AssessmentComponents_AssessmentComponentId",
                        column: x => x.AssessmentComponentId,
                        principalTable: "AssessmentComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentMarks_CourseLecturerAssignments_CourseLecturerAssignmentId",
                        column: x => x.CourseLecturerAssignmentId,
                        principalTable: "CourseLecturerAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeachingAppraisalScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingAppraisalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppraisalCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingAppraisalScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalScores_AppraisalCriteria_AppraisalCriterionId",
                        column: x => x.AppraisalCriterionId,
                        principalTable: "AppraisalCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalScores_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalScores_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalScores_TeachingAppraisals_TeachingAppraisalId",
                        column: x => x.TeachingAppraisalId,
                        principalTable: "TeachingAppraisals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentApprovalLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentApprovalLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentApprovalLogs_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentApprovalLogs_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentApprovalLogs_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssessmentApprovalLogs_AssessmentSubmissions_AssessmentSubmissionId",
                        column: x => x.AssessmentSubmissionId,
                        principalTable: "AssessmentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalCriteria_AddedById",
                table: "AppraisalCriteria",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalCriteria_AppraisalTemplateId",
                table: "AppraisalCriteria",
                column: "AppraisalTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalCriteria_DeletedById",
                table: "AppraisalCriteria",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalTemplates_AddedById",
                table: "AppraisalTemplates",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalTemplates_DeletedById",
                table: "AppraisalTemplates",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentApprovalLogs_ActorId",
                table: "AssessmentApprovalLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentApprovalLogs_AddedById",
                table: "AssessmentApprovalLogs",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentApprovalLogs_AssessmentSubmissionId",
                table: "AssessmentApprovalLogs",
                column: "AssessmentSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentApprovalLogs_DeletedById",
                table: "AssessmentApprovalLogs",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComponents_AddedById",
                table: "AssessmentComponents",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComponents_AssessmentStructureId",
                table: "AssessmentComponents",
                column: "AssessmentStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComponents_DeletedById",
                table: "AssessmentComponents",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentStructures_AddedById",
                table: "AssessmentStructures",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentStructures_CollegeProgramId",
                table: "AssessmentStructures",
                column: "CollegeProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentStructures_DeletedById",
                table: "AssessmentStructures",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_AddedById",
                table: "AssessmentSubmissions",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_CourseLecturerAssignmentId",
                table: "AssessmentSubmissions",
                column: "CourseLecturerAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_DeletedById",
                table: "AssessmentSubmissions",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_HODReviewerId",
                table: "AssessmentSubmissions",
                column: "HODReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_PrincipalReviewerId",
                table: "AssessmentSubmissions",
                column: "PrincipalReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_QAReviewerId",
                table: "AssessmentSubmissions",
                column: "QAReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLecturerAssignments_AcademicSemesterId",
                table: "CourseLecturerAssignments",
                column: "AcademicSemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLecturerAssignments_AddedById",
                table: "CourseLecturerAssignments",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLecturerAssignments_CourseId",
                table: "CourseLecturerAssignments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLecturerAssignments_DeletedById",
                table: "CourseLecturerAssignments",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLecturerAssignments_LecturerId",
                table: "CourseLecturerAssignments",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_CumulativeResults_AcademicYearId",
                table: "CumulativeResults",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_CumulativeResults_AddedById",
                table: "CumulativeResults",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_CumulativeResults_DeletedById",
                table: "CumulativeResults",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CumulativeResults_StudentId",
                table: "CumulativeResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_AddedById",
                table: "Departments",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DeletedById",
                table: "Departments",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDefinitions_AddedById",
                table: "GradeDefinitions",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDefinitions_DeletedById",
                table: "GradeDefinitions",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDefinitions_GradingScaleId",
                table: "GradeDefinitions",
                column: "GradingScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_GradingScales_AddedById",
                table: "GradingScales",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_GradingScales_DeletedById",
                table: "GradingScales",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_AddedById",
                table: "Lecturers",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_DeletedById",
                table: "Lecturers",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_DepartmentId",
                table: "Lecturers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_UserId",
                table: "Lecturers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisals_AcademicYearId",
                table: "NonTeachingAppraisals",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisals_AddedById",
                table: "NonTeachingAppraisals",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisals_AppraisalTemplateId",
                table: "NonTeachingAppraisals",
                column: "AppraisalTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisals_ConductedById",
                table: "NonTeachingAppraisals",
                column: "ConductedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisals_DeletedById",
                table: "NonTeachingAppraisals",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisals_NonTeachingStaffId",
                table: "NonTeachingAppraisals",
                column: "NonTeachingStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisalScores_AddedById",
                table: "NonTeachingAppraisalScores",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisalScores_AppraisalCriterionId",
                table: "NonTeachingAppraisalScores",
                column: "AppraisalCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisalScores_DeletedById",
                table: "NonTeachingAppraisalScores",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingAppraisalScores_NonTeachingAppraisalId",
                table: "NonTeachingAppraisalScores",
                column: "NonTeachingAppraisalId");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingStaffMembers_AddedById",
                table: "NonTeachingStaffMembers",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingStaffMembers_DeletedById",
                table: "NonTeachingStaffMembers",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingStaffMembers_UserId",
                table: "NonTeachingStaffMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResults_AcademicSemesterId",
                table: "SemesterResults",
                column: "AcademicSemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResults_AddedById",
                table: "SemesterResults",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResults_DeletedById",
                table: "SemesterResults",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResults_StudentId",
                table: "SemesterResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_AddedById",
                table: "StudentMarks",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_AssessmentComponentId",
                table: "StudentMarks",
                column: "AssessmentComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_CourseLecturerAssignmentId",
                table: "StudentMarks",
                column: "CourseLecturerAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_DeletedById",
                table: "StudentMarks",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_StudentId",
                table: "StudentMarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisals_AcademicYearId",
                table: "TeachingAppraisals",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisals_AddedById",
                table: "TeachingAppraisals",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisals_AppraisalTemplateId",
                table: "TeachingAppraisals",
                column: "AppraisalTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisals_ConductedById",
                table: "TeachingAppraisals",
                column: "ConductedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisals_DeletedById",
                table: "TeachingAppraisals",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisals_LecturerId",
                table: "TeachingAppraisals",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalScores_AddedById",
                table: "TeachingAppraisalScores",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalScores_AppraisalCriterionId",
                table: "TeachingAppraisalScores",
                column: "AppraisalCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalScores_DeletedById",
                table: "TeachingAppraisalScores",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalScores_TeachingAppraisalId",
                table: "TeachingAppraisalScores",
                column: "TeachingAppraisalId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptRequests_AddedById",
                table: "TranscriptRequests",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptRequests_DeletedById",
                table: "TranscriptRequests",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptRequests_RequestedById",
                table: "TranscriptRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptRequests_StudentId",
                table: "TranscriptRequests",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentApprovalLogs");

            migrationBuilder.DropTable(
                name: "CumulativeResults");

            migrationBuilder.DropTable(
                name: "GradeDefinitions");

            migrationBuilder.DropTable(
                name: "NonTeachingAppraisalScores");

            migrationBuilder.DropTable(
                name: "SemesterResults");

            migrationBuilder.DropTable(
                name: "StudentMarks");

            migrationBuilder.DropTable(
                name: "TeachingAppraisalScores");

            migrationBuilder.DropTable(
                name: "TranscriptRequests");

            migrationBuilder.DropTable(
                name: "AssessmentSubmissions");

            migrationBuilder.DropTable(
                name: "GradingScales");

            migrationBuilder.DropTable(
                name: "NonTeachingAppraisals");

            migrationBuilder.DropTable(
                name: "AssessmentComponents");

            migrationBuilder.DropTable(
                name: "AppraisalCriteria");

            migrationBuilder.DropTable(
                name: "TeachingAppraisals");

            migrationBuilder.DropTable(
                name: "CourseLecturerAssignments");

            migrationBuilder.DropTable(
                name: "NonTeachingStaffMembers");

            migrationBuilder.DropTable(
                name: "AssessmentStructures");

            migrationBuilder.DropTable(
                name: "AppraisalTemplates");

            migrationBuilder.DropTable(
                name: "Lecturers");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
