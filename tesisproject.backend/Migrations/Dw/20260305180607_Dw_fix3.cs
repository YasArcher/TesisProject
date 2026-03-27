using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Dw
{
    /// <inheritdoc />
    public partial class Dw_fix3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "DW");

            migrationBuilder.CreateTable(
                name: "DimDates",
                schema: "DW",
                columns: table => new
                {
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    PeriodName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimDates", x => x.DateKey);
                });

            migrationBuilder.CreateTable(
                name: "DimFaculties",
                schema: "DW",
                columns: table => new
                {
                    FacultyKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    FacultyCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FacultyName = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimFaculties", x => x.FacultyKey);
                });

            migrationBuilder.CreateTable(
                name: "DimFundingTypes",
                schema: "DW",
                columns: table => new
                {
                    FundingTypeKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundingTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimFundingTypes", x => x.FundingTypeKey);
                });

            migrationBuilder.CreateTable(
                name: "DimIndexingDatabases",
                schema: "DW",
                columns: table => new
                {
                    IndexingDatabaseKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimIndexingDatabases", x => x.IndexingDatabaseKey);
                });

            migrationBuilder.CreateTable(
                name: "DimProductTypes",
                schema: "DW",
                columns: table => new
                {
                    ProductTypeKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimProductTypes", x => x.ProductTypeKey);
                });

            migrationBuilder.CreateTable(
                name: "DimProjectStates",
                schema: "DW",
                columns: table => new
                {
                    ProjectStateKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectStateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimProjectStates", x => x.ProjectStateKey);
                });

            migrationBuilder.CreateTable(
                name: "DimQuartiles",
                schema: "DW",
                columns: table => new
                {
                    QuartileKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimQuartiles", x => x.QuartileKey);
                });

            migrationBuilder.CreateTable(
                name: "DimResearchCategories",
                schema: "DW",
                columns: table => new
                {
                    ResearchCategoryKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchCategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResearchCategoryTypeId = table.Column<int>(type: "int", nullable: false),
                    CategoryTypeName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResearchCategoryGroupId = table.Column<int>(type: "int", nullable: false),
                    ResearchCategoryGroupName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParentCategoryKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimResearchCategories", x => x.ResearchCategoryKey);
                });

            migrationBuilder.CreateTable(
                name: "FactBudgets",
                schema: "DW",
                columns: table => new
                {
                    FactBudgetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    InitialAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CertifiedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExecutedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacultyKey = table.Column<int>(type: "int", nullable: false),
                    FundingTypeKey = table.Column<int>(type: "int", nullable: false),
                    ApprovedDateKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactBudgets", x => x.FactBudgetId);
                    table.ForeignKey(
                        name: "FK_FactBudgets_DimDates_ApprovedDateKey",
                        column: x => x.ApprovedDateKey,
                        principalSchema: "DW",
                        principalTable: "DimDates",
                        principalColumn: "DateKey");
                    table.ForeignKey(
                        name: "FK_FactBudgets_DimFaculties_FacultyKey",
                        column: x => x.FacultyKey,
                        principalSchema: "DW",
                        principalTable: "DimFaculties",
                        principalColumn: "FacultyKey");
                    table.ForeignKey(
                        name: "FK_FactBudgets_DimFundingTypes_FundingTypeKey",
                        column: x => x.FundingTypeKey,
                        principalSchema: "DW",
                        principalTable: "DimFundingTypes",
                        principalColumn: "FundingTypeKey");
                });

            migrationBuilder.CreateTable(
                name: "FactProjects",
                schema: "DW",
                columns: table => new
                {
                    FactProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectCount = table.Column<int>(type: "int", nullable: false),
                    DurationInMonths = table.Column<int>(type: "int", nullable: false),
                    ExecutionPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacultyKey = table.Column<int>(type: "int", nullable: false),
                    ProjectStateKey = table.Column<int>(type: "int", nullable: false),
                    ApprovalDateKey = table.Column<int>(type: "int", nullable: false),
                    StartDateKey = table.Column<int>(type: "int", nullable: false),
                    EndDateKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactProjects", x => x.FactProjectId);
                    table.UniqueConstraint("AK_FactProjects_ProjectId", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_FactProjects_DimDates_ApprovalDateKey",
                        column: x => x.ApprovalDateKey,
                        principalSchema: "DW",
                        principalTable: "DimDates",
                        principalColumn: "DateKey");
                    table.ForeignKey(
                        name: "FK_FactProjects_DimDates_EndDateKey",
                        column: x => x.EndDateKey,
                        principalSchema: "DW",
                        principalTable: "DimDates",
                        principalColumn: "DateKey");
                    table.ForeignKey(
                        name: "FK_FactProjects_DimDates_StartDateKey",
                        column: x => x.StartDateKey,
                        principalSchema: "DW",
                        principalTable: "DimDates",
                        principalColumn: "DateKey");
                    table.ForeignKey(
                        name: "FK_FactProjects_DimFaculties_FacultyKey",
                        column: x => x.FacultyKey,
                        principalSchema: "DW",
                        principalTable: "DimFaculties",
                        principalColumn: "FacultyKey");
                    table.ForeignKey(
                        name: "FK_FactProjects_DimProjectStates_ProjectStateKey",
                        column: x => x.ProjectStateKey,
                        principalSchema: "DW",
                        principalTable: "DimProjectStates",
                        principalColumn: "ProjectStateKey");
                });

            migrationBuilder.CreateTable(
                name: "FactProducts",
                schema: "DW",
                columns: table => new
                {
                    FactProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProductCount = table.Column<int>(type: "int", nullable: false),
                    IsActiveFlag = table.Column<bool>(type: "bit", nullable: false),
                    FacultyKey = table.Column<int>(type: "int", nullable: false),
                    ProductTypeKey = table.Column<int>(type: "int", nullable: false),
                    CreatedDateKey = table.Column<int>(type: "int", nullable: false),
                    IndexingDatabaseKey = table.Column<int>(type: "int", nullable: true),
                    QuartileKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactProducts", x => x.FactProductId);
                    table.ForeignKey(
                        name: "FK_FactProducts_DimDates_CreatedDateKey",
                        column: x => x.CreatedDateKey,
                        principalSchema: "DW",
                        principalTable: "DimDates",
                        principalColumn: "DateKey");
                    table.ForeignKey(
                        name: "FK_FactProducts_DimFaculties_FacultyKey",
                        column: x => x.FacultyKey,
                        principalSchema: "DW",
                        principalTable: "DimFaculties",
                        principalColumn: "FacultyKey");
                    table.ForeignKey(
                        name: "FK_FactProducts_DimIndexingDatabases_IndexingDatabaseKey",
                        column: x => x.IndexingDatabaseKey,
                        principalSchema: "DW",
                        principalTable: "DimIndexingDatabases",
                        principalColumn: "IndexingDatabaseKey");
                    table.ForeignKey(
                        name: "FK_FactProducts_DimProductTypes_ProductTypeKey",
                        column: x => x.ProductTypeKey,
                        principalSchema: "DW",
                        principalTable: "DimProductTypes",
                        principalColumn: "ProductTypeKey");
                    table.ForeignKey(
                        name: "FK_FactProducts_DimQuartiles_QuartileKey",
                        column: x => x.QuartileKey,
                        principalSchema: "DW",
                        principalTable: "DimQuartiles",
                        principalColumn: "QuartileKey");
                });

            migrationBuilder.CreateTable(
                name: "BridgeProjectResearchCategories",
                schema: "DW",
                columns: table => new
                {
                    BridgeProjectResearchCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ResearchCategoryKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BridgeProjectResearchCategories", x => x.BridgeProjectResearchCategoryId);
                    table.ForeignKey(
                        name: "FK_BridgeProjectResearchCategories_DimResearchCategories_ResearchCategoryKey",
                        column: x => x.ResearchCategoryKey,
                        principalSchema: "DW",
                        principalTable: "DimResearchCategories",
                        principalColumn: "ResearchCategoryKey");
                    table.ForeignKey(
                        name: "FK_BridgeProjectResearchCategories_FactProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "DW",
                        principalTable: "FactProjects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BridgeProjectResearchCategories_ProjectId",
                schema: "DW",
                table: "BridgeProjectResearchCategories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BridgeProjectResearchCategories_ProjectId_ResearchCategoryKey",
                schema: "DW",
                table: "BridgeProjectResearchCategories",
                columns: new[] { "ProjectId", "ResearchCategoryKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BridgeProjectResearchCategories_ResearchCategoryKey",
                schema: "DW",
                table: "BridgeProjectResearchCategories",
                column: "ResearchCategoryKey");

            migrationBuilder.CreateIndex(
                name: "IX_DimDates_Date",
                schema: "DW",
                table: "DimDates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DimDates_Year_Month",
                schema: "DW",
                table: "DimDates",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_DimFaculties_FacultyCode",
                schema: "DW",
                table: "DimFaculties",
                column: "FacultyCode");

            migrationBuilder.CreateIndex(
                name: "IX_DimFaculties_FacultyId",
                schema: "DW",
                table: "DimFaculties",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_DimFaculties_FacultyName",
                schema: "DW",
                table: "DimFaculties",
                column: "FacultyName");

            migrationBuilder.CreateIndex(
                name: "IX_DimFundingTypes_FundingTypeId",
                schema: "DW",
                table: "DimFundingTypes",
                column: "FundingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DimFundingTypes_Name",
                schema: "DW",
                table: "DimFundingTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DimIndexingDatabases_Name",
                schema: "DW",
                table: "DimIndexingDatabases",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DimProductTypes_Name",
                schema: "DW",
                table: "DimProductTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DimProductTypes_ProductTypeId",
                schema: "DW",
                table: "DimProductTypes",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DimProjectStates_Name",
                schema: "DW",
                table: "DimProjectStates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DimProjectStates_ProjectStateId",
                schema: "DW",
                table: "DimProjectStates",
                column: "ProjectStateId");

            migrationBuilder.CreateIndex(
                name: "IX_DimQuartiles_Code",
                schema: "DW",
                table: "DimQuartiles",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchCategories_CategoryTypeName",
                schema: "DW",
                table: "DimResearchCategories",
                column: "CategoryTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchCategories_Name",
                schema: "DW",
                table: "DimResearchCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchCategories_ParentCategoryKey",
                schema: "DW",
                table: "DimResearchCategories",
                column: "ParentCategoryKey");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchCategories_ResearchCategoryGroupId",
                schema: "DW",
                table: "DimResearchCategories",
                column: "ResearchCategoryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchCategories_ResearchCategoryGroupName",
                schema: "DW",
                table: "DimResearchCategories",
                column: "ResearchCategoryGroupName");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchCategories_ResearchCategoryId",
                schema: "DW",
                table: "DimResearchCategories",
                column: "ResearchCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchCategories_ResearchCategoryTypeId",
                schema: "DW",
                table: "DimResearchCategories",
                column: "ResearchCategoryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FactBudgets_ApprovedDateKey",
                schema: "DW",
                table: "FactBudgets",
                column: "ApprovedDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactBudgets_BudgetId",
                schema: "DW",
                table: "FactBudgets",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_FactBudgets_FacultyKey",
                schema: "DW",
                table: "FactBudgets",
                column: "FacultyKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactBudgets_FundingTypeKey",
                schema: "DW",
                table: "FactBudgets",
                column: "FundingTypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactBudgets_ProjectId",
                schema: "DW",
                table: "FactBudgets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_CreatedDateKey",
                schema: "DW",
                table: "FactProducts",
                column: "CreatedDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_FacultyKey",
                schema: "DW",
                table: "FactProducts",
                column: "FacultyKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_IndexingDatabaseKey",
                schema: "DW",
                table: "FactProducts",
                column: "IndexingDatabaseKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_IsActiveFlag",
                schema: "DW",
                table: "FactProducts",
                column: "IsActiveFlag");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_ProductId",
                schema: "DW",
                table: "FactProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_ProductTypeKey",
                schema: "DW",
                table: "FactProducts",
                column: "ProductTypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_ProjectId",
                schema: "DW",
                table: "FactProducts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_QuartileKey",
                schema: "DW",
                table: "FactProducts",
                column: "QuartileKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProjects_ApprovalDateKey",
                schema: "DW",
                table: "FactProjects",
                column: "ApprovalDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProjects_EndDateKey",
                schema: "DW",
                table: "FactProjects",
                column: "EndDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProjects_FacultyKey",
                schema: "DW",
                table: "FactProjects",
                column: "FacultyKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProjects_ProjectId",
                schema: "DW",
                table: "FactProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FactProjects_ProjectStateKey",
                schema: "DW",
                table: "FactProjects",
                column: "ProjectStateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProjects_StartDateKey",
                schema: "DW",
                table: "FactProjects",
                column: "StartDateKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BridgeProjectResearchCategories",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "FactBudgets",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "FactProducts",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimResearchCategories",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "FactProjects",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimFundingTypes",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimIndexingDatabases",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimProductTypes",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimQuartiles",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimDates",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimFaculties",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimProjectStates",
                schema: "DW");
        }
    }
}
