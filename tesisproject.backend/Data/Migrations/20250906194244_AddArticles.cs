using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodoAcademico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Anio = table.Column<int>(type: "int", nullable: true),
                    CodigoPublicacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CodigoISSN = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NombreRevista = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    VolumenRevista = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NumeroRevista = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NumeroPaginas = table.Column<int>(type: "int", nullable: true),
                    SJR = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaseDatos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CampoAmplio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CampoEspecifico = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CampoDetallado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quartil = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Filiacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccesoAbierto = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LinkPublicacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EnlaceRevista = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CodigoProyectoArticulado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProyectoArticulado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LineaInvestigacionArticulada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrupoInvestigacionArticulado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
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
                    Nombre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Participacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_ArticleParticipants_ArticleId_Index",
                table: "ArticleParticipants",
                columns: new[] { "ArticleId", "Index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleParticipants");

            migrationBuilder.DropTable(
                name: "Articles");
        }
    }
}
