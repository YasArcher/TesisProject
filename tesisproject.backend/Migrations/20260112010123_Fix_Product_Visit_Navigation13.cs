using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "ObjectiveActivities");

            migrationBuilder.CreateTable(
                name: "VisitObjectiveActivityProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    ObjectiveActivityId = table.Column<int>(type: "int", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitObjectiveActivityProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitObjectiveActivityProgresses_ObjectiveActivities_ObjectiveActivityId",
                        column: x => x.ObjectiveActivityId,
                        principalTable: "ObjectiveActivities",
                        principalColumn: "ObjectiveActivityId");
                    table.ForeignKey(
                        name: "FK_VisitObjectiveActivityProgresses_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitObjectiveActivityProgresses_ObjectiveActivityId",
                table: "VisitObjectiveActivityProgresses",
                column: "ObjectiveActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitObjectiveActivityProgresses_VisitId",
                table: "VisitObjectiveActivityProgresses",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitObjectiveActivityProgresses_VisitId_ObjectiveActivityId",
                table: "VisitObjectiveActivityProgresses",
                columns: new[] { "VisitId", "ObjectiveActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitObjectiveActivityProgresses");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "ObjectiveActivities",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
