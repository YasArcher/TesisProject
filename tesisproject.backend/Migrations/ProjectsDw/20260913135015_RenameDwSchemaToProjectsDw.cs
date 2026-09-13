using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.ProjectsDw
{
    /// <inheritdoc />
    public partial class RenameDwSchemaToProjectsDw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "FactProjects",
                schema: "DW",
                newName: "FactProjects",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "FactProducts",
                schema: "DW",
                newName: "FactProducts",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "FactBudgets",
                schema: "DW",
                newName: "FactBudgets",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimResearchCategories",
                schema: "DW",
                newName: "DimResearchCategories",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimQuartiles",
                schema: "DW",
                newName: "DimQuartiles",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimProjectStates",
                schema: "DW",
                newName: "DimProjectStates",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimProductTypes",
                schema: "DW",
                newName: "DimProductTypes",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimJournals",
                schema: "DW",
                newName: "DimJournals",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimIndexingDatabases",
                schema: "DW",
                newName: "DimIndexingDatabases",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimFundingTypes",
                schema: "DW",
                newName: "DimFundingTypes",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimFaculties",
                schema: "DW",
                newName: "DimFaculties",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimDates",
                schema: "DW",
                newName: "DimDates",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "DimAuthors",
                schema: "DW",
                newName: "DimAuthors",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "BridgeProjectResearchCategories",
                schema: "DW",
                newName: "BridgeProjectResearchCategories",
                newSchema: "ProjectsDW");

            migrationBuilder.RenameTable(
                name: "BridgeProductAuthors",
                schema: "DW",
                newName: "BridgeProductAuthors",
                newSchema: "ProjectsDW");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "DW");

            migrationBuilder.RenameTable(
                name: "FactProjects",
                schema: "ProjectsDW",
                newName: "FactProjects",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "FactProducts",
                schema: "ProjectsDW",
                newName: "FactProducts",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "FactBudgets",
                schema: "ProjectsDW",
                newName: "FactBudgets",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimResearchCategories",
                schema: "ProjectsDW",
                newName: "DimResearchCategories",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimQuartiles",
                schema: "ProjectsDW",
                newName: "DimQuartiles",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimProjectStates",
                schema: "ProjectsDW",
                newName: "DimProjectStates",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimProductTypes",
                schema: "ProjectsDW",
                newName: "DimProductTypes",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimJournals",
                schema: "ProjectsDW",
                newName: "DimJournals",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimIndexingDatabases",
                schema: "ProjectsDW",
                newName: "DimIndexingDatabases",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimFundingTypes",
                schema: "ProjectsDW",
                newName: "DimFundingTypes",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimFaculties",
                schema: "ProjectsDW",
                newName: "DimFaculties",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimDates",
                schema: "ProjectsDW",
                newName: "DimDates",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "DimAuthors",
                schema: "ProjectsDW",
                newName: "DimAuthors",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "BridgeProjectResearchCategories",
                schema: "ProjectsDW",
                newName: "BridgeProjectResearchCategories",
                newSchema: "DW");

            migrationBuilder.RenameTable(
                name: "BridgeProductAuthors",
                schema: "ProjectsDW",
                newName: "BridgeProductAuthors",
                newSchema: "DW");
        }
    }
}
