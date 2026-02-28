using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWCEPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddClassHall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "collegeClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    collegeProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collegeClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_collegeClasses_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_collegeClasses_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_collegeClasses_CollegePrograms_collegeProgramId",
                        column: x => x.collegeProgramId,
                        principalTable: "CollegePrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollegeHalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HallName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollegeHalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollegeHalls_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CollegeHalls_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_collegeClasses_AddedById",
                table: "collegeClasses",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_collegeClasses_collegeProgramId",
                table: "collegeClasses",
                column: "collegeProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_collegeClasses_DeletedById",
                table: "collegeClasses",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CollegeHalls_AddedById",
                table: "CollegeHalls",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_CollegeHalls_DeletedById",
                table: "CollegeHalls",
                column: "DeletedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collegeClasses");

            migrationBuilder.DropTable(
                name: "CollegeHalls");
        }
    }
}
