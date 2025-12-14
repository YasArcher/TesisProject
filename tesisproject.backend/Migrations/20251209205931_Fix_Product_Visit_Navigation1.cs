using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDocuments_Documents_DocumentId1",
                table: "ProjectDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDocuments_Projects_ProjectId1",
                table: "ProjectDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_DocumentId1",
                table: "ProjectDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_ProjectId1",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentId1",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ProjectId1",
                table: "ProjectDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId_DocumentId",
                table: "ProjectDocuments",
                columns: new[] { "ProjectId", "DocumentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_ProjectId_DocumentId",
                table: "ProjectDocuments");

            migrationBuilder.AddColumn<int>(
                name: "DocumentId1",
                table: "ProjectDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId1",
                table: "ProjectDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_DocumentId1",
                table: "ProjectDocuments",
                column: "DocumentId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId1",
                table: "ProjectDocuments",
                column: "ProjectId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDocuments_Documents_DocumentId1",
                table: "ProjectDocuments",
                column: "DocumentId1",
                principalTable: "Documents",
                principalColumn: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDocuments_Projects_ProjectId1",
                table: "ProjectDocuments",
                column: "ProjectId1",
                principalTable: "Projects",
                principalColumn: "ProjectId");
        }
    }
}
