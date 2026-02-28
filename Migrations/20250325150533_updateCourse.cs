using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class updateCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseCode",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseCode",
                table: "Courses");
        }
    }
}
