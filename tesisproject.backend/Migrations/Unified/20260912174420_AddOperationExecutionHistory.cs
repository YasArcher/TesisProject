using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Unified
{
    /// <inheritdoc />
    public partial class AddOperationExecutionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationExecutionHistory",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedByAppUserId = table.Column<int>(type: "int", nullable: true),
                    ExecutedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationExecutionHistory", x => x.Id);
                    table.CheckConstraint("CK_OperationExecutionHistory_OperationType", "[OperationType] IN ('DATA_MIGRATION','BULK_IMPORT','SYNCHRONIZATION','ETL','BACKGROUND_JOB','MANUAL_PROCESS')");
                    table.CheckConstraint("CK_OperationExecutionHistory_ResultJson_IsJson", "[ResultJson] IS NULL OR ISJSON([ResultJson]) = 1");
                    table.CheckConstraint("CK_OperationExecutionHistory_Status", "[Status] IN ('RUNNING','SUCCEEDED','PARTIALLY_SUCCEEDED','FAILED')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationExecutionHistory_ExecutionId",
                schema: "dbo",
                table: "OperationExecutionHistory",
                column: "ExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationExecutionHistory_OperationType_OperationCode",
                schema: "dbo",
                table: "OperationExecutionHistory",
                columns: new[] { "OperationType", "OperationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationExecutionHistory_StartedAt",
                schema: "dbo",
                table: "OperationExecutionHistory",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OperationExecutionHistory_Status",
                schema: "dbo",
                table: "OperationExecutionHistory",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_OperationExecutionHistory_DataMigration_Succeeded",
                schema: "dbo",
                table: "OperationExecutionHistory",
                column: "OperationCode",
                unique: true,
                filter: "[OperationType] = 'DATA_MIGRATION' AND [Status] = 'SUCCEEDED'");

            migrationBuilder.CreateIndex(
                name: "UX_OperationExecutionHistory_ProjectsMatrix_SucceededHash",
                schema: "dbo",
                table: "OperationExecutionHistory",
                column: "FileHash",
                unique: true,
                filter: "[OperationType] = 'BULK_IMPORT' AND [OperationCode] = 'PROJECTS_MATRIX_IMPORT' AND [Status] = 'SUCCEEDED' AND [FileHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationExecutionHistory",
                schema: "dbo");
        }
    }
}
