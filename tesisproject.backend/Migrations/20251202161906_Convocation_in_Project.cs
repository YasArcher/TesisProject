using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Convocation_in_Project : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicPeriod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPeriod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
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
                name: "Convocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsoCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsoAlpha3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndexingSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Abbreviation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexingSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberRoleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Flag = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberRoleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectExtensionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsBudgetExecutable = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExtensionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchCategoryGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchCategoryGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
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
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppUsers",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
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
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Institutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Institutions_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
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
                        principalTable: "GroupTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConvocationRules",
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
                        principalTable: "Convocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConvocationRules_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DataType = table.Column<int>(type: "int", maxLength: 32, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeDefinitions_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchCategoryTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchCategoryGroupId = table.Column<int>(type: "int", nullable: false),
                    IsFilterEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchCategoryTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchCategoryTypes_ResearchCategoryGroups_ResearchCategoryGroupId",
                        column: x => x.ResearchCategoryGroupId,
                        principalTable: "ResearchCategoryGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RelatedDocumentId = table.Column<int>(type: "int", nullable: true),
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
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Documents_AppUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Documents_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_Documents_RelatedDocumentId",
                        column: x => x.RelatedDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                });

            migrationBuilder.CreateTable(
                name: "UserFacultyScopes",
                columns: table => new
                {
                    UserFacultyScopeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityUserId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFacultyScopes", x => x.UserFacultyScopeId);
                    table.ForeignKey(
                        name: "FK_UserFacultyScopes_AppUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                });

            migrationBuilder.CreateTable(
                name: "ExternalResearchers",
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
                        principalTable: "Institutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
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
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                    table.ForeignKey(
                        name: "FK_GroupMembers_MemberRoleTypes_MemberRoleId",
                        column: x => x.MemberRoleId,
                        principalTable: "MemberRoleTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConvocationRuleIndexing",
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
                        principalTable: "ConvocationRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConvocationRuleIndexing_IndexingSources_IndexingSourceId",
                        column: x => x.IndexingSourceId,
                        principalTable: "IndexingSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchCategoryTypeId = table.Column<int>(type: "int", nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchCategories_ResearchCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ResearchCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResearchCategories_ResearchCategoryTypes_ResearchCategoryTypeId",
                        column: x => x.ResearchCategoryTypeId,
                        principalTable: "ResearchCategoryTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ProjectTypeId = table.Column<int>(type: "int", nullable: false),
                    ProjectStateId = table.Column<int>(type: "int", nullable: false),
                    ProjectGroupId = table.Column<int>(type: "int", nullable: false),
                    InitialDocumentId = table.Column<int>(type: "int", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectNumber = table.Column<int>(type: "int", nullable: false),
                    ConvocationId = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "date", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: true),
                    DurationInMonths = table.Column<int>(type: "int", nullable: false),
                    TentativeEndDate = table.Column<DateTime>(type: "date", nullable: true),
                    RealEndDate = table.Column<DateTime>(type: "date", nullable: true),
                    ExecutionPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Projects_Convocations_ConvocationId",
                        column: x => x.ConvocationId,
                        principalTable: "Convocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Documents_InitialDocumentId",
                        column: x => x.InitialDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_Projects_Groups_ProjectGroupId",
                        column: x => x.ProjectGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                    table.ForeignKey(
                        name: "FK_Projects_ProjectStates_ProjectStateId",
                        column: x => x.ProjectStateId,
                        principalTable: "ProjectStates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProjectTypes_ProjectTypeId",
                        column: x => x.ProjectTypeId,
                        principalTable: "ProjectTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    BudgetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false),
                    FundingTypeId = table.Column<int>(type: "int", nullable: false),
                    InitialAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CertifiedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExecutedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.BudgetId);
                    table.ForeignKey(
                        name: "FK_Budgets_AppUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Budgets_FundingTypes_FundingTypeId",
                        column: x => x.FundingTypeId,
                        principalTable: "FundingTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Budgets_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ExternalResearcherProjects",
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
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_ExternalResearcherProjects_ExternalResearchers_ExternalResearcherId",
                        column: x => x.ExternalResearcherId,
                        principalTable: "ExternalResearchers",
                        principalColumn: "ExternalResearcherId");
                    table.ForeignKey(
                        name: "FK_ExternalResearcherProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ProjectExtensions",
                columns: table => new
                {
                    ProjectExtensionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectExtensionTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExtensions", x => x.ProjectExtensionId);
                    table.ForeignKey(
                        name: "FK_ProjectExtensions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_ProjectExtensions_ProjectExtensionTypes_ProjectExtensionTypeId",
                        column: x => x.ProjectExtensionTypeId,
                        principalTable: "ProjectExtensionTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectExtensions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ProjectObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ObjectiveTypeId = table.Column<int>(type: "int", nullable: false),
                    Objetive = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WeightedPercentage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectObjectives_ObjectiveTypes_ObjectiveTypeId",
                        column: x => x.ObjectiveTypeId,
                        principalTable: "ObjectiveTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectObjectives_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                });

            migrationBuilder.CreateTable(
                name: "ProjectResearchCategories",
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
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_ProjectResearchCategories_ResearchCategories_ResearchCategoryId",
                        column: x => x.ResearchCategoryId,
                        principalTable: "ResearchCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectScopes",
                columns: table => new
                {
                    ProjectScopeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ScopeTypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectScopes", x => x.ProjectScopeId);
                    table.ForeignKey(
                        name: "FK_ProjectScopes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_ProjectScopes_ScopeTypes_ScopeTypeId",
                        column: x => x.ScopeTypeId,
                        principalTable: "ScopeTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    VisitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    VisitStateId = table.Column<int>(type: "int", nullable: false),
                    AcademicPeriodId = table.Column<int>(type: "int", nullable: false),
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
                        name: "FK_Visits_AcademicPeriod_AcademicPeriodId",
                        column: x => x.AcademicPeriodId,
                        principalTable: "AcademicPeriod",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Visits_AppUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_Visits_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_Visits_Documents_FundingDocumentId",
                        column: x => x.FundingDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_Visits_Documents_ProgressDocumentId",
                        column: x => x.ProgressDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_Visits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_Visits_VisitStates_VisitStateId",
                        column: x => x.VisitStateId,
                        principalTable: "VisitStates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransactions",
                columns: table => new
                {
                    BudgetTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "int", nullable: false),
                    CertifiedByUserId = table.Column<int>(type: "int", nullable: false),
                    ExecutedByUserId = table.Column<int>(type: "int", nullable: true),
                    CertifiedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExecutedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_AppUsers_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "BudgetId");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_TransactionTypes_TransactionTypeId",
                        column: x => x.TransactionTypeId,
                        principalTable: "TransactionTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveActivities",
                columns: table => new
                {
                    ObjectiveActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<int>(type: "int", nullable: false),
                    ActivityResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveActivities", x => x.ObjectiveActivityId);
                    table.ForeignKey(
                        name: "FK_ObjectiveActivities_ProjectObjectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "ProjectObjectives",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VisitId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId");
                    table.ForeignKey(
                        name: "FK_Products_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                    table.ForeignKey(
                        name: "FK_Products_Visits_VisitId1",
                        column: x => x.VisitId1,
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateTable(
                name: "VisitIssues",
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
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_VisitIssues_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveActivityUsers",
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
                        name: "FK_ObjectiveActivityUsers_ObjectiveActivities_ObjectiveActivityId",
                        column: x => x.ObjectiveActivityId,
                        principalTable: "ObjectiveActivities",
                        principalColumn: "ObjectiveActivityId");
                    table.ForeignKey(
                        name: "FK_ObjectiveActivityUsers_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId");
                });

            migrationBuilder.CreateTable(
                name: "ProductAuthors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAuthors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAuthors_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_ProductAuthors_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductValues",
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
                        principalTable: "ProductAttributeDefinitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductValues_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_IdAsp",
                table: "AppUsers",
                column: "IdAsp");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_IdLocal",
                table: "AppUsers",
                column: "IdLocal");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ApprovedByUserId",
                table: "Budgets",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FundingTypeId",
                table: "Budgets",
                column: "FundingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ProjectId",
                table: "Budgets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_BudgetId",
                table: "BudgetTransactions",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_CertifiedByUserId",
                table: "BudgetTransactions",
                column: "CertifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_ExecutedByUserId",
                table: "BudgetTransactions",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_TransactionTypeId",
                table: "BudgetTransactions",
                column: "TransactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRuleIndexing_ConvocationRuleId_IndexingSourceId",
                table: "ConvocationRuleIndexing",
                columns: new[] { "ConvocationRuleId", "IndexingSourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRuleIndexing_IndexingSourceId",
                table: "ConvocationRuleIndexing",
                column: "IndexingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ConvocationId",
                table: "ConvocationRules",
                column: "ConvocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ConvocationId_GroupCode",
                table: "ConvocationRules",
                columns: new[] { "ConvocationId", "GroupCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ConvocationId_ProductTypeId",
                table: "ConvocationRules",
                columns: new[] { "ConvocationId", "ProductTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_IsActive",
                table: "ConvocationRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_MinDurationMonths_MaxDurationMonths",
                table: "ConvocationRules",
                columns: new[] { "MinDurationMonths", "MaxDurationMonths" });

            migrationBuilder.CreateIndex(
                name: "IX_ConvocationRules_ProductTypeId",
                table: "ConvocationRules",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Convocations_IsActive",
                table: "Convocations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedByUserId",
                table: "Documents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentTypeId",
                table: "Documents",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RelatedDocumentId",
                table: "Documents",
                column: "RelatedDocumentId",
                unique: true,
                filter: "[RelatedDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UpdatedByUserId",
                table: "Documents",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearcherProjects_CreatedByUserId",
                table: "ExternalResearcherProjects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearcherProjects_ExternalResearcherId",
                table: "ExternalResearcherProjects",
                column: "ExternalResearcherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearcherProjects_ProjectId",
                table: "ExternalResearcherProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResearchers_InstitutionId",
                table: "ExternalResearchers",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId",
                table: "GroupMembers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberRoleId",
                table: "GroupMembers",
                column: "MemberRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GroupTypeId",
                table: "Groups",
                column: "GroupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_CountryId",
                table: "Institutions",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Name_CountryId",
                table: "Institutions",
                columns: new[] { "Name", "CountryId" },
                unique: true,
                filter: "[CountryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveActivities_ObjectiveId",
                table: "ObjectiveActivities",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveActivityUsers_ObjectiveActivityId",
                table: "ObjectiveActivityUsers",
                column: "ObjectiveActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveActivityUsers_VisitId",
                table: "ObjectiveActivityUsers",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeDefinitions_ProductTypeId_AttributeName",
                table: "ProductAttributeDefinitions",
                columns: new[] { "ProductTypeId", "AttributeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAuthors_ProductId_UserId",
                table: "ProductAuthors",
                columns: new[] { "ProductId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAuthors_UserId",
                table: "ProductAuthors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId",
                table: "Products",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProjectId",
                table: "Products",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VisitId",
                table: "Products",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VisitId1",
                table: "Products",
                column: "VisitId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_Name",
                table: "ProductTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductValues_AttributeDefinitionId",
                table: "ProductValues",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductValues_ProductId_AttributeDefinitionId",
                table: "ProductValues",
                columns: new[] { "ProductId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensions_DocumentId",
                table: "ProjectExtensions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensions_ProjectExtensionTypeId",
                table: "ProjectExtensions",
                column: "ProjectExtensionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExtensions_ProjectId",
                table: "ProjectExtensions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectObjectives_ObjectiveTypeId",
                table: "ProjectObjectives",
                column: "ObjectiveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectObjectives_ProjectId",
                table: "ProjectObjectives",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResearchCategories_ProjectId_ResearchCategoryId",
                table: "ProjectResearchCategories",
                columns: new[] { "ProjectId", "ResearchCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResearchCategories_ResearchCategoryId",
                table: "ProjectResearchCategories",
                column: "ResearchCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ConvocationId",
                table: "Projects",
                column: "ConvocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedByUserId",
                table: "Projects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_InitialDocumentId",
                table: "Projects",
                column: "InitialDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectGroupId",
                table: "Projects",
                column: "ProjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectStateId",
                table: "Projects",
                column: "ProjectStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectTypeId",
                table: "Projects",
                column: "ProjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScopes_ProjectId",
                table: "ProjectScopes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScopes_ScopeTypeId",
                table: "ProjectScopes",
                column: "ScopeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCategories_ParentCategoryId",
                table: "ResearchCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCategories_ResearchCategoryTypeId",
                table: "ResearchCategories",
                column: "ResearchCategoryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCategoryTypes_ResearchCategoryGroupId",
                table: "ResearchCategoryTypes",
                column: "ResearchCategoryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFacultyScopes_IdentityUserId",
                table: "UserFacultyScopes",
                column: "IdentityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitIssues_ReportedByUserId",
                table: "VisitIssues",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitIssues_VisitId",
                table: "VisitIssues",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AcademicPeriodId",
                table: "Visits",
                column: "AcademicPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_DocumentId",
                table: "Visits",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_FundingDocumentId",
                table: "Visits",
                column: "FundingDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PerformedByUserId",
                table: "Visits",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ProgressDocumentId",
                table: "Visits",
                column: "ProgressDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ProjectId",
                table: "Visits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_VisitStateId",
                table: "Visits",
                column: "VisitStateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BudgetTransactions");

            migrationBuilder.DropTable(
                name: "ConvocationRuleIndexing");

            migrationBuilder.DropTable(
                name: "ExternalResearcherProjects");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "ObjectiveActivityUsers");

            migrationBuilder.DropTable(
                name: "ProductAuthors");

            migrationBuilder.DropTable(
                name: "ProductValues");

            migrationBuilder.DropTable(
                name: "ProjectExtensions");

            migrationBuilder.DropTable(
                name: "ProjectResearchCategories");

            migrationBuilder.DropTable(
                name: "ProjectScopes");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserFacultyScopes");

            migrationBuilder.DropTable(
                name: "VisitIssues");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropTable(
                name: "TransactionTypes");

            migrationBuilder.DropTable(
                name: "ConvocationRules");

            migrationBuilder.DropTable(
                name: "IndexingSources");

            migrationBuilder.DropTable(
                name: "ExternalResearchers");

            migrationBuilder.DropTable(
                name: "MemberRoleTypes");

            migrationBuilder.DropTable(
                name: "ObjectiveActivities");

            migrationBuilder.DropTable(
                name: "ProductAttributeDefinitions");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProjectExtensionTypes");

            migrationBuilder.DropTable(
                name: "ResearchCategories");

            migrationBuilder.DropTable(
                name: "ScopeTypes");

            migrationBuilder.DropTable(
                name: "FundingTypes");

            migrationBuilder.DropTable(
                name: "Institutions");

            migrationBuilder.DropTable(
                name: "ProjectObjectives");

            migrationBuilder.DropTable(
                name: "ProductTypes");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "ResearchCategoryTypes");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "ObjectiveTypes");

            migrationBuilder.DropTable(
                name: "AcademicPeriod");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "VisitStates");

            migrationBuilder.DropTable(
                name: "ResearchCategoryGroups");

            migrationBuilder.DropTable(
                name: "Convocations");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "ProjectStates");

            migrationBuilder.DropTable(
                name: "ProjectTypes");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "GroupTypes");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
