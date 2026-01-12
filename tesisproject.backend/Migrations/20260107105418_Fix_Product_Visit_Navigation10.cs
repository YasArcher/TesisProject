using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectExtensionTypeId",
                table: "ProjectExtensions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectExtensionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExtensionTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensions_ProjectExtensionTypeId",
                table: "ProjectExtensions",
                column: "ProjectExtensionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensionTypes_Name",
                table: "ProjectExtensionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExtensions_ProjectExtensionTypes_ProjectExtensionTypeId",
                table: "ProjectExtensions",
                column: "ProjectExtensionTypeId",
                principalTable: "ProjectExtensionTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExtensions_ProjectExtensionTypes_ProjectExtensionTypeId",
                table: "ProjectExtensions");

            migrationBuilder.DropTable(
                name: "ProjectExtensionTypes");

            migrationBuilder.DropIndex(
                name: "IX_ProjectExtensions_ProjectExtensionTypeId",
                table: "ProjectExtensions");

            migrationBuilder.DropColumn(
                name: "ProjectExtensionTypeId",
                table: "ProjectExtensions");
        }
    }
}
