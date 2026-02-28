using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class updateAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "StudentCourseRegistrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseRegistrations_SemesterId",
                table: "StudentCourseRegistrations",
                column: "SemesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourseRegistrations_AcademicSemesters_SemesterId",
                table: "StudentCourseRegistrations",
                column: "SemesterId",
                principalTable: "AcademicSemesters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourseRegistrations_AcademicSemesters_SemesterId",
                table: "StudentCourseRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_StudentCourseRegistrations_SemesterId",
                table: "StudentCourseRegistrations");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "StudentCourseRegistrations");

            migrationBuilder.DropColumn(
                name: "NewValues",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OldValues",
                table: "AuditLogs");
        }
    }
}
