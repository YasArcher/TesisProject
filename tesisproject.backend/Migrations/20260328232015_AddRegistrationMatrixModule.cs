using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using tesisproject.backend.Data;

#nullable disable

namespace tesisproject.backend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260328232015_AddRegistrationMatrixModule")]
    public partial class AddRegistrationMatrixModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrationMatrix",
                columns: table => new
                {
                    RegistrationMatrixId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastImportBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationMatrix", x => x.RegistrationMatrixId);
                    table.ForeignKey(
                        name: "FK_RegistrationMatrix_ImportBatch_LastImportBatchId",
                        column: x => x.LastImportBatchId,
                        principalTable: "ImportBatch",
                        principalColumn: "ImportBatchId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationMatrixColumn",
                columns: table => new
                {
                    RegistrationMatrixColumnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationMatrixId = table.Column<int>(type: "int", nullable: false),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    WidthUnits = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationMatrixColumn", x => x.RegistrationMatrixColumnId);
                    table.ForeignKey(
                        name: "FK_RegistrationMatrixColumn_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrationMatrixColumn_RegistrationMatrix_RegistrationMatrixId",
                        column: x => x.RegistrationMatrixId,
                        principalTable: "RegistrationMatrix",
                        principalColumn: "RegistrationMatrixId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationMatrixRow",
                columns: table => new
                {
                    RegistrationMatrixRowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationMatrixId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationMatrixRow", x => x.RegistrationMatrixRowId);
                    table.ForeignKey(
                        name: "FK_RegistrationMatrixRow_RegistrationMatrix_RegistrationMatrixId",
                        column: x => x.RegistrationMatrixId,
                        principalTable: "RegistrationMatrix",
                        principalColumn: "RegistrationMatrixId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationMatrixCell",
                columns: table => new
                {
                    RegistrationMatrixCellId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationMatrixRowId = table.Column<int>(type: "int", nullable: false),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationMatrixCell", x => x.RegistrationMatrixCellId);
                    table.ForeignKey(
                        name: "FK_RegistrationMatrixCell_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrationMatrixCell_RegistrationMatrixRow_RegistrationMatrixRowId",
                        column: x => x.RegistrationMatrixRowId,
                        principalTable: "RegistrationMatrixRow",
                        principalColumn: "RegistrationMatrixRowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrix_LastImportBatchId",
                table: "RegistrationMatrix",
                column: "LastImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixCell_FieldId",
                table: "RegistrationMatrixCell",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixCell_RegistrationMatrixRowId_FieldId",
                table: "RegistrationMatrixCell",
                columns: new[] { "RegistrationMatrixRowId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixColumn_FieldId",
                table: "RegistrationMatrixColumn",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixColumn_RegistrationMatrixId_FieldId",
                table: "RegistrationMatrixColumn",
                columns: new[] { "RegistrationMatrixId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixRow_RegistrationMatrixId_RowNumber",
                table: "RegistrationMatrixRow",
                columns: new[] { "RegistrationMatrixId", "RowNumber" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RegistrationMatrixCell");
            migrationBuilder.DropTable(name: "RegistrationMatrixColumn");
            migrationBuilder.DropTable(name: "RegistrationMatrixRow");
            migrationBuilder.DropTable(name: "RegistrationMatrix");
        }
    }
}
