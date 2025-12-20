using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentTypeId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "UX_Documents_ResolutionCode",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "UX_Documents_Type1_ResolutionCode",
                table: "Documents",
                columns: new[] { "DocumentTypeId", "ResolutionCode" },
                unique: true,
                filter: "[ResolutionCode] IS NOT NULL AND [DocumentTypeId] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Documents_Type1_ResolutionCode",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentTypeId",
                table: "Documents",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_Documents_ResolutionCode",
                table: "Documents",
                column: "ResolutionCode",
                unique: true,
                filter: "[ResolutionCode] IS NOT NULL");
        }
    }
}
