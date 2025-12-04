using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Visits_VisitId1",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_VisitId1",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VisitId1",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VisitId1",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_VisitId1",
                table: "Products",
                column: "VisitId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Visits_VisitId1",
                table: "Products",
                column: "VisitId1",
                principalTable: "Visits",
                principalColumn: "VisitId");
        }
    }
}
