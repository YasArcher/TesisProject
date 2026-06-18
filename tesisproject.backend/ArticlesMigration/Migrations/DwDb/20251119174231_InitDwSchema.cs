using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.DwDb
{
    /// <inheritdoc />
    public partial class InitDwSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DimAcademicTerms",
                columns: table => new
                {
                    AcademicTermKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicTermId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimAcademicTerms", x => x.AcademicTermKey);
                });

            migrationBuilder.CreateTable(
                name: "DimArticles",
                columns: table => new
                {
                    ArticleKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Doi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOpenAccess = table.Column<bool>(type: "bit", nullable: false),
                    IsProjectResult = table.Column<bool>(type: "bit", nullable: false),
                    HasInterculturalComponent = table.Column<bool>(type: "bit", nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    PublicationUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProceedingsName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Filiacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimArticles", x => x.ArticleKey);
                });

            migrationBuilder.CreateTable(
                name: "DimAuthors",
                columns: table => new
                {
                    AuthorKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Identificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Participacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimAuthors", x => x.AuthorKey);
                });

            migrationBuilder.CreateTable(
                name: "DimDates",
                columns: table => new
                {
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Quarter = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    WeekOfYear = table.Column<int>(type: "int", nullable: false),
                    IsWeekend = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimDates", x => x.DateKey);
                });

            migrationBuilder.CreateTable(
                name: "DimFields",
                columns: table => new
                {
                    FieldKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BroadFieldId = table.Column<int>(type: "int", nullable: true),
                    BroadFieldName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecificFieldId = table.Column<int>(type: "int", nullable: true),
                    SpecificFieldName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailedFieldId = table.Column<int>(type: "int", nullable: true),
                    DetailedFieldName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimFields", x => x.FieldKey);
                });

            migrationBuilder.CreateTable(
                name: "DimIndexingSources",
                columns: table => new
                {
                    IndexingSourceKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndexingSourceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimIndexingSources", x => x.IndexingSourceKey);
                });

            migrationBuilder.CreateTable(
                name: "DimProjects",
                columns: table => new
                {
                    ProjectKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimProjects", x => x.ProjectKey);
                });

            migrationBuilder.CreateTable(
                name: "DimPublicationStatuses",
                columns: table => new
                {
                    PublicationStatusKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicationStatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimPublicationStatuses", x => x.PublicationStatusKey);
                });

            migrationBuilder.CreateTable(
                name: "DimResearchLines",
                columns: table => new
                {
                    ResearchLineKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchLineId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimResearchLines", x => x.ResearchLineKey);
                });

            migrationBuilder.CreateTable(
                name: "DimVenues",
                columns: table => new
                {
                    VenueKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssnCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssueNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VolumeNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JournalUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimVenues", x => x.VenueKey);
                });

            migrationBuilder.CreateTable(
                name: "FactArticleAuthors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleKey = table.Column<int>(type: "int", nullable: false),
                    AuthorKey = table.Column<int>(type: "int", nullable: false),
                    AuthorIndex = table.Column<int>(type: "int", nullable: false),
                    TotalAuthors = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactArticleAuthors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactArticleAuthors_DimArticles_ArticleKey",
                        column: x => x.ArticleKey,
                        principalTable: "DimArticles",
                        principalColumn: "ArticleKey",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactArticleAuthors_DimAuthors_AuthorKey",
                        column: x => x.AuthorKey,
                        principalTable: "DimAuthors",
                        principalColumn: "AuthorKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactArticleIndexings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleKey = table.Column<int>(type: "int", nullable: false),
                    IndexingSourceKey = table.Column<int>(type: "int", nullable: false),
                    IndexedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactArticleIndexings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactArticleIndexings_DimArticles_ArticleKey",
                        column: x => x.ArticleKey,
                        principalTable: "DimArticles",
                        principalColumn: "ArticleKey",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactArticleIndexings_DimIndexingSources_IndexingSourceKey",
                        column: x => x.IndexingSourceKey,
                        principalTable: "DimIndexingSources",
                        principalColumn: "IndexingSourceKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactArticlePublications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleKey = table.Column<int>(type: "int", nullable: false),
                    VenueKey = table.Column<int>(type: "int", nullable: true),
                    FieldKey = table.Column<int>(type: "int", nullable: true),
                    ResearchLineKey = table.Column<int>(type: "int", nullable: true),
                    PublicationStatusKey = table.Column<int>(type: "int", nullable: true),
                    ProjectKey = table.Column<int>(type: "int", nullable: true),
                    AcademicTermKey = table.Column<int>(type: "int", nullable: true),
                    CreatedDateKey = table.Column<int>(type: "int", nullable: false),
                    PublicationDateKey = table.Column<int>(type: "int", nullable: true),
                    ArticleCount = table.Column<int>(type: "int", nullable: false),
                    AuthorCount = table.Column<int>(type: "int", nullable: false),
                    IndexingCount = table.Column<int>(type: "int", nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    IsOpenAccess = table.Column<bool>(type: "bit", nullable: false),
                    IsProjectResult = table.Column<bool>(type: "bit", nullable: false),
                    HasInterculturalComponent = table.Column<bool>(type: "bit", nullable: false),
                    SJR = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quartile = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactArticlePublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimAcademicTerms_AcademicTermKey",
                        column: x => x.AcademicTermKey,
                        principalTable: "DimAcademicTerms",
                        principalColumn: "AcademicTermKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimArticles_ArticleKey",
                        column: x => x.ArticleKey,
                        principalTable: "DimArticles",
                        principalColumn: "ArticleKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimDates_CreatedDateKey",
                        column: x => x.CreatedDateKey,
                        principalTable: "DimDates",
                        principalColumn: "DateKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimDates_PublicationDateKey",
                        column: x => x.PublicationDateKey,
                        principalTable: "DimDates",
                        principalColumn: "DateKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimFields_FieldKey",
                        column: x => x.FieldKey,
                        principalTable: "DimFields",
                        principalColumn: "FieldKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimProjects_ProjectKey",
                        column: x => x.ProjectKey,
                        principalTable: "DimProjects",
                        principalColumn: "ProjectKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimPublicationStatuses_PublicationStatusKey",
                        column: x => x.PublicationStatusKey,
                        principalTable: "DimPublicationStatuses",
                        principalColumn: "PublicationStatusKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimResearchLines_ResearchLineKey",
                        column: x => x.ResearchLineKey,
                        principalTable: "DimResearchLines",
                        principalColumn: "ResearchLineKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimVenues_VenueKey",
                        column: x => x.VenueKey,
                        principalTable: "DimVenues",
                        principalColumn: "VenueKey",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FactVenueMetricYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueKey = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    YearDateKey = table.Column<int>(type: "int", nullable: true),
                    SJR = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quartile = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactVenueMetricYears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactVenueMetricYears_DimDates_YearDateKey",
                        column: x => x.YearDateKey,
                        principalTable: "DimDates",
                        principalColumn: "DateKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactVenueMetricYears_DimVenues_VenueKey",
                        column: x => x.VenueKey,
                        principalTable: "DimVenues",
                        principalColumn: "VenueKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DimAcademicTerms_AcademicTermId",
                table: "DimAcademicTerms",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_DimArticles_ArticleId",
                table: "DimArticles",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_DimFields_DetailedFieldId",
                table: "DimFields",
                column: "DetailedFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_DimIndexingSources_IndexingSourceId",
                table: "DimIndexingSources",
                column: "IndexingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DimProjects_ProjectId",
                table: "DimProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DimPublicationStatuses_PublicationStatusId",
                table: "DimPublicationStatuses",
                column: "PublicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchLines_ResearchLineId",
                table: "DimResearchLines",
                column: "ResearchLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DimVenues_VenueId",
                table: "DimVenues",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleAuthors_ArticleKey_AuthorKey",
                table: "FactArticleAuthors",
                columns: new[] { "ArticleKey", "AuthorKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleAuthors_AuthorKey",
                table: "FactArticleAuthors",
                column: "AuthorKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleIndexings_ArticleKey_IndexingSourceKey",
                table: "FactArticleIndexings",
                columns: new[] { "ArticleKey", "IndexingSourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleIndexings_IndexingSourceKey",
                table: "FactArticleIndexings",
                column: "IndexingSourceKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_AcademicTermKey",
                table: "FactArticlePublications",
                column: "AcademicTermKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ArticleKey",
                table: "FactArticlePublications",
                column: "ArticleKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_CreatedDateKey",
                table: "FactArticlePublications",
                column: "CreatedDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_FieldKey",
                table: "FactArticlePublications",
                column: "FieldKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ProjectKey",
                table: "FactArticlePublications",
                column: "ProjectKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_PublicationDateKey",
                table: "FactArticlePublications",
                column: "PublicationDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_PublicationStatusKey",
                table: "FactArticlePublications",
                column: "PublicationStatusKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ResearchLineKey",
                table: "FactArticlePublications",
                column: "ResearchLineKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_VenueKey",
                table: "FactArticlePublications",
                column: "VenueKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactVenueMetricYears_VenueKey_Year",
                table: "FactVenueMetricYears",
                columns: new[] { "VenueKey", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactVenueMetricYears_YearDateKey",
                table: "FactVenueMetricYears",
                column: "YearDateKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactArticleAuthors");

            migrationBuilder.DropTable(
                name: "FactArticleIndexings");

            migrationBuilder.DropTable(
                name: "FactArticlePublications");

            migrationBuilder.DropTable(
                name: "FactVenueMetricYears");

            migrationBuilder.DropTable(
                name: "DimAuthors");

            migrationBuilder.DropTable(
                name: "DimIndexingSources");

            migrationBuilder.DropTable(
                name: "DimAcademicTerms");

            migrationBuilder.DropTable(
                name: "DimArticles");

            migrationBuilder.DropTable(
                name: "DimFields");

            migrationBuilder.DropTable(
                name: "DimProjects");

            migrationBuilder.DropTable(
                name: "DimPublicationStatuses");

            migrationBuilder.DropTable(
                name: "DimResearchLines");

            migrationBuilder.DropTable(
                name: "DimDates");

            migrationBuilder.DropTable(
                name: "DimVenues");
        }
    }
}
