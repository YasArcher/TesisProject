using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectFacultyHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectFacultyHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFacultyHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFacultyHistories_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_ProjectFacultyHistories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFacultyHistories_CreatedAtUtc",
                table: "ProjectFacultyHistories",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFacultyHistories_CreatedByUserId",
                table: "ProjectFacultyHistories",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFacultyHistories_ProjectId",
                table: "ProjectFacultyHistories",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectFacultyHistories");
        }
    }
}
