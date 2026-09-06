using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations.Unified
{
    /// <inheritdoc />
    public partial class InitialUnifiedDide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AcademicTerms",
                schema: "dbo",
                columns: table => new
                {
                    AcademicTermId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalPeriodId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: true),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicTerms", x => x.AcademicTermId);
                    table.CheckConstraint("CK_AcademicTerms_Dates", "([StartDate] IS NULL AND [EndDate] IS NULL) OR ([StartDate] IS NOT NULL AND [EndDate] IS NOT NULL AND [StartDate] <= [EndDate])");
                });

            migrationBuilder.CreateTable(
                name: "AppConfigurations",
                schema: "dbo",
                columns: table => new
                {
                    AppConfigurationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaxValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConfigurations", x => x.AppConfigurationId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BroadFields",
                schema: "dbo",
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
                name: "Convocations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsoCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsoAlpha3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportFields",
                schema: "dbo",
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
                schema: "dbo",
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
                name: "Faculties",
                schema: "dbo",
                columns: table => new
                {
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalFacultyId = table.Column<int>(type: "int", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Acronym = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.FacultyId);
                });

            migrationBuilder.CreateTable(
                name: "FacultyScopes",
                schema: "dbo",
                columns: table => new
                {
                    FacultyScopeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyScopes", x => x.FacultyScopeId);
                });

            migrationBuilder.CreateTable(
                name: "FieldCatalog",
                schema: "dbo",
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
                schema: "dbo",
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
                name: "FundingTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndexingSources",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Abbreviation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexingSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberRoleTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Flag = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberRoleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectExtensionTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExtensionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectOriginTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOriginTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStates",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicationStatuses",
                schema: "dbo",
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
                schema: "dbo",
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
                name: "ResearchCategoryGroups",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchCategoryGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchLines",
                schema: "dbo",
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
                name: "TransactionTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                schema: "dbo",
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
                name: "VisitStates",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppUsers",
                schema: "dbo",
                columns: table => new
                {
                    IdUser = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdLocal = table.Column<int>(type: "int", nullable: true),
                    IdAsp = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.IdUser);
                    table.ForeignKey(
                        name: "FK_AppUsers_AspNetUsers_IdLocal",
                        column: x => x.IdLocal,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "dbo",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SpecificFields",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "BroadFields",
                        principalColumn: "BroadFieldId");
                });

            migrationBuilder.CreateTable(
                name: "Institutions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Institutions_Countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "dbo",
                        principalTable: "Countries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExportTemplateColumns",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "ExportFields",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportTemplateColumns_ExportTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "dbo",
                        principalTable: "ExportTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FacultyScopeFaculties",
                schema: "dbo",
                columns: table => new
                {
                    FacultyScopeId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyScopeFaculties", x => new { x.FacultyScopeId, x.FacultyId });
                    table.ForeignKey(
                        name: "FK_FacultyScopeFaculties_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalSchema: "dbo",
                        principalTable: "Faculties",
                        principalColumn: "FacultyId");
                    table.ForeignKey(
                        name: "FK_FacultyScopeFaculties_FacultyScopes_FacultyScopeId",
                        column: x => x.FacultyScopeId,
                        principalSchema: "dbo",
                        principalTable: "FacultyScopes",
                        principalColumn: "FacultyScopeId");
                });

            migrationBuilder.CreateTable(
                name: "DynamicFieldOptions",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId");
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId");
                    table.ForeignKey(
                        name: "FK_FormFields_FormDefinitions_FormId",
                        column: x => x.FormId,
                        principalSchema: "dbo",
                        principalTable: "FormDefinitions",
                        principalColumn: "FormId");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "dbo",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_Groups_GroupTypes_GroupTypeId",
                        column: x => x.GroupTypeId,
                        principalSchema: "dbo",
                        principalTable: "GroupTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConvocationRules",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConvocationId = table.Column<int>(type: "int", nullable: false),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    MinDurationMonths = table.Column<int>(type: "int", nullable: true),
                    MaxDurationMonths = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    MinQuartile = table.Column<int>(type: "int", nullable: false),
                    GroupCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequiredInGroup = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvocationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConvocationRules_Convocations_ConvocationId",
                        column: x => x.ConvocationId,
                        principalSchema: "dbo",
                        principalTable: "Convocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConvocationRules_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalSchema: "dbo",
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeDefinitions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    ProductAttributeId = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeDefinitions_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalSchema: "dbo",
                        principalTable: "ProductAttributes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductAttributeDefinitions_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalSchema: "dbo",
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RegistrationMatrixColumn",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId");
                    table.ForeignKey(
                        name: "FK_RegistrationMatrixColumn_RegistrationMatrix_RegistrationMatrixId",
                        column: x => x.RegistrationMatrixId,
                        principalSchema: "dbo",
                        principalTable: "RegistrationMatrix",
                        principalColumn: "RegistrationMatrixId");
                });

            migrationBuilder.CreateTable(
                name: "RegistrationMatrixRow",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "RegistrationMatrix",
                        principalColumn: "RegistrationMatrixId");
                });

            migrationBuilder.CreateTable(
                name: "ResearchCategoryTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchCategoryGroupId = table.Column<int>(type: "int", nullable: false),
                    IsFilterEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchCategoryTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchCategoryTypes_ResearchCategoryGroups_ResearchCategoryGroupId",
                        column: x => x.ResearchCategoryGroupId,
                        principalSchema: "dbo",
                        principalTable: "ResearchCategoryGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VenueMetrics",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Venues",
                        principalColumn: "VenueId");
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "dbo",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ResolutionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_Documents_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Documents_AppUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Documents_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "dbo",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserFacultyScopeAssignments",
                schema: "dbo",
                columns: table => new
                {
                    IdentityUserId = table.Column<int>(type: "int", nullable: false),
                    FacultyScopeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFacultyScopeAssignments", x => new { x.IdentityUserId, x.FacultyScopeId });
                    table.ForeignKey(
                        name: "FK_UserFacultyScopeAssignments_AppUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_UserFacultyScopeAssignments_FacultyScopes_FacultyScopeId",
                        column: x => x.FacultyScopeId,
                        principalSchema: "dbo",
                        principalTable: "FacultyScopes",
                        principalColumn: "FacultyScopeId");
                });

            migrationBuilder.CreateTable(
                name: "DetailedFields",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "SpecificFields",
                        principalColumn: "SpecificFieldId");
                });

            migrationBuilder.CreateTable(
                name: "ExternalResearchers",
                schema: "dbo",
                columns: table => new
                {
                    ExternalResearcherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InstitutionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalResearchers", x => x.ExternalResearcherId);
                    table.ForeignKey(
                        name: "FK_ExternalResearchers_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalSchema: "dbo",
                        principalTable: "Institutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                schema: "dbo",
                columns: table => new
                {
                    GroupMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MemberRoleId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.GroupMemberId);
                    table.ForeignKey(
                        name: "FK_GroupMembers_AppUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "dbo",
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                    table.ForeignKey(
                        name: "FK_GroupMembers_MemberRoleTypes_MemberRoleId",
                        column: x => x.MemberRoleId,
                        principalSchema: "dbo",
                        principalTable: "MemberRoleTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "dbo",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ProjectTypeId = table.Column<int>(type: "int", nullable: false),
                    ProjectStateId = table.Column<int>(type: "int", nullable: false),
                    ProjectGroupId = table.Column<int>(type: "int", nullable: false),
                    ProjectOriginTypeId = table.Column<int>(type: "int", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectNumber = table.Column<int>(type: "int", nullable: false),
                    ConvocationId = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "date", nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: true),
                    DurationInMonths = table.Column<int>(type: "int", nullable: false),
                    TentativeEndDate = table.Column<DateTime>(type: "date", nullable: true),
                    RealEndDate = table.Column<DateTime>(type: "date", nullable: true),
                    ExecutionPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Projects_Convocations_ConvocationId",
                        column: x => x.ConvocationId,
                        principalSchema: "dbo",
                        principalTable: "Convocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalSchema: "dbo",
                        principalTable: "Faculties",
                        principalColumn: "FacultyId");
                    table.ForeignKey(
                        name: "FK_Projects_Groups_ProjectGroupId",
                        column: x => x.ProjectGroupId,
                        principalSchema: "dbo",
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                    table.ForeignKey(
                        name: "FK_Projects_ProjectOriginTypes_ProjectOriginTypeId",
                        column: x => x.ProjectOriginTypeId,
                        principalSchema: "dbo",
                        principalTable: "ProjectOriginTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProjectStates_ProjectStateId",
                        column: x => x.ProjectStateId,
                        principalSchema: "dbo",
                        principalTable: "ProjectStates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProjectTypes_ProjectTypeId",
                        column: x => x.ProjectTypeId,
                        principalSchema: "dbo",
                        principalTable: "ProjectTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConvocationRuleIndexing",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConvocationRuleId = table.Column<int>(type: "int", nullable: false),
                    IndexingSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvocationRuleIndexing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConvocationRuleIndexing_ConvocationRules_ConvocationRuleId",
                        column: x => x.ConvocationRuleId,
                        principalSchema: "dbo",
                        principalTable: "ConvocationRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConvocationRuleIndexing_IndexingSources_IndexingSourceId",
                        column: x => x.IndexingSourceId,
                        principalSchema: "dbo",
                        principalTable: "IndexingSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RegistrationMatrixCell",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId");
                    table.ForeignKey(
                        name: "FK_RegistrationMatrixCell_RegistrationMatrixRow_RegistrationMatrixRowId",
                        column: x => x.RegistrationMatrixRowId,
                        principalSchema: "dbo",
                        principalTable: "RegistrationMatrixRow",
                        principalColumn: "RegistrationMatrixRowId");
                });

            migrationBuilder.CreateTable(
                name: "ResearchCategories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchCategoryTypeId = table.Column<int>(type: "int", nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchCategories_ResearchCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "dbo",
                        principalTable: "ResearchCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResearchCategories_ResearchCategoryTypes_ResearchCategoryTypeId",
                        column: x => x.ResearchCategoryTypeId,
                        principalSchema: "dbo",
                        principalTable: "ResearchCategoryTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Authors",
                schema: "dbo",
                columns: table => new
                {
                    AuthorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: true),
                    ExternalResearcherId = table.Column<int>(type: "int", nullable: true),
                    Orcid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalAuthorId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.AuthorId);
                    table.CheckConstraint("CK_Authors_ExactlyOneSource", "([AppUserId] IS NOT NULL AND [ExternalResearcherId] IS NULL) OR ([AppUserId] IS NULL AND [ExternalResearcherId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Authors_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Authors_ExternalResearchers_ExternalResearcherId",
                        column: x => x.ExternalResearcherId,
                        principalSchema: "dbo",
                        principalTable: "ExternalResearchers",
                        principalColumn: "ExternalResearcherId");
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                schema: "dbo",
                columns: table => new
                {
                    BudgetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false),
                    FundingTypeId = table.Column<int>(type: "int", nullable: false),
                    InitialAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CertifiedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExecutedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.BudgetId);
                    table.ForeignKey(
                        name: "FK_Budgets_AppUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Budgets_FundingTypes_FundingTypeId",
                        column: x => x.FundingTypeId,
                        principalSchema: "dbo",
                        principalTable: "FundingTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Budgets_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ExternalResearcherProjects",
                schema: "dbo",
                columns: table => new
                {
                    ExternalResearcherProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalResearcherId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ExitDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalResearcherProjects", x => x.ExternalResearcherProjectId);
                    table.ForeignKey(
                        name: "FK_ExternalResearcherProjects_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_ExternalResearcherProjects_ExternalResearchers_ExternalResearcherId",
                        column: x => x.ExternalResearcherId,
                        principalSchema: "dbo",
                        principalTable: "ExternalResearchers",
                        principalColumn: "ExternalResearcherId");
                    table.ForeignKey(
                        name: "FK_ExternalResearcherProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ProjectDocuments",
                schema: "dbo",
                columns: table => new
                {
                    ProjectDocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDocuments", x => x.ProjectDocumentId);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "dbo",
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ProjectExtensions",
                schema: "dbo",
                columns: table => new
                {
                    ProjectExtensionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectExtensionTypeId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    ExtensionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExtensions", x => x.ProjectExtensionId);
                    table.ForeignKey(
                        name: "FK_ProjectExtensions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "dbo",
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_ProjectExtensions_ProjectExtensionTypes_ProjectExtensionTypeId",
                        column: x => x.ProjectExtensionTypeId,
                        principalSchema: "dbo",
                        principalTable: "ProjectExtensionTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectExtensions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ProjectObjectives",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ObjectiveTypeId = table.Column<int>(type: "int", nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WeightedPercentage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectObjectives_ObjectiveTypes_ObjectiveTypeId",
                        column: x => x.ObjectiveTypeId,
                        principalSchema: "dbo",
                        principalTable: "ObjectiveTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectObjectives_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                schema: "dbo",
                columns: table => new
                {
                    VisitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    VisitStateId = table.Column<int>(type: "int", nullable: false),
                    AcademicTermId = table.Column<int>(type: "int", nullable: true),
                    FundingDocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    ProgressDocumentId = table.Column<int>(type: "int", nullable: true),
                    PerformedByUserId = table.Column<int>(type: "int", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PerformedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.VisitId);
                    table.ForeignKey(
                        name: "FK_Visits_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalSchema: "dbo",
                        principalTable: "AcademicTerms",
                        principalColumn: "AcademicTermId");
                    table.ForeignKey(
                        name: "FK_Visits_AppUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Visits_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "dbo",
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_Visits_Documents_FundingDocumentId",
                        column: x => x.FundingDocumentId,
                        principalSchema: "dbo",
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_Visits_Documents_ProgressDocumentId",
                        column: x => x.ProgressDocumentId,
                        principalSchema: "dbo",
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_Visits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_Visits_VisitStates_VisitStateId",
                        column: x => x.VisitStateId,
                        principalSchema: "dbo",
                        principalTable: "VisitStates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectResearchCategories",
                schema: "dbo",
                columns: table => new
                {
                    ProjectResearchCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ResearchCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectResearchCategories", x => x.ProjectResearchCategoryId);
                    table.ForeignKey(
                        name: "FK_ProjectResearchCategories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_ProjectResearchCategories_ResearchCategories_ResearchCategoryId",
                        column: x => x.ResearchCategoryId,
                        principalSchema: "dbo",
                        principalTable: "ResearchCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransactions",
                schema: "dbo",
                columns: table => new
                {
                    BudgetTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "int", nullable: false),
                    CertifiedByUserId = table.Column<int>(type: "int", nullable: false),
                    ExecutedByUserId = table.Column<int>(type: "int", nullable: true),
                    CertifiedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExecutedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertifiedAt = table.Column<DateTime>(type: "date", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "date", nullable: true),
                    BudgetItem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CURNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CertificationDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExecutionDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransactions", x => x.BudgetTransactionId);
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_AppUsers_CertifiedByUserId",
                        column: x => x.CertifiedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_AppUsers_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalSchema: "dbo",
                        principalTable: "Budgets",
                        principalColumn: "BudgetId");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_TransactionTypes_TransactionTypeId",
                        column: x => x.TransactionTypeId,
                        principalSchema: "dbo",
                        principalTable: "TransactionTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveActivities",
                schema: "dbo",
                columns: table => new
                {
                    ObjectiveActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<int>(type: "int", nullable: false),
                    ActivityResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveActivities", x => x.ObjectiveActivityId);
                    table.ForeignKey(
                        name: "FK_ObjectiveActivities_ProjectObjectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalSchema: "dbo",
                        principalTable: "ProjectObjectives",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalSchema: "dbo",
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "dbo",
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_Products_Visits_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "dbo",
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateTable(
                name: "VisitIssues",
                schema: "dbo",
                columns: table => new
                {
                    VisitIssueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReportedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitIssues", x => x.VisitIssueId);
                    table.ForeignKey(
                        name: "FK_VisitIssues_AppUsers_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_VisitIssues_Visits_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "dbo",
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveActivityUsers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveActivityId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    WeeklyHours = table.Column<double>(type: "float", nullable: true),
                    RoleDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReportNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveActivityUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveActivityUsers_AppUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_ObjectiveActivityUsers_ObjectiveActivities_ObjectiveActivityId",
                        column: x => x.ObjectiveActivityId,
                        principalSchema: "dbo",
                        principalTable: "ObjectiveActivities",
                        principalColumn: "ObjectiveActivityId");
                    table.ForeignKey(
                        name: "FK_ObjectiveActivityUsers_Visits_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "dbo",
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateTable(
                name: "VisitObjectiveActivityProgresses",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    ObjectiveActivityId = table.Column<int>(type: "int", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitObjectiveActivityProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitObjectiveActivityProgresses_ObjectiveActivities_ObjectiveActivityId",
                        column: x => x.ObjectiveActivityId,
                        principalSchema: "dbo",
                        principalTable: "ObjectiveActivities",
                        principalColumn: "ObjectiveActivityId");
                    table.ForeignKey(
                        name: "FK_VisitObjectiveActivityProgresses_Visits_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "dbo",
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Doi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Year = table.Column<short>(type: "smallint", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    PublicationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    IsOpenAccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalSchema: "dbo",
                        principalTable: "AcademicTerms",
                        principalColumn: "AcademicTermId");
                    table.ForeignKey(
                        name: "FK_Articles_BroadFields_BroadFieldId",
                        column: x => x.BroadFieldId,
                        principalSchema: "dbo",
                        principalTable: "BroadFields",
                        principalColumn: "BroadFieldId");
                    table.ForeignKey(
                        name: "FK_Articles_DetailedFields_DetailedFieldId",
                        column: x => x.DetailedFieldId,
                        principalSchema: "dbo",
                        principalTable: "DetailedFields",
                        principalColumn: "DetailedFieldId");
                    table.ForeignKey(
                        name: "FK_Articles_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalSchema: "dbo",
                        principalTable: "Faculties",
                        principalColumn: "FacultyId");
                    table.ForeignKey(
                        name: "FK_Articles_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articles_PublicationStatuses_PublicationStatusId",
                        column: x => x.PublicationStatusId,
                        principalSchema: "dbo",
                        principalTable: "PublicationStatuses",
                        principalColumn: "PublicationStatusId");
                    table.ForeignKey(
                        name: "FK_Articles_ResearchLines_ResearchLineId",
                        column: x => x.ResearchLineId,
                        principalSchema: "dbo",
                        principalTable: "ResearchLines",
                        principalColumn: "ResearchLineId");
                    table.ForeignKey(
                        name: "FK_Articles_SpecificFields_SpecificFieldId",
                        column: x => x.SpecificFieldId,
                        principalSchema: "dbo",
                        principalTable: "SpecificFields",
                        principalColumn: "SpecificFieldId");
                    table.ForeignKey(
                        name: "FK_Articles_Venues_VenueId",
                        column: x => x.VenueId,
                        principalSchema: "dbo",
                        principalTable: "Venues",
                        principalColumn: "VenueId");
                });

            migrationBuilder.CreateTable(
                name: "ProductAuthors",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    AuthorOrder = table.Column<int>(type: "int", nullable: true),
                    Participation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsPrimaryAuthor = table.Column<bool>(type: "bit", nullable: false),
                    ParticipantTypeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AffiliationSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IdentificationSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmailSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAuthors", x => x.Id);
                    table.CheckConstraint("CK_ProductAuthors_PositiveOrder", "[AuthorOrder] IS NULL OR [AuthorOrder] > 0");
                    table.ForeignKey(
                        name: "FK_ProductAuthors_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "dbo",
                        principalTable: "Authors",
                        principalColumn: "AuthorId");
                    table.ForeignKey(
                        name: "FK_ProductAuthors_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductValues",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AttributeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductValues_ProductAttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalSchema: "dbo",
                        principalTable: "ProductAttributeDefinitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductValues_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleFiles",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Articles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleIndexings",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleIndexings_IndexingSources_IndexingSourceId",
                        column: x => x.IndexingSourceId,
                        principalSchema: "dbo",
                        principalTable: "IndexingSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DynamicFieldValues",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DynamicFieldValues_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalSchema: "dbo",
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId");
                });

            migrationBuilder.CreateTable(
                name: "ProductAuthorDynamicFieldValues",
                schema: "dbo",
                columns: table => new
                {
                    ProductAuthorDynamicFieldValueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAuthorId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ProductAuthorDynamicFieldValues", x => x.ProductAuthorDynamicFieldValueId);
                    table.ForeignKey(
                        name: "FK_ProductAuthorDynamicFieldValues_FieldCatalog_FieldId",
                        column: x => x.FieldId,
                        principalSchema: "dbo",
                        principalTable: "FieldCatalog",
                        principalColumn: "FieldId");
                    table.ForeignKey(
                        name: "FK_ProductAuthorDynamicFieldValues_ProductAuthors_ProductAuthorId",
                        column: x => x.ProductAuthorId,
                        principalSchema: "dbo",
                        principalTable: "ProductAuthors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_ExternalPeriodId",
                schema: "dbo",
                table: "AcademicTerms",
                column: "ExternalPeriodId",
                unique: true,
                filter: "[ExternalPeriodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_Name",
                schema: "dbo",
                table: "AcademicTerms",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AppConfigurations_Module_SettingKey",
                schema: "dbo",
                table: "AppConfigurations",
                columns: new[] { "Module", "SettingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_IdAsp",
                schema: "dbo",
                table: "AppUsers",
                column: "IdAsp",
                unique: true,
                filter: "[IdAsp] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_IdLocal",
                schema: "dbo",
                table: "AppUsers",
                column: "IdLocal",
                unique: true,
                filter: "[IdLocal] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleFiles_ArticleId",
                schema: "dbo",
                table: "ArticleFiles",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleIndexings_IndexingSourceId",
                schema: "dbo",
                table: "ArticleIndexings",
                column: "IndexingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_AcademicTermId",
                schema: "dbo",
                table: "Articles",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_BroadFieldId",
                schema: "dbo",
                table: "Articles",
                column: "BroadFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_DetailedFieldId",
                schema: "dbo",
                table: "Articles",
                column: "DetailedFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ExternalSource_ExternalId",
                schema: "dbo",
                table: "Articles",
                columns: new[] { "ExternalSource", "ExternalId" },
                filter: "[ExternalSource] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_FacultyId",
                schema: "dbo",
                table: "Articles",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ProductId",
                schema: "dbo",
                table: "Articles",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_PublicationStatusId",
                schema: "dbo",
                table: "Articles",
                column: "PublicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ResearchLineId",
                schema: "dbo",
                table: "Articles",
                column: "ResearchLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_SpecificFieldId",
                schema: "dbo",
                table: "Articles",
                column: "SpecificFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_VenueId",
                schema: "dbo",
                table: "Articles",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "UX_Articles_Doi_NotBlank",
                schema: "dbo",
                table: "Articles",
                column: "Doi",
                unique: true,
                filter: "[Doi] IS NOT NULL AND [Doi] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "dbo",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "dbo",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "dbo",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "dbo",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "dbo",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "dbo",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "dbo",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_AppUserId",
                schema: "dbo",
                table: "Authors",
                column: "AppUserId",
                unique: true,
                filter: "[AppUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_ExternalResearcherId",
                schema: "dbo",
                table: "Authors",
                column: "ExternalResearcherId",
                unique: true,
                filter: "[ExternalResearcherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Orcid",
                schema: "dbo",
                table: "Authors",
                column: "Orcid",
                unique: true,
                filter: "[Orcid] IS NOT NULL AND [Orcid] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_BroadFields_Name",
                schema: "dbo",
                table: "BroadFields",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ApprovedByUserId",
                schema: "dbo",
                table: "Budgets",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FundingTypeId",
                schema: "dbo",
                table: "Budgets",
                column: "FundingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ProjectId",
                schema: "dbo",
                table: "Budgets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_BudgetId",
                schema: "dbo",
                table: "BudgetTransactions",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_CertifiedByUserId",
                schema: "dbo",
                table: "BudgetTransactions",
                column: "CertifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_ExecutedByUserId",
                schema: "dbo",
                table: "BudgetTransactions",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_TransactionTypeId",
                schema: "dbo",
                table: "BudgetTransactions",
                column: "TransactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRuleIndexing_ConvocationRuleId_IndexingSourceId",
                schema: "dbo",
                table: "ConvocationRuleIndexing",
                columns: new[] { "ConvocationRuleId", "IndexingSourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRuleIndexing_IndexingSourceId",
                schema: "dbo",
                table: "ConvocationRuleIndexing",
                column: "IndexingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ConvocationId",
                schema: "dbo",
                table: "ConvocationRules",
                column: "ConvocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ConvocationId_GroupCode",
                schema: "dbo",
                table: "ConvocationRules",
                columns: new[] { "ConvocationId", "GroupCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ConvocationId_ProductTypeId",
                schema: "dbo",
                table: "ConvocationRules",
                columns: new[] { "ConvocationId", "ProductTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_IsActive",
                schema: "dbo",
                table: "ConvocationRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_MinDurationMonths_MaxDurationMonths",
                schema: "dbo",
                table: "ConvocationRules",
                columns: new[] { "MinDurationMonths", "MaxDurationMonths" });

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ProductTypeId",
                schema: "dbo",
                table: "ConvocationRules",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Convocations_IsActive",
                schema: "dbo",
                table: "Convocations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UQ_DetailedField_Specific_Code",
                schema: "dbo",
                table: "DetailedFields",
                columns: new[] { "SpecificFieldId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");

            migrationBuilder.CreateIndex(
                name: "UQ_DetailedField_Specific_Name",
                schema: "dbo",
                table: "DetailedFields",
                columns: new[] { "SpecificFieldId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedByUserId",
                schema: "dbo",
                table: "Documents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UpdatedByUserId",
                schema: "dbo",
                table: "Documents",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Documents_Type1_ResolutionCode",
                schema: "dbo",
                table: "Documents",
                columns: new[] { "DocumentTypeId", "ResolutionCode" },
                unique: true,
                filter: "[ResolutionCode] IS NOT NULL AND [DocumentTypeId] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldOptions_FieldId",
                schema: "dbo",
                table: "DynamicFieldOptions",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldValues_ArticleId",
                schema: "dbo",
                table: "DynamicFieldValues",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicFieldValues_FieldId",
                schema: "dbo",
                table: "DynamicFieldValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplateColumns_ExportFieldId",
                schema: "dbo",
                table: "ExportTemplateColumns",
                column: "ExportFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplateColumns_TemplateId",
                schema: "dbo",
                table: "ExportTemplateColumns",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearcherProjects_CreatedByUserId",
                schema: "dbo",
                table: "ExternalResearcherProjects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearcherProjects_ExternalResearcherId",
                schema: "dbo",
                table: "ExternalResearcherProjects",
                column: "ExternalResearcherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearcherProjects_ProjectId",
                schema: "dbo",
                table: "ExternalResearcherProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearchers_InstitutionId",
                schema: "dbo",
                table: "ExternalResearchers",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_ExternalFacultyId",
                schema: "dbo",
                table: "Faculties",
                column: "ExternalFacultyId",
                unique: true,
                filter: "[ExternalFacultyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_Name",
                schema: "dbo",
                table: "Faculties",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyScopeFaculties_FacultyId",
                schema: "dbo",
                table: "FacultyScopeFaculties",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyScopeFaculties_IsActive",
                schema: "dbo",
                table: "FacultyScopeFaculties",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyScopes_IsActive",
                schema: "dbo",
                table: "FacultyScopes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyScopes_Name",
                schema: "dbo",
                table: "FacultyScopes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FieldId",
                schema: "dbo",
                table: "FormFields",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormId",
                schema: "dbo",
                table: "FormFields",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId",
                schema: "dbo",
                table: "GroupMembers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberRoleId",
                schema: "dbo",
                table: "GroupMembers",
                column: "MemberRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                schema: "dbo",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GroupTypeId",
                schema: "dbo",
                table: "Groups",
                column: "GroupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_CountryId",
                schema: "dbo",
                table: "Institutions",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Name_CountryId",
                schema: "dbo",
                table: "Institutions",
                columns: new[] { "Name", "CountryId" },
                unique: true,
                filter: "[CountryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveActivities_ObjectiveId",
                schema: "dbo",
                table: "ObjectiveActivities",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveActivityUsers_ObjectiveActivityId_UserId_VisitId",
                schema: "dbo",
                table: "ObjectiveActivityUsers",
                columns: new[] { "ObjectiveActivityId", "UserId", "VisitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveActivityUsers_UserId",
                schema: "dbo",
                table: "ObjectiveActivityUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveActivityUsers_VisitId",
                schema: "dbo",
                table: "ObjectiveActivityUsers",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeDefinitions_ProductAttributeId",
                schema: "dbo",
                table: "ProductAttributeDefinitions",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeDefinitions_ProductTypeId",
                schema: "dbo",
                table: "ProductAttributeDefinitions",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAuthorDynamicFieldValues_FieldId",
                schema: "dbo",
                table: "ProductAuthorDynamicFieldValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAuthorDynamicFieldValues_ProductAuthorId",
                schema: "dbo",
                table: "ProductAuthorDynamicFieldValues",
                column: "ProductAuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAuthors_AuthorId",
                schema: "dbo",
                table: "ProductAuthors",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAuthors_ProductId_AuthorId",
                schema: "dbo",
                table: "ProductAuthors",
                columns: new[] { "ProductId", "AuthorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAuthors_ProductId_AuthorOrder",
                schema: "dbo",
                table: "ProductAuthors",
                columns: new[] { "ProductId", "AuthorOrder" },
                unique: true,
                filter: "[AuthorOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                schema: "dbo",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId",
                schema: "dbo",
                table: "Products",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProjectId",
                schema: "dbo",
                table: "Products",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VisitId",
                schema: "dbo",
                table: "Products",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_Name",
                schema: "dbo",
                table: "ProductTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductValues_AttributeDefinitionId",
                schema: "dbo",
                table: "ProductValues",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductValues_ProductId_AttributeDefinitionId",
                schema: "dbo",
                table: "ProductValues",
                columns: new[] { "ProductId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_DocumentId",
                schema: "dbo",
                table: "ProjectDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId",
                schema: "dbo",
                table: "ProjectDocuments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId_DocumentId",
                schema: "dbo",
                table: "ProjectDocuments",
                columns: new[] { "ProjectId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensions_DocumentId",
                schema: "dbo",
                table: "ProjectExtensions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensions_ProjectExtensionTypeId",
                schema: "dbo",
                table: "ProjectExtensions",
                column: "ProjectExtensionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensions_ProjectId",
                schema: "dbo",
                table: "ProjectExtensions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensionTypes_Name",
                schema: "dbo",
                table: "ProjectExtensionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectObjectives_ObjectiveTypeId",
                schema: "dbo",
                table: "ProjectObjectives",
                column: "ObjectiveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectObjectives_ProjectId",
                schema: "dbo",
                table: "ProjectObjectives",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResearchCategories_ProjectId_ResearchCategoryId",
                schema: "dbo",
                table: "ProjectResearchCategories",
                columns: new[] { "ProjectId", "ResearchCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResearchCategories_ResearchCategoryId",
                schema: "dbo",
                table: "ProjectResearchCategories",
                column: "ResearchCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ConvocationId",
                schema: "dbo",
                table: "Projects",
                column: "ConvocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedByUserId",
                schema: "dbo",
                table: "Projects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_FacultyId",
                schema: "dbo",
                table: "Projects",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectGroupId",
                schema: "dbo",
                table: "Projects",
                column: "ProjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectOriginTypeId",
                schema: "dbo",
                table: "Projects",
                column: "ProjectOriginTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectStateId",
                schema: "dbo",
                table: "Projects",
                column: "ProjectStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectTypeId",
                schema: "dbo",
                table: "Projects",
                column: "ProjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationStatuses_Name",
                schema: "dbo",
                table: "PublicationStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "dbo",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "dbo",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrix_CreatedByUserId",
                schema: "dbo",
                table: "RegistrationMatrix",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixCell_FieldId",
                schema: "dbo",
                table: "RegistrationMatrixCell",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixCell_RegistrationMatrixRowId_FieldId",
                schema: "dbo",
                table: "RegistrationMatrixCell",
                columns: new[] { "RegistrationMatrixRowId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixColumn_FieldId",
                schema: "dbo",
                table: "RegistrationMatrixColumn",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixColumn_RegistrationMatrixId_FieldId",
                schema: "dbo",
                table: "RegistrationMatrixColumn",
                columns: new[] { "RegistrationMatrixId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationMatrixRow_RegistrationMatrixId_RowNumber",
                schema: "dbo",
                table: "RegistrationMatrixRow",
                columns: new[] { "RegistrationMatrixId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCategories_ParentCategoryId",
                schema: "dbo",
                table: "ResearchCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCategories_ResearchCategoryTypeId",
                schema: "dbo",
                table: "ResearchCategories",
                column: "ResearchCategoryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCategoryTypes_ResearchCategoryGroupId",
                schema: "dbo",
                table: "ResearchCategoryTypes",
                column: "ResearchCategoryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchLines_Name",
                schema: "dbo",
                table: "ResearchLines",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SpecificField_Broad_Code",
                schema: "dbo",
                table: "SpecificFields",
                columns: new[] { "BroadFieldId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");

            migrationBuilder.CreateIndex(
                name: "UQ_SpecificField_Broad_Name",
                schema: "dbo",
                table: "SpecificFields",
                columns: new[] { "BroadFieldId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFacultyScopeAssignments_FacultyScopeId",
                schema: "dbo",
                table: "UserFacultyScopeAssignments",
                column: "FacultyScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFacultyScopeAssignments_IdentityUserId",
                schema: "dbo",
                table: "UserFacultyScopeAssignments",
                column: "IdentityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFacultyScopeAssignments_IsActive",
                schema: "dbo",
                table: "UserFacultyScopeAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UQ_Venue_Name_Issn",
                schema: "dbo",
                table: "Venues",
                columns: new[] { "Name", "IssnCode" },
                unique: true,
                filter: "[IssnCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VisitIssues_ReportedByUserId",
                schema: "dbo",
                table: "VisitIssues",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitIssues_VisitId",
                schema: "dbo",
                table: "VisitIssues",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitObjectiveActivityProgresses_ObjectiveActivityId",
                schema: "dbo",
                table: "VisitObjectiveActivityProgresses",
                column: "ObjectiveActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitObjectiveActivityProgresses_VisitId",
                schema: "dbo",
                table: "VisitObjectiveActivityProgresses",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitObjectiveActivityProgresses_VisitId_ObjectiveActivityId",
                schema: "dbo",
                table: "VisitObjectiveActivityProgresses",
                columns: new[] { "VisitId", "ObjectiveActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AcademicTermId",
                schema: "dbo",
                table: "Visits",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_DocumentId",
                schema: "dbo",
                table: "Visits",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_FundingDocumentId",
                schema: "dbo",
                table: "Visits",
                column: "FundingDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PerformedByUserId",
                schema: "dbo",
                table: "Visits",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ProgressDocumentId",
                schema: "dbo",
                table: "Visits",
                column: "ProgressDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ProjectId",
                schema: "dbo",
                table: "Visits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_VisitStateId",
                schema: "dbo",
                table: "Visits",
                column: "VisitStateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppConfigurations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ArticleFiles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ArticleIndexings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BudgetTransactions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ConvocationRuleIndexing",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DynamicFieldOptions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DynamicFieldValues",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExportTemplateColumns",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExternalResearcherProjects",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FacultyScopeFaculties",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FormFields",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "GroupMembers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ObjectiveActivityUsers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductAuthorDynamicFieldValues",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductValues",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectDocuments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectExtensions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectResearchCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RegistrationMatrixCell",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RegistrationMatrixColumn",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserFacultyScopeAssignments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VenueMetrics",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VisitIssues",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VisitObjectiveActivityProgresses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Budgets",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TransactionTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ConvocationRules",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IndexingSources",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Articles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExportFields",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExportTemplates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FormDefinitions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MemberRoleTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductAuthors",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductAttributeDefinitions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectExtensionTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ResearchCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RegistrationMatrixRow",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FieldCatalog",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FacultyScopes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ObjectiveActivities",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FundingTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DetailedFields",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PublicationStatuses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ResearchLines",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Venues",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Authors",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductAttributes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ResearchCategoryTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RegistrationMatrix",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectObjectives",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SpecificFields",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExternalResearchers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Visits",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ResearchCategoryGroups",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ObjectiveTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BroadFields",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Institutions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AcademicTerms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Documents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VisitStates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DocumentTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AppUsers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Convocations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Faculties",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectOriginTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectStates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProjectTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "GroupTypes",
                schema: "dbo");
        }
    }
}
