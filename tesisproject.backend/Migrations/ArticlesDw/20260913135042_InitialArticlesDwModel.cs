using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.ArticlesDw
{
    /// <inheritdoc />
    public partial class InitialArticlesDwModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ArticlesDW");

            migrationBuilder.CreateTable(
                name: "DimAcademicTerms",
                schema: "ArticlesDW",
                columns: table => new
                {
                    AcademicTermKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicTermId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimAcademicTerms", x => x.AcademicTermKey);
                });

            migrationBuilder.CreateTable(
                name: "DimArticles",
                schema: "ArticlesDW",
                columns: table => new
                {
                    ArticleKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: true),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Doi = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PublicationYear = table.Column<short>(type: "smallint", nullable: true),
                    YearRaw = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IssnIsbn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublicationUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ExternalSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProceedingsName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Proceedings = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Filiacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimArticles", x => x.ArticleKey);
                });

            migrationBuilder.CreateTable(
                name: "DimAuthors",
                schema: "ArticlesDW",
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
                name: "DimDates",
                schema: "ArticlesDW",
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
                schema: "ArticlesDW",
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
                name: "DimFields",
                schema: "ArticlesDW",
                columns: table => new
                {
                    FieldKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BroadFieldId = table.Column<int>(type: "int", nullable: false),
                    BroadFieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SpecificFieldId = table.Column<int>(type: "int", nullable: true),
                    SpecificFieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DetailedFieldId = table.Column<int>(type: "int", nullable: true),
                    DetailedFieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimFields", x => x.FieldKey);
                });

            migrationBuilder.CreateTable(
                name: "DimIndexingDatabases",
                schema: "ArticlesDW",
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
                name: "DimIndexingSources",
                schema: "ArticlesDW",
                columns: table => new
                {
                    IndexingSourceKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndexingSourceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimIndexingSources", x => x.IndexingSourceKey);
                });

            migrationBuilder.CreateTable(
                name: "DimJournals",
                schema: "ArticlesDW",
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
                name: "DimProductTypes",
                schema: "ArticlesDW",
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
                name: "DimPublicationStatuses",
                schema: "ArticlesDW",
                columns: table => new
                {
                    PublicationStatusKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicationStatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimPublicationStatuses", x => x.PublicationStatusKey);
                });

            migrationBuilder.CreateTable(
                name: "DimQuartiles",
                schema: "ArticlesDW",
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
                name: "DimResearchLines",
                schema: "ArticlesDW",
                columns: table => new
                {
                    ResearchLineKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchLineId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimResearchLines", x => x.ResearchLineKey);
                });

            migrationBuilder.CreateTable(
                name: "DimVenues",
                schema: "ArticlesDW",
                columns: table => new
                {
                    VenueKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IssnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Issue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Volume = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimVenues", x => x.VenueKey);
                });

            migrationBuilder.CreateTable(
                name: "FactArticleAuthors",
                schema: "ArticlesDW",
                columns: table => new
                {
                    FactArticleAuthorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAuthorId = table.Column<int>(type: "int", nullable: false),
                    ArticleKey = table.Column<int>(type: "int", nullable: false),
                    AuthorKey = table.Column<int>(type: "int", nullable: false),
                    AuthorOrder = table.Column<int>(type: "int", nullable: true),
                    IsPrimaryAuthor = table.Column<bool>(type: "bit", nullable: false),
                    Participation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AffiliationSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactArticleAuthors", x => x.FactArticleAuthorId);
                    table.ForeignKey(
                        name: "FK_FactArticleAuthors_DimArticles_ArticleKey",
                        column: x => x.ArticleKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimArticles",
                        principalColumn: "ArticleKey");
                    table.ForeignKey(
                        name: "FK_FactArticleAuthors_DimAuthors_AuthorKey",
                        column: x => x.AuthorKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimAuthors",
                        principalColumn: "AuthorKey");
                });

            migrationBuilder.CreateTable(
                name: "FactArticleIndexings",
                schema: "ArticlesDW",
                columns: table => new
                {
                    FactArticleIndexingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleKey = table.Column<int>(type: "int", nullable: false),
                    IndexingSourceKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactArticleIndexings", x => x.FactArticleIndexingId);
                    table.ForeignKey(
                        name: "FK_FactArticleIndexings_DimArticles_ArticleKey",
                        column: x => x.ArticleKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimArticles",
                        principalColumn: "ArticleKey");
                    table.ForeignKey(
                        name: "FK_FactArticleIndexings_DimIndexingSources_IndexingSourceKey",
                        column: x => x.IndexingSourceKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimIndexingSources",
                        principalColumn: "IndexingSourceKey");
                });

            migrationBuilder.CreateTable(
                name: "FactArticlePublications",
                schema: "ArticlesDW",
                columns: table => new
                {
                    FactArticlePublicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleKey = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductTypeKey = table.Column<int>(type: "int", nullable: false),
                    JournalKey = table.Column<int>(type: "int", nullable: true),
                    IndexingDatabaseKey = table.Column<int>(type: "int", nullable: true),
                    QuartileKey = table.Column<int>(type: "int", nullable: true),
                    VenueKey = table.Column<int>(type: "int", nullable: true),
                    AcademicTermKey = table.Column<int>(type: "int", nullable: true),
                    PublicationStatusKey = table.Column<int>(type: "int", nullable: true),
                    ResearchLineKey = table.Column<int>(type: "int", nullable: true),
                    FieldKey = table.Column<int>(type: "int", nullable: true),
                    ArticleFacultyKey = table.Column<int>(type: "int", nullable: true),
                    ProjectFacultyKey = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    CreatedDateKey = table.Column<int>(type: "int", nullable: false),
                    PublishedDateKey = table.Column<int>(type: "int", nullable: true),
                    ArticleCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AuthorCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IndexingCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    Sjr = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    IsProjectResultFlag = table.Column<bool>(type: "bit", nullable: false),
                    IsOpenAccessFlag = table.Column<bool>(type: "bit", nullable: true),
                    HasInterculturalFlag = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactArticlePublications", x => x.FactArticlePublicationId);
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimAcademicTerms_AcademicTermKey",
                        column: x => x.AcademicTermKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimAcademicTerms",
                        principalColumn: "AcademicTermKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimArticles_ArticleKey",
                        column: x => x.ArticleKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimArticles",
                        principalColumn: "ArticleKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimDates_CreatedDateKey",
                        column: x => x.CreatedDateKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimDates",
                        principalColumn: "DateKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimDates_PublishedDateKey",
                        column: x => x.PublishedDateKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimDates",
                        principalColumn: "DateKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimFaculties_ArticleFacultyKey",
                        column: x => x.ArticleFacultyKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimFaculties",
                        principalColumn: "FacultyKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimFaculties_ProjectFacultyKey",
                        column: x => x.ProjectFacultyKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimFaculties",
                        principalColumn: "FacultyKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimFields_FieldKey",
                        column: x => x.FieldKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimFields",
                        principalColumn: "FieldKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimIndexingDatabases_IndexingDatabaseKey",
                        column: x => x.IndexingDatabaseKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimIndexingDatabases",
                        principalColumn: "IndexingDatabaseKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimJournals_JournalKey",
                        column: x => x.JournalKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimJournals",
                        principalColumn: "JournalKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimProductTypes_ProductTypeKey",
                        column: x => x.ProductTypeKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimProductTypes",
                        principalColumn: "ProductTypeKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimPublicationStatuses_PublicationStatusKey",
                        column: x => x.PublicationStatusKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimPublicationStatuses",
                        principalColumn: "PublicationStatusKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimQuartiles_QuartileKey",
                        column: x => x.QuartileKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimQuartiles",
                        principalColumn: "QuartileKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimResearchLines_ResearchLineKey",
                        column: x => x.ResearchLineKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimResearchLines",
                        principalColumn: "ResearchLineKey");
                    table.ForeignKey(
                        name: "FK_FactArticlePublications_DimVenues_VenueKey",
                        column: x => x.VenueKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimVenues",
                        principalColumn: "VenueKey");
                });

            migrationBuilder.CreateTable(
                name: "FactVenueMetricYears",
                schema: "ArticlesDW",
                columns: table => new
                {
                    FactVenueMetricYearId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueKey = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    Sjr = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Quartile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactVenueMetricYears", x => x.FactVenueMetricYearId);
                    table.ForeignKey(
                        name: "FK_FactVenueMetricYears_DimVenues_VenueKey",
                        column: x => x.VenueKey,
                        principalSchema: "ArticlesDW",
                        principalTable: "DimVenues",
                        principalColumn: "VenueKey");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DimAcademicTerms_AcademicTermId",
                schema: "ArticlesDW",
                table: "DimAcademicTerms",
                column: "AcademicTermId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimArticles_ArticleId",
                schema: "ArticlesDW",
                table: "DimArticles",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_DimArticles_ProductId",
                schema: "ArticlesDW",
                table: "DimArticles",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimArticles_ProductTypeId",
                schema: "ArticlesDW",
                table: "DimArticles",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_AppUserId",
                schema: "ArticlesDW",
                table: "DimAuthors",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_AuthorId",
                schema: "ArticlesDW",
                table: "DimAuthors",
                column: "AuthorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_ExternalResearcherId",
                schema: "ArticlesDW",
                table: "DimAuthors",
                column: "ExternalResearcherId");

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_IdAsp",
                schema: "ArticlesDW",
                table: "DimAuthors",
                column: "IdAsp");

            migrationBuilder.CreateIndex(
                name: "IX_DimAuthors_IsInstitutional",
                schema: "ArticlesDW",
                table: "DimAuthors",
                column: "IsInstitutional");

            migrationBuilder.CreateIndex(
                name: "IX_DimDates_Date",
                schema: "ArticlesDW",
                table: "DimDates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DimDates_Year_Month",
                schema: "ArticlesDW",
                table: "DimDates",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_DimFaculties_FacultyCode",
                schema: "ArticlesDW",
                table: "DimFaculties",
                column: "FacultyCode");

            migrationBuilder.CreateIndex(
                name: "IX_DimFaculties_FacultyId",
                schema: "ArticlesDW",
                table: "DimFaculties",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_DimFaculties_FacultyName",
                schema: "ArticlesDW",
                table: "DimFaculties",
                column: "FacultyName");

            migrationBuilder.CreateIndex(
                name: "IX_DimFields_BroadFieldId_SpecificFieldId_DetailedFieldId",
                schema: "ArticlesDW",
                table: "DimFields",
                columns: new[] { "BroadFieldId", "SpecificFieldId", "DetailedFieldId" },
                unique: true,
                filter: "[SpecificFieldId] IS NOT NULL AND [DetailedFieldId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DimIndexingDatabases_Name",
                schema: "ArticlesDW",
                table: "DimIndexingDatabases",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DimIndexingSources_IndexingSourceId",
                schema: "ArticlesDW",
                table: "DimIndexingSources",
                column: "IndexingSourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimJournals_Name",
                schema: "ArticlesDW",
                table: "DimJournals",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimProductTypes_Name",
                schema: "ArticlesDW",
                table: "DimProductTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DimProductTypes_ProductTypeId",
                schema: "ArticlesDW",
                table: "DimProductTypes",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DimPublicationStatuses_PublicationStatusId",
                schema: "ArticlesDW",
                table: "DimPublicationStatuses",
                column: "PublicationStatusId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimQuartiles_Code",
                schema: "ArticlesDW",
                table: "DimQuartiles",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DimResearchLines_ResearchLineId",
                schema: "ArticlesDW",
                table: "DimResearchLines",
                column: "ResearchLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimVenues_VenueId",
                schema: "ArticlesDW",
                table: "DimVenues",
                column: "VenueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleAuthors_ArticleKey_AuthorKey",
                schema: "ArticlesDW",
                table: "FactArticleAuthors",
                columns: new[] { "ArticleKey", "AuthorKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleAuthors_AuthorKey",
                schema: "ArticlesDW",
                table: "FactArticleAuthors",
                column: "AuthorKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleAuthors_ProductAuthorId",
                schema: "ArticlesDW",
                table: "FactArticleAuthors",
                column: "ProductAuthorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleIndexings_ArticleKey_IndexingSourceKey",
                schema: "ArticlesDW",
                table: "FactArticleIndexings",
                columns: new[] { "ArticleKey", "IndexingSourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticleIndexings_IndexingSourceKey",
                schema: "ArticlesDW",
                table: "FactArticleIndexings",
                column: "IndexingSourceKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_AcademicTermKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "AcademicTermKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ArticleFacultyKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "ArticleFacultyKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ArticleKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "ArticleKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_CreatedDateKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "CreatedDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_FieldKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "FieldKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_IndexingDatabaseKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "IndexingDatabaseKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_JournalKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "JournalKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ProductId",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ProductTypeKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "ProductTypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ProjectFacultyKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "ProjectFacultyKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ProjectId",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_PublicationStatusKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "PublicationStatusKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_PublishedDateKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "PublishedDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_QuartileKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "QuartileKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_ResearchLineKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "ResearchLineKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactArticlePublications_VenueKey",
                schema: "ArticlesDW",
                table: "FactArticlePublications",
                column: "VenueKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactVenueMetricYears_VenueKey_Year",
                schema: "ArticlesDW",
                table: "FactVenueMetricYears",
                columns: new[] { "VenueKey", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactArticleAuthors",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "FactArticleIndexings",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "FactArticlePublications",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "FactVenueMetricYears",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimAuthors",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimIndexingSources",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimAcademicTerms",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimArticles",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimDates",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimFaculties",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimFields",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimIndexingDatabases",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimJournals",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimProductTypes",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimPublicationStatuses",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimQuartiles",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimResearchLines",
                schema: "ArticlesDW");

            migrationBuilder.DropTable(
                name: "DimVenues",
                schema: "ArticlesDW");
        }
    }
}
