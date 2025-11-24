using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using tesisproject.backend.DataWarehouse;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.BI.Reports
{
    [ApiController]
    [Route("api/reports")]
    //[Authorize]
    [AllowAnonymous]
    public class ReportsController : ControllerBase
    {
        // Constructor estático (QuestPDF)
        static ReportsController()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private readonly DwDbContext _dw;

        public ReportsController(DwDbContext dw)
        {
            _dw = dw;
        }

        // =====================================================================
        // 1) ENDPOINTS JSON PARA EL FRONTEND
        // =====================================================================

        [HttpGet("articles/by-year")]
        public async Task<ActionResult<IEnumerable<ArticlesByYearDto>>> GetArticlesByYear(CancellationToken ct)
        {
            var query = await _dw.FactArticlePublications
                .Where(f => f.CreatedDateKey != null)
                .GroupBy(f => f.CreatedDate!.Year)
                .Select(g => new ArticlesByYearDto
                {
                    Year = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderBy(x => x.Year)
                .ToListAsync(ct);

            return Ok(query);
        }

        [HttpGet("articles/by-field")]
        public async Task<ActionResult<IEnumerable<ArticlesByFieldDto>>> GetArticlesByField(CancellationToken ct)
        {
            var query = await _dw.FactArticlePublications
                .Where(f => f.FieldKey != null)
                .GroupBy(f => new
                {
                    f.Field!.BroadFieldName,
                    f.Field!.SpecificFieldName,
                    f.Field!.DetailedFieldName
                })
                .Select(g => new ArticlesByFieldDto
                {
                    BroadFieldName = g.Key.BroadFieldName,
                    SpecificFieldName = g.Key.SpecificFieldName,
                    DetailedFieldName = g.Key.DetailedFieldName,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            return Ok(query);
        }

        [HttpGet("articles/by-research-line")]
        public async Task<ActionResult<IEnumerable<ArticlesByResearchLineDto>>> GetArticlesByResearchLine(
            CancellationToken ct)
        {
            var query = await _dw.FactArticlePublications
                .Where(f => f.ResearchLineKey != null)
                .GroupBy(f => f.ResearchLine!.Name)
                .Select(g => new ArticlesByResearchLineDto
                {
                    ResearchLineName = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            return Ok(query);
        }

        [HttpGet("articles/kpis/summary")]
        public async Task<ActionResult<ArticlesKpiSummaryDto>> GetArticlesKpiSummary(CancellationToken ct)
        {
            var nowYear = DateTime.UtcNow.Year;
            var facts = _dw.FactArticlePublications.AsQueryable();

            var totalArticles = await facts.CountAsync(ct);

            var publishedThisYear = await facts
                .Where(f => f.PublicationDate != null && f.PublicationDate!.Year == nowYear)
                .CountAsync(ct);

            var q1q2Count = await facts
                .Where(f => f.Quartile == "Q1" || f.Quartile == "Q2")
                .CountAsync(ct);

            var openAccessCount = await facts
                .Where(f => f.IsOpenAccess)
                .CountAsync(ct);

            var scopusIndexed = await _dw.FactArticleIndexings
                .Where(fi => fi.IndexingSource!.Name == "Scopus")
                .Select(fi => fi.ArticleKey)
                .Distinct()
                .CountAsync(ct);

            var dto = new ArticlesKpiSummaryDto
            {
                TotalArticles = totalArticles,
                PublishedThisYear = publishedThisYear,
                Q1Q2Count = q1q2Count,
                OpenAccessCount = openAccessCount,
                IndexedInScopusCount = scopusIndexed
            };

            return Ok(dto);
        }

        // =====================================================================
        // 2) ENDPOINT PDF
        // =====================================================================

        [HttpGet("articles/statistics/pdf")]
        public async Task<IActionResult> GetStatisticsPdf(
            [FromQuery] int? minYear,
            [FromQuery] int? maxYear,
            CancellationToken ct)
        {
            var nowYear = DateTime.UtcNow.Year;

            // Base query filtrada
            var facts = _dw.FactArticlePublications.AsQueryable();

            if (minYear.HasValue || maxYear.HasValue)
            {
                var dates = _dw.DimDates.AsQueryable();

                var min = minYear ?? dates.Min(d => d.Year);
                var max = maxYear ?? dates.Max(d => d.Year);

                facts = facts
                    .Where(f => f.CreatedDateKey != null)
                    .Where(f =>
                        f.CreatedDate!.Year >= min &&
                        f.CreatedDate!.Year <= max);
            }

            // ----------------- KPIs ------------------
            var totalArticles = await facts.CountAsync(ct);

            var publishedThisYear = await facts
                .Where(f => f.PublicationDate != null && f.PublicationDate!.Year == nowYear)
                .CountAsync(ct);

            var q1q2Count = await facts
                .Where(f => f.Quartile == "Q1" || f.Quartile == "Q2")
                .CountAsync(ct);

            var openAccessCount = await facts
                .Where(f => f.IsOpenAccess)
                .CountAsync(ct);

            var scopusIndexed = await _dw.FactArticleIndexings
                .Where(fi => fi.IndexingSource!.Name == "Scopus")
                .Select(fi => fi.ArticleKey)
                .Distinct()
                .CountAsync(ct);

            var kpis = new ArticlesKpiSummaryDto
            {
                TotalArticles = totalArticles,
                PublishedThisYear = publishedThisYear,
                Q1Q2Count = q1q2Count,
                OpenAccessCount = openAccessCount,
                IndexedInScopusCount = scopusIndexed
            };

            // ----------------- Distribución por año ------------------
            var byYear = await facts
                .Where(f => f.CreatedDateKey != null)
                .GroupBy(f => f.CreatedDate!.Year)
                .Select(g => new ArticlesByYearDto
                {
                    Year = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderBy(x => x.Year)
                .ToListAsync(ct);

            // ----------------- Áreas ------------------
            var byField = await facts
                .Where(f => f.FieldKey != null)
                .GroupBy(f => new
                {
                    f.Field!.BroadFieldName,
                    f.Field!.SpecificFieldName,
                    f.Field!.DetailedFieldName
                })
                .Select(g => new ArticlesByFieldDto
                {
                    BroadFieldName = g.Key.BroadFieldName,
                    SpecificFieldName = g.Key.SpecificFieldName,
                    DetailedFieldName = g.Key.DetailedFieldName,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.Count)
                .Take(30)
                .ToListAsync(ct);

            // ----------------- Líneas de investigación ------------------
            var byResearchLine = await facts
                .Where(f => f.ResearchLineKey != null)
                .GroupBy(f => f.ResearchLine!.Name)
                .Select(g => new ArticlesByResearchLineDto
                {
                    ResearchLineName = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.Count)
                .Take(30)
                .ToListAsync(ct);

            // =====================================================================
            //   GENERAR PDF
            // =====================================================================

            var pdfBytes = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    // -------- Header --------
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Reportes estadísticos de artículos")
                                .FontSize(18)
                                .SemiBold();

                            col.Item().Text(text =>
                            {
                                text.Span("Generado: ").SemiBold();
                                text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                            }).FontSize(10);

                            if (minYear.HasValue || maxYear.HasValue)
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span("Rango de años: ").SemiBold();
                                    text.Span($"{minYear?.ToString() ?? "min"} - {maxYear?.ToString() ?? "max"}");
                                }).FontSize(10);
                            }
                        });
                    });

                    // -------- Content --------
                    page.Content().Column(content =>
                    {
                        // ----- TITULO KPI -----
                        content.Item().Element(c =>
                        {
                            var box = c
                                .PaddingBottom(10)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2);

                            box.Text("Resumen de indicadores clave")
                                .FontSize(12)
                                .SemiBold();
                        });

                        // ----- TABLA KPI -----
                        content.Item().Element(container =>
                        {
                            container.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Element(BodyCell).Text($"Total artículos: {kpis.TotalArticles}");
                                table.Cell().Element(BodyCell).Text($"Publicados este año: {kpis.PublishedThisYear}");
                                table.Cell().Element(BodyCell).Text($"Q1 + Q2: {kpis.Q1Q2Count}");
                                table.Cell().Element(BodyCell).Text($"OA: {kpis.OpenAccessCount} / Scopus: {kpis.IndexedInScopusCount}");
                            });
                        });

                        // ---------- Distribución por Año ----------
                        if (byYear.Any())
                        {
                            content.Item().Element(c =>
                            {
                                c.PaddingTop(15)
                                 .Text("Distribución por año")
                                 .FontSize(12)
                                 .SemiBold();
                            });

                            content.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(60);
                                        cols.RelativeColumn();
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCell).Text("Año");
                                        header.Cell().Element(HeaderCell).Text("Artículos");
                                    });

                                    foreach (var item in byYear)
                                    {
                                        table.Cell().Element(BodyCell).Text(item.Year.ToString());
                                        table.Cell().Element(BodyCell).Text(item.Count.ToString());
                                    }
                                });
                            });
                        }

                        // ---------- Distribución por Área ----------
                        if (byField.Any())
                        {
                            content.Item().Element(c =>
                            {
                                c.PaddingTop(15)
                                 .Text("Distribución por área de conocimiento (Top 30)")
                                 .FontSize(12)
                                 .SemiBold();
                            });

                            content.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.ConstantColumn(50);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCell).Text("Área amplia");
                                        header.Cell().Element(HeaderCell).Text("Área específica");
                                        header.Cell().Element(HeaderCell).Text("Área detallada");
                                        header.Cell().Element(HeaderCell).Text("Art.");
                                    });

                                    foreach (var item in byField)
                                    {
                                        table.Cell().Element(BodyCell).Text(item.BroadFieldName ?? "-");
                                        table.Cell().Element(BodyCell).Text(item.SpecificFieldName ?? "-");
                                        table.Cell().Element(BodyCell).Text(item.DetailedFieldName ?? "-");
                                        table.Cell().Element(BodyCell).Text(item.Count.ToString());
                                    }
                                });
                            });
                        }

                        // ---------- Distribución por Línea ----------
                        if (byResearchLine.Any())
                        {
                            content.Item().Element(c =>
                            {
                                c.PaddingTop(15)
                                 .Text("Distribución por línea de investigación (Top 30)")
                                 .FontSize(12)
                                 .SemiBold();
                            });

                            content.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.ConstantColumn(50);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCell).Text("Línea de investigación");
                                        header.Cell().Element(HeaderCell).Text("Art.");
                                    });

                                    foreach (var item in byResearchLine)
                                    {
                                        table.Cell().Element(BodyCell).Text(item.ResearchLineName);
                                        table.Cell().Element(BodyCell).Text(item.Count.ToString());
                                    }
                                });
                            });
                        }
                    });

                    // -------- Footer --------
                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Página ").FontSize(9);
                        x.CurrentPageNumber().FontSize(9);
                        x.Span(" de ").FontSize(9);
                        x.TotalPages().FontSize(9);
                    });
                });

                // Helpers
                static IContainer HeaderCell(IContainer container) =>
                    container.DefaultTextStyle(x => x.FontSize(9).SemiBold())
                             .PaddingVertical(2)
                             .BorderBottom(1)
                             .BorderColor(Colors.Grey.Lighten2);

                static IContainer BodyCell(IContainer container) =>
                    container.DefaultTextStyle(x => x.FontSize(9))
                             .PaddingVertical(1);
            })
            .GeneratePdf();

            const string fileName = "Reporte_Estadistico_Articulos.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}