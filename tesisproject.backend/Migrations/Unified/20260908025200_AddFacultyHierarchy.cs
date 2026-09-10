using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Unified
{
    /// <inheritdoc />
    public partial class AddFacultyHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentFacultyId",
                schema: "dbo",
                table: "Faculties",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_ParentFacultyId",
                schema: "dbo",
                table: "Faculties",
                column: "ParentFacultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faculties_Faculties_ParentFacultyId",
                schema: "dbo",
                table: "Faculties",
                column: "ParentFacultyId",
                principalSchema: "dbo",
                principalTable: "Faculties",
                principalColumn: "FacultyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faculties_Faculties_ParentFacultyId",
                schema: "dbo",
                table: "Faculties");

            migrationBuilder.DropIndex(
                name: "IX_Faculties_ParentFacultyId",
                schema: "dbo",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "ParentFacultyId",
                schema: "dbo",
                table: "Faculties");
        }
    }
}
