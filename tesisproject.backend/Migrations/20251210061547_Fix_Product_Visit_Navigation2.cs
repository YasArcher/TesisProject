using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectScopes");

            migrationBuilder.DropTable(
                name: "ScopeTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScopeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectScopes",
                columns: table => new
                {
                    ProjectScopeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ScopeTypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectScopes", x => x.ProjectScopeId);
                    table.ForeignKey(
                        name: "FK_ProjectScopes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_ProjectScopes_ScopeTypes_ScopeTypeId",
                        column: x => x.ScopeTypeId,
                        principalTable: "ScopeTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScopes_ProjectId",
                table: "ProjectScopes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScopes_ScopeTypeId",
                table: "ProjectScopes",
                column: "ScopeTypeId");
        }
    }
}
