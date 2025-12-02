using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDescription_amount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "BudgetTransactions",
                newName: "CertifiedAmount");

            migrationBuilder.AddColumn<string>(
                name: "CertificationDescription",
                table: "BudgetTransactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExecutedAmount",
                table: "BudgetTransactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionDescription",
                table: "BudgetTransactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "BudgetTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificationDescription",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "ExecutedAmount",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "ExecutionDescription",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "BudgetTransactions");

            migrationBuilder.RenameColumn(
                name: "CertifiedAmount",
                table: "BudgetTransactions",
                newName: "Amount");
        }
    }
}
