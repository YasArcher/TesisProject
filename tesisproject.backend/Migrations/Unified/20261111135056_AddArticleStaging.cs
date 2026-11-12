using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Unified
{
    /// <inheritdoc />
    public partial class AddArticleStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleStagingBatch",
                schema: "dbo",
                columns: table => new
                {
                    ArticleStagingBatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationMatrixId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ReadyRows = table.Column<int>(type: "int", nullable: false),
                    DuplicateRows = table.Column<int>(type: "int", nullable: false),
                    InvalidRows = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleStagingBatch", x => x.ArticleStagingBatchId);
                    table.ForeignKey(
                        name: "FK_ArticleStagingBatch_RegistrationMatrix_RegistrationMatrixId",
                        column: x => x.RegistrationMatrixId,
                        principalSchema: "dbo",
                        principalTable: "RegistrationMatrix",
                        principalColumn: "RegistrationMatrixId");
                });

            migrationBuilder.CreateTable(
                name: "ArticleStagingRow",
                schema: "dbo",
                columns: table => new
                {
                    ArticleStagingRowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleStagingBatchId = table.Column<int>(type: "int", nullable: false),
                    RegistrationMatrixRowId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DoiNormalized = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleStagingRow", x => x.ArticleStagingRowId);
                    table.ForeignKey(
                        name: "FK_ArticleStagingRow_ArticleStagingBatch_ArticleStagingBatchId",
                        column: x => x.ArticleStagingBatchId,
                        principalSchema: "dbo",
                        principalTable: "ArticleStagingBatch",
                        principalColumn: "ArticleStagingBatchId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleStagingBatch_CreatedByUserId_CreatedAt",
                schema: "dbo",
                table: "ArticleStagingBatch",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleStagingBatch_RegistrationMatrixId",
                schema: "dbo",
                table: "ArticleStagingBatch",
                column: "RegistrationMatrixId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleStagingRow_ArticleStagingBatchId_RegistrationMatrixRowId",
                schema: "dbo",
                table: "ArticleStagingRow",
                columns: new[] { "ArticleStagingBatchId", "RegistrationMatrixRowId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleStagingRow_ArticleStagingBatchId_RowNumber",
                schema: "dbo",
                table: "ArticleStagingRow",
                columns: new[] { "ArticleStagingBatchId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleStagingRow_DoiNormalized",
                schema: "dbo",
                table: "ArticleStagingRow",
                column: "DoiNormalized",
                filter: "[DoiNormalized] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleStagingRow",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ArticleStagingBatch",
                schema: "dbo");
        }
    }
}
