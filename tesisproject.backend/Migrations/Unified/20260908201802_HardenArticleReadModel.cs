using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Unified
{
    /// <inheritdoc />
    public partial class HardenArticleReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeDefinitions_ProductTypeId",
                schema: "dbo",
                table: "ProductAttributeDefinitions");

            migrationBuilder.CreateIndex(
                name: "UX_ProductAttributeDefinitions_Type_Attribute",
                schema: "dbo",
                table: "ProductAttributeDefinitions",
                columns: new[] { "ProductTypeId", "ProductAttributeId" },
                unique: true);

            migrationBuilder.Sql("""
                SET ANSI_NULLS ON;
                SET ANSI_PADDING ON;
                SET ANSI_WARNINGS ON;
                SET ARITHABORT ON;
                SET CONCAT_NULL_YIELDS_NULL ON;
                SET QUOTED_IDENTIFIER ON;
                SET NUMERIC_ROUNDABORT OFF;
                EXEC(N'CREATE VIEW dbo.ArticleDoiUniqueness WITH SCHEMABINDING AS
                    SELECT pv.Id AS ProductValueId,
                        CONVERT(binary(32), HASHBYTES(''SHA2_256'',
                            LOWER(LTRIM(RTRIM(pv.Value)) COLLATE Latin1_General_100_CI_AS))) AS DoiKey
                    FROM dbo.ProductValues AS pv
                    JOIN dbo.ProductAttributeDefinitions AS d ON d.Id = pv.AttributeDefinitionId
                    JOIN dbo.Products AS p ON p.Id = pv.ProductId AND p.ProductTypeId = d.ProductTypeId
                    WHERE p.ProductTypeId >= 1 AND p.ProductTypeId <= 2
                        AND d.ProductAttributeId = 8
                        AND pv.Value IS NOT NULL AND LTRIM(RTRIM(pv.Value)) <> N'''';');
                CREATE UNIQUE CLUSTERED INDEX UX_ArticleDoiUniqueness_DoiKey
                    ON dbo.ArticleDoiUniqueness(DoiKey);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW dbo.ArticleDoiUniqueness;");
            migrationBuilder.DropIndex(
                name: "UX_ProductAttributeDefinitions_Type_Attribute",
                schema: "dbo",
                table: "ProductAttributeDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeDefinitions_ProductTypeId",
                schema: "dbo",
                table: "ProductAttributeDefinitions",
                column: "ProductTypeId");
        }
    }
}
