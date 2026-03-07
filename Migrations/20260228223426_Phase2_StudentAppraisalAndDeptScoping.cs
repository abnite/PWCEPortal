using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class Phase2StudentAppraisalAndDeptScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_AddedById",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_DeletedById",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_AddedById",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DeletedById",
                table: "Departments");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "NonTeachingStaffMembers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeletedById",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AddedById",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddedById1",
                table: "Departments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedById1",
                table: "Departments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeachingAppraisalSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppraisalTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseLecturerAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByHODId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingAppraisalSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalSessions_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalSessions_AppraisalTemplates_AppraisalTemplateId",
                        column: x => x.AppraisalTemplateId,
                        principalTable: "AppraisalTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalSessions_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalSessions_AspNetUsers_CreatedByHODId",
                        column: x => x.CreatedByHODId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalSessions_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeachingAppraisalSessions_CourseLecturerAssignments_CourseLecturerAssignmentId",
                        column: x => x.CourseLecturerAssignmentId,
                        principalTable: "CourseLecturerAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "StudentAppraisalSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAppraisalSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAppraisalSubmissions_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAppraisalSubmissions_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAppraisalSubmissions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAppraisalSubmissions_TeachingAppraisalSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TeachingAppraisalSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAppraisalScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppraisalCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAppraisalScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAppraisalScores_AppraisalCriteria_AppraisalCriterionId",
                        column: x => x.AppraisalCriterionId,
                        principalTable: "AppraisalCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAppraisalScores_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAppraisalScores_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAppraisalScores_StudentAppraisalSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "StudentAppraisalSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NonTeachingStaffMembers_DepartmentId",
                table: "NonTeachingStaffMembers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_AddedById1",
                table: "Departments",
                column: "AddedById1");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DeletedById1",
                table: "Departments",
                column: "DeletedById1");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalScores_AddedById",
                table: "StudentAppraisalScores",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalScores_AppraisalCriterionId",
                table: "StudentAppraisalScores",
                column: "AppraisalCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalScores_DeletedById",
                table: "StudentAppraisalScores",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalScores_SubmissionId",
                table: "StudentAppraisalScores",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalSubmissions_AddedById",
                table: "StudentAppraisalSubmissions",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalSubmissions_DeletedById",
                table: "StudentAppraisalSubmissions",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalSubmissions_SessionId",
                table: "StudentAppraisalSubmissions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppraisalSubmissions_StudentId",
                table: "StudentAppraisalSubmissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalSessions_AcademicYearId",
                table: "TeachingAppraisalSessions",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalSessions_AddedById",
                table: "TeachingAppraisalSessions",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalSessions_AppraisalTemplateId",
                table: "TeachingAppraisalSessions",
                column: "AppraisalTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalSessions_CourseLecturerAssignmentId",
                table: "TeachingAppraisalSessions",
                column: "CourseLecturerAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalSessions_CreatedByHODId",
                table: "TeachingAppraisalSessions",
                column: "CreatedByHODId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingAppraisalSessions_DeletedById",
                table: "TeachingAppraisalSessions",
                column: "DeletedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_AddedById1",
                table: "Departments",
                column: "AddedById1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_DeletedById1",
                table: "Departments",
                column: "DeletedById1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NonTeachingStaffMembers_Departments_DepartmentId",
                table: "NonTeachingStaffMembers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_AddedById1",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_DeletedById1",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_NonTeachingStaffMembers_Departments_DepartmentId",
                table: "NonTeachingStaffMembers");

            migrationBuilder.DropTable(
                name: "StudentAppraisalScores");

            migrationBuilder.DropTable(
                name: "StudentAppraisalSubmissions");

            migrationBuilder.DropTable(
                name: "TeachingAppraisalSessions");

            migrationBuilder.DropIndex(
                name: "IX_NonTeachingStaffMembers_DepartmentId",
                table: "NonTeachingStaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_Departments_AddedById1",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DeletedById1",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "NonTeachingStaffMembers");

            migrationBuilder.DropColumn(
                name: "AddedById1",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeletedById1",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "DeletedById",
                table: "Departments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AddedById",
                table: "Departments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_AddedById",
                table: "Departments",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DeletedById",
                table: "Departments",
                column: "DeletedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_AddedById",
                table: "Departments",
                column: "AddedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_DeletedById",
                table: "Departments",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
