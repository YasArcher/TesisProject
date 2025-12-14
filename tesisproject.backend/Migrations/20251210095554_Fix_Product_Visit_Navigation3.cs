using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Product_Visit_Navigation3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefaultHeader = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceEntity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourcePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportTemplateColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    ExportFieldId = table.Column<int>(type: "int", nullable: false),
                    TargetHeader = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Separator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportTemplateColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportTemplateColumns_ExportFields_ExportFieldId",
                        column: x => x.ExportFieldId,
                        principalTable: "ExportFields",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportTemplateColumns_ExportTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ExportTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplateColumns_ExportFieldId",
                table: "ExportTemplateColumns",
                column: "ExportFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplateColumns_TemplateId",
                table: "ExportTemplateColumns",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportTemplateColumns");

            migrationBuilder.DropTable(
                name: "ExportFields");

            migrationBuilder.DropTable(
                name: "ExportTemplates");
        }
    }
}
