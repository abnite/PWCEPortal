using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCourseCreationAddIsCommon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommon",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommon",
                table: "Courses");
        }
    }
}
