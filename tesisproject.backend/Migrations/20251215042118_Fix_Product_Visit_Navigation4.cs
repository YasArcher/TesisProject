using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectOriginTypeId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProjectOriginTypes",
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
                    table.PrimaryKey("PK_ProjectOriginTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectOriginTypeId",
                table: "Projects",
                column: "ProjectOriginTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectOriginTypes_ProjectOriginTypeId",
                table: "Projects",
                column: "ProjectOriginTypeId",
                principalTable: "ProjectOriginTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectOriginTypes_ProjectOriginTypeId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectOriginTypes");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectOriginTypeId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectOriginTypeId",
                table: "Projects");
        }
    }
}
