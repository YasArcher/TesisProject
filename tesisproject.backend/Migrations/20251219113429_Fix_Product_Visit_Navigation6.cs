using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Documents_RelatedDocumentId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_RelatedDocumentId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RelatedDocumentId",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "UX_Documents_ResolutionCode",
                table: "Documents",
                column: "ResolutionCode",
                unique: true,
                filter: "[ResolutionCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Documents_ResolutionCode",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "RelatedDocumentId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RelatedDocumentId",
                table: "Documents",
                column: "RelatedDocumentId",
                unique: true,
                filter: "[RelatedDocumentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Documents_RelatedDocumentId",
                table: "Documents",
                column: "RelatedDocumentId",
                principalTable: "Documents",
                principalColumn: "DocumentId");
        }
    }
}
