using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "ObjectiveActivities");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "ObjectiveActivities",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "ObjectiveActivities");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "ObjectiveActivities",
                type: "bit",
                maxLength: 1000,
                nullable: false,
                defaultValue: false);
        }
    }
}
