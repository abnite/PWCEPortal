using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class updatePayment3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerifiedById",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_VerifiedById",
                table: "Payments",
                column: "VerifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_VerifiedById",
                table: "Payments",
                column: "VerifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_VerifiedById",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_VerifiedById",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "Payments");
        }
    }
}
