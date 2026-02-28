using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class updateStudentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollegeClassId",
                table: "Students",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollegeHallId",
                table: "Students",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeClassId",
                table: "Students",
                column: "CollegeClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeHallId",
                table: "Students",
                column: "CollegeHallId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_CollegeHalls_CollegeHallId",
                table: "Students",
                column: "CollegeHallId",
                principalTable: "CollegeHalls",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_collegeClasses_CollegeClassId",
                table: "Students",
                column: "CollegeClassId",
                principalTable: "collegeClasses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_CollegeHalls_CollegeHallId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_collegeClasses_CollegeClassId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_CollegeClassId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_CollegeHallId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CollegeClassId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CollegeHallId",
                table: "Students");
        }
    }
}
