using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Articles
{
    /// <inheritdoc />
    public partial class ActualizarArticulos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicTerms",
                columns: table => new
                {
                    AcademicTermId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicTerms", x => x.AcademicTermId);
                });

            migrationBuilder.CreateTable(
                name: "BroadFields",
                columns: table => new
                {
                    BroadFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadFields", x => x.BroadFieldId);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.FacultyId);
                });

            migrationBuilder.CreateTable(
                name: "FieldCatalog",
                columns: table => new
                {
                    FieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhysicalTableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhysicalColumnName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceTableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSystemField = table.Column<bool>(type: "bit", nullable: false),
                    IsDynamic = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    IsFilterable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValidationRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldCatalog", x => x.FieldId);
                });

            migrationBuilder.CreateTable(
                name: "FormDefinitions",
                columns: table => new
                {
                    FormId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FormName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormDefinitions", x => x.FormId);
                });

            migrationBuilder.CreateTable(
                name: "PublicationStatuses",
                columns: table => new
                {
                    PublicationStatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationStatuses", x => x.PublicationStatusId);
                });

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
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastImportBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationMatrix", x => x.RegistrationMatrixId);
                });

            migrationBuilder.CreateTable(
                name: "ResearchLines",
                columns: table => new
                {
                    ResearchLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchLines", x => x.ResearchLineId);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    VenueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssnCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IssueNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VolumeNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    JournalUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Journal")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.VenueId);
                });

            migrationBuilder.CreateTable(
                name: "SpecificFields",
                columns: table => new
                {
                    SpecificFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BroadFieldId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificFields", x => x.SpecificFieldId);
                    table.ForeignKey(
                        name: "FK_SpecificFields_BroadFields_BroadFieldId",
                        column: x => x.BroadFieldId,
                        principalTable: "BroadFields",
                        principalColumn: "BroadFieldId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DynamicFieldOptions",
                columns: table => new
                {
                    DynamicFieldOptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    OptionValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OptionLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicFieldOptions", x => x.DynamicFieldOptionId);
                    table.ForeignKey(
                        name: "FK_DynamicFieldOptions_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                columns: table => new
                {
                    FormFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColumnSpan = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFields", x => x.FormFieldId);
                    table.ForeignKey(
                        name: "FK_FormFields_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormFields_FormDefinitions_FormId",
                        column: x => x.FormId,
                        principalTable: "FormDefinitions",
                        principalColumn: "FormId",
                        onDelete: ReferentialAction.Cascade);
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
                name: "VenueMetrics",
                columns: table => new
                {
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    SJR = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    Quartile = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueMetrics", x => new { x.VenueId, x.Year });
                    table.ForeignKey(
                        name: "FK_VenueMetrics_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "VenueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetailedFields",
                columns: table => new
                {
                    DetailedFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpecificFieldId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailedFields", x => x.DetailedFieldId);
                    table.ForeignKey(
                        name: "FK_DetailedFields_SpecificFields_SpecificFieldId",
                        column: x => x.SpecificFieldId,
                        principalTable: "SpecificFields",
                        principalColumn: "SpecificFieldId",
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

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Doi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Year = table.Column<short>(type: "smallint", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    PublicationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsProjectResult = table.Column<bool>(type: "bit", nullable: false),
                    HasInterculturalComponent = table.Column<bool>(type: "bit", nullable: false),
                    ProceedingsName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Proceedings = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Filiacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ExternalSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VenueId = table.Column<int>(type: "int", nullable: true),
                    AcademicTermId = table.Column<int>(type: "int", nullable: true),
                    PublicationStatusId = table.Column<byte>(type: "tinyint", nullable: true),
                    ResearchLineId = table.Column<int>(type: "int", nullable: true),
                    BroadFieldId = table.Column<int>(type: "int", nullable: true),
                    SpecificFieldId = table.Column<int>(type: "int", nullable: true),
                    DetailedFieldId = table.Column<int>(type: "int", nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOpenAccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "AcademicTermId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Articles_BroadFields_BroadFieldId",
                        column: x => x.BroadFieldId,
                        principalTable: "BroadFields",
                        principalColumn: "BroadFieldId");
                    table.ForeignKey(
                        name: "FK_Articles_DetailedFields_DetailedFieldId",
                        column: x => x.DetailedFieldId,
                        principalTable: "DetailedFields",
                        principalColumn: "DetailedFieldId");
                    table.ForeignKey(
                        name: "FK_Articles_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "FacultyId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Articles_PublicationStatuses_PublicationStatusId",
                        column: x => x.PublicationStatusId,
                        principalTable: "PublicationStatuses",
                        principalColumn: "PublicationStatusId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Articles_ResearchLines_ResearchLineId",
                        column: x => x.ResearchLineId,
                        principalTable: "ResearchLines",
                        principalColumn: "ResearchLineId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Articles_SpecificFields_SpecificFieldId",
                        column: x => x.SpecificFieldId,
                        principalTable: "SpecificFields",
                        principalColumn: "SpecificFieldId");
                    table.ForeignKey(
                        name: "FK_Articles_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "VenueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArticleFiles",
                columns: table => new
                {
                    ArticleFileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleFiles", x => x.ArticleFileId);
                    table.ForeignKey(
                        name: "FK_ArticleFiles_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleIndexings",
                columns: table => new
                {
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    IndexingSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleIndexings", x => new { x.ArticleId, x.IndexingSourceId });
                    table.ForeignKey(
                        name: "FK_ArticleIndexings_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleIndexings_IndexingSources_IndexingSourceId",
                        column: x => x.IndexingSourceId,
                        principalTable: "IndexingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Identificacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Participacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ParticipantType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InstitutionalPersonId = table.Column<int>(type: "int", nullable: true),
                    IsPrimaryAuthor = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Orcid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Affiliation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ExternalAuthorId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleParticipants_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DynamicFieldValues",
                columns: table => new
                {
                    DynamicFieldValueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    ValueString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValueInt = table.Column<int>(type: "int", nullable: true),
                    ValueDecimal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValueBit = table.Column<bool>(type: "bit", nullable: true),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicFieldValues", x => x.DynamicFieldValueId);
                    table.ForeignKey(
                        name: "FK_DynamicFieldValues_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DynamicFieldValues_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArticleParticipantDynamicFieldValues",
                columns: table => new
                {
                    ArticleParticipantDynamicFieldValueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleParticipantId = table.Column<int>(type: "int", nullable: false),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    ValueString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValueInt = table.Column<int>(type: "int", nullable: true),
                    ValueDecimal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValueBit = table.Column<bool>(type: "bit", nullable: true),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleParticipantDynamicFieldValues", x => x.ArticleParticipantDynamicFieldValueId);
                    table.ForeignKey(
                        name: "FK_ArticleParticipantDynamicFieldValues_ArticleParticipants_ArticleParticipantId",
                        column: x => x.ArticleParticipantId,
                        principalTable: "ArticleParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleParticipantDynamicFieldValues_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_Name",
                table: "AcademicTerms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleFiles_ArticleId",
                table: "ArticleFiles",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleIndexings_IndexingSourceId",
                table: "ArticleIndexings",
                column: "IndexingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleParticipantDynamicFieldValues_ArticleParticipantId",
                table: "ArticleParticipantDynamicFieldValues",
                column: "ArticleParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleParticipantDynamicFieldValues_FieldId",
                table: "ArticleParticipantDynamicFieldValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleParticipants_ArticleId_Index",
                table: "ArticleParticipants",
                columns: new[] { "ArticleId", "Index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_AcademicTermId",
                table: "Articles",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_BroadFieldId",
                table: "Articles",
                column: "BroadFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_DetailedFieldId",
                table: "Articles",
                column: "DetailedFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ExternalSource_ExternalId",
                table: "Articles",
                columns: new[] { "ExternalSource", "ExternalId" },
                filter: "[ExternalSource] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_FacultyId",
                table: "Articles",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_PublicationStatusId",
                table: "Articles",
                column: "PublicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ResearchLineId",
                table: "Articles",
                column: "ResearchLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_SpecificFieldId",
                table: "Articles",
                column: "SpecificFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_VenueId",
                table: "Articles",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "UX_Articles_Doi_NotBlank",
                table: "Articles",
                column: "Doi",
                unique: true,
                filter: "[Doi] IS NOT NULL AND [Doi] <> N''");

            migrationBuilder.CreateIndex(
                name: "UX_Articles_TitleYearVenue_NoDoi",
                table: "Articles",
                columns: new[] { "Title", "Year", "VenueId" },
                unique: true,
                filter: "[Doi] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BroadFields_Name",
                table: "BroadFields",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_DetailedField_Specific_Code",
                table: "DetailedFields",
                columns: new[] { "SpecificFieldId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");

            migrationBuilder.CreateIndex(
                name: "UQ_DetailedField_Specific_Name",
                table: "DetailedFields",
                columns: new[] { "SpecificFieldId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldOptions_FieldId",
                table: "DynamicFieldOptions",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldValues_ArticleId",
                table: "DynamicFieldValues",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldValues_FieldId",
                table: "DynamicFieldValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_Code",
                table: "Faculties",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_Name",
                table: "Faculties",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FieldId",
                table: "FormFields",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormId",
                table: "FormFields",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationStatuses_Name",
                table: "PublicationStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrix_CreatedByUserId",
                table: "RegistrationMatrix",
                column: "CreatedByUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ResearchLines_Name",
                table: "ResearchLines",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SpecificField_Broad_Code",
                table: "SpecificFields",
                columns: new[] { "BroadFieldId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");

            migrationBuilder.CreateIndex(
                name: "UQ_SpecificField_Broad_Name",
                table: "SpecificFields",
                columns: new[] { "BroadFieldId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Venue_Name_Issn",
                table: "Venues",
                columns: new[] { "Name", "IssnCode" },
                unique: true,
                filter: "[IssnCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleFiles");

            migrationBuilder.DropTable(
                name: "ArticleIndexings");

            migrationBuilder.DropTable(
                name: "ArticleParticipantDynamicFieldValues");

            migrationBuilder.DropTable(
                name: "DynamicFieldOptions");

            migrationBuilder.DropTable(
                name: "DynamicFieldValues");

            migrationBuilder.DropTable(
                name: "FormFields");

            migrationBuilder.DropTable(
                name: "RegistrationMatrixCell");

            migrationBuilder.DropTable(
                name: "RegistrationMatrixColumn");

            migrationBuilder.DropTable(
                name: "VenueMetrics");

            migrationBuilder.DropTable(
                name: "ArticleParticipants");

            migrationBuilder.DropTable(
                name: "FormDefinitions");

            migrationBuilder.DropTable(
                name: "RegistrationMatrixRow");

            migrationBuilder.DropTable(
                name: "FieldCatalog");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "RegistrationMatrix");

            migrationBuilder.DropTable(
                name: "AcademicTerms");

            migrationBuilder.DropTable(
                name: "DetailedFields");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropTable(
                name: "PublicationStatuses");

            migrationBuilder.DropTable(
                name: "ResearchLines");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropTable(
                name: "SpecificFields");

            migrationBuilder.DropTable(
                name: "BroadFields");
        }
    }
}
