using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Unified
{
    /// <inheritdoc />
    public partial class AddArticleReadView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dynamic batches also work inside EF's idempotent migration guards.
            migrationBuilder.Sql("EXEC(N'" + ArticleReadViewSql.TransferCanonicalValues.Replace("'", "''") + "');");
            migrationBuilder.DropIndex(
                name: "UX_Articles_Doi_NotBlank",
                schema: "dbo",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Doi",
                schema: "dbo",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PublicationUrl",
                schema: "dbo",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Year",
                schema: "dbo",
                table: "Articles");
            migrationBuilder.Sql("EXEC(N'" + ArticleReadViewSql.CreateView.Replace("'", "''") + "');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Doi",
                schema: "dbo",
                table: "Articles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicationUrl",
                schema: "dbo",
                table: "Articles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Year",
                schema: "dbo",
                table: "Articles",
                type: "smallint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE a SET Doi=v.Doi, [Year]=v.[Year], PublicationUrl=v.PublicationUrl
                FROM dbo.Articles a JOIN dbo.ArticleReadView v ON v.ArticleId=a.Id;
                DROP VIEW dbo.ArticleReadView;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_Articles_Doi_NotBlank",
                schema: "dbo",
                table: "Articles",
                column: "Doi",
                unique: true,
                filter: "[Doi] IS NOT NULL AND [Doi] <> N''");
        }
    }
}
