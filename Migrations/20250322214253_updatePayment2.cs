using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class updatePayment2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAcademicYearId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CurrentAcademicYearId",
                table: "Payments",
                column: "CurrentAcademicYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AcademicYears_CurrentAcademicYearId",
                table: "Payments",
                column: "CurrentAcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AcademicYears_CurrentAcademicYearId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CurrentAcademicYearId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CurrentAcademicYearId",
                table: "Payments");
        }
    }
}
