using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.ProjectsDw
{
    /// <inheritdoc />
    public partial class ExtendProjectsDwReportingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthorCount",
                schema: "DW",
                table: "FactProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Doi",
                schema: "DW",
                table: "FactProducts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssnIsbn",
                schema: "DW",
                table: "FactProducts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JournalKey",
                schema: "DW",
                table: "FactProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicationYear",
                schema: "DW",
                table: "FactProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "DW",
                table: "FactProducts",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DimAuthors",
                schema: "DW",
                columns: table => new
                {
                    AuthorKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    IsInstitutional = table.Column<bool>(type: "bit", nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: true),
                    IdAsp = table.Column<int>(type: "int", nullable: true),
                    ExternalResearcherId = table.Column<int>(type: "int", nullable: true),
                    ExternalFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Orcid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimAuthors", x => x.AuthorKey);
                });

            migrationBuilder.CreateTable(
                name: "DimJournals",
                schema: "DW",
                columns: table => new
                {
                    JournalKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimJournals", x => x.JournalKey);
                    table.CheckConstraint("CK_DimJournals_Name_Trimmed", "[Name] <> N'' AND [Name] = LTRIM(RTRIM([Name]))");
                });

            migrationBuilder.CreateTable(
                name: "BridgeProductAuthors",
                schema: "DW",
                columns: table => new
                {
                    BridgeProductAuthorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAuthorId = table.Column<int>(type: "int", nullable: false),
                    FactProductId = table.Column<int>(type: "int", nullable: false),
                    AuthorKey = table.Column<int>(type: "int", nullable: false),
                    AuthorOrder = table.Column<int>(type: "int", nullable: true),
                    IsPrimaryAuthor = table.Column<bool>(type: "bit", nullable: false),
                    Participation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BridgeProductAuthors", x => x.BridgeProductAuthorId);
                    table.ForeignKey(
                        name: "FK_BridgeProductAuthors_DimAuthors_AuthorKey",
                        column: x => x.AuthorKey,
                        principalSchema: "DW",
                        principalTable: "DimAuthors",
                        principalColumn: "AuthorKey");
                    table.ForeignKey(
                        name: "FK_BridgeProductAuthors_FactProducts_FactProductId",
                        column: x => x.FactProductId,
                        principalSchema: "DW",
                        principalTable: "FactProducts",
                        principalColumn: "FactProductId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_JournalKey",
                schema: "DW",
                table: "FactProducts",
                column: "JournalKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactProducts_PublicationYear",
                schema: "DW",
                table: "FactProducts",
                column: "PublicationYear");

            migrationBuilder.CreateIndex(
                name: "IX_BridgeProductAuthors_AuthorKey",
                schema: "DW",
                table: "BridgeProductAuthors",
                column: "AuthorKey");

            migrationBuilder.CreateIndex(
                name: "IX_BridgeProductAuthors_FactProductId_AuthorKey",
                schema: "DW",
                table: "BridgeProductAuthors",
                columns: new[] { "FactProductId", "AuthorKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BridgeProductAuthors_ProductAuthorId",
                schema: "DW",
                table: "BridgeProductAuthors",
                column: "ProductAuthorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_AppUserId",
                schema: "DW",
                table: "DimAuthors",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_AuthorId",
                schema: "DW",
                table: "DimAuthors",
                column: "AuthorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_ExternalResearcherId",
                schema: "DW",
                table: "DimAuthors",
                column: "ExternalResearcherId");

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_IdAsp",
                schema: "DW",
                table: "DimAuthors",
                column: "IdAsp");

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_IsInstitutional",
                schema: "DW",
                table: "DimAuthors",
                column: "IsInstitutional");

            migrationBuilder.CreateIndex(
                name: "IX_DimJournals_Name",
                schema: "DW",
                table: "DimJournals",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FactProducts_DimJournals_JournalKey",
                schema: "DW",
                table: "FactProducts",
                column: "JournalKey",
                principalSchema: "DW",
                principalTable: "DimJournals",
                principalColumn: "JournalKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FactProducts_DimJournals_JournalKey",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropTable(
                name: "BridgeProductAuthors",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimJournals",
                schema: "DW");

            migrationBuilder.DropTable(
                name: "DimAuthors",
                schema: "DW");

            migrationBuilder.DropIndex(
                name: "IX_FactProducts_JournalKey",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropIndex(
                name: "IX_FactProducts_PublicationYear",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropColumn(
                name: "AuthorCount",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropColumn(
                name: "Doi",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropColumn(
                name: "IssnIsbn",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropColumn(
                name: "JournalKey",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropColumn(
                name: "PublicationYear",
                schema: "DW",
                table: "FactProducts");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "DW",
                table: "FactProducts");
        }
    }
}

