using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using tesisproject.shared.DTOs.Reports;
using ScottPlot;

namespace tesisproject.backend.BI.Reports
{
    public class ArticlesStatisticsModel
    {
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }

        public bool IncludeKpis { get; set; } = true;
        public bool IncludeByYear { get; set; } = true;
        public bool IncludeByField { get; set; } = true;
        public bool IncludeByResearchLine { get; set; } = true;
        public bool IncludeQuartiles { get; set; } = true;

        public ArticlesQuartileStatsDto? QuartileStats { get; set; }

        public ArticlesKpiSummaryDto Kpis { get; set; } = default!;
        public List<ArticlesByYearDto> ByYear { get; set; } = new();
        public List<ArticlesByFieldDto> ByField { get; set; } = new();
        public List<ArticlesByResearchLineDto> ByResearchLine { get; set; } = new();
    }

    public class ArticlesStatisticsDocument : IDocument
    {
        private readonly ArticlesStatisticsModel _model;
        private readonly string? _headerImagePath;
        private readonly string? _logoPath;

        // Imágenes de gráficas generadas en memoria
        private readonly byte[]? _chartByYear;
        private readonly byte[]? _chartByField;
        private readonly byte[]? _chartByResearchLine;
        private readonly byte[]? _chartQuartiles;

        public ArticlesStatisticsDocument(
            ArticlesStatisticsModel model,
            string? headerImagePath,
            string? logoPath)
        {
            _model = model;
            _headerImagePath = headerImagePath;
            _logoPath = logoPath;

            // Generamos las gráficas como imágenes
            _chartByYear = BuildYearChart();
            _chartByField = BuildFieldChart();
            _chartByResearchLine = BuildResearchLineChart();
            _chartQuartiles = BuildQuartileChart();
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = "Reporte estadístico de artículos",
            Author = "Sistema de reportería DIDE",
            Subject = "Producción científica",
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                // 🔤 Fuente global: Times New Roman, 12
                page.DefaultTextStyle(TextStyle
                    .Default
                    .FontFamily("Times New Roman")
                    .FontSize(12));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        // ---------------------------------------------------------------------
        // HEADER
        // ---------------------------------------------------------------------
        private void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                // 1) Imagen de encabezado (banner horizontal)
                if (!string.IsNullOrEmpty(_headerImagePath) && File.Exists(_headerImagePath))
                {
                    col.Item()
                       .AlignCenter()
                       .Image(_headerImagePath)
                       .FitWidth();

                    // pequeño espacio
                    col.Item().PaddingTop(5).Text("");
                }

                // 2) Fila con logo + textos
                col.Item().Row(row =>
                {
                    // Logo a la izquierda
                    row.ConstantItem(70).Element(c =>
                    {
                        if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                        {
                            c.AlignLeft()
                             .AlignMiddle()
                             .Image(_logoPath)
                             .FitWidth();
                        }
                    });

                    // Texto institucional
                    row.RelativeItem().Column(textCol =>
                    {
                        textCol.Item().Text("UNIVERSIDAD TÉCNICA DE AMBATO")
                            .FontSize(12)
                            .SemiBold();

                        textCol.Item().Text("Dirección de Investigación y Desarrollo (DIDE)")
                            .FontSize(11);

                        textCol.Item().Text("Reporte estadístico de publicaciones científicas")
                            .FontSize(12)
                            .SemiBold();

                        textCol.Item().Text(t =>
                        {
                            t.Span("Generado: ").SemiBold().FontSize(10);
                            t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10);
                        });

                        if (_model.MinYear.HasValue || _model.MaxYear.HasValue)
                        {
                            textCol.Item().Text(t =>
                            {
                                t.Span("Rango de años: ").SemiBold().FontSize(10);
                                t.Span($"{_model.MinYear?.ToString() ?? "min"} - {_model.MaxYear?.ToString() ?? "max"}")
                                 .FontSize(10);
                            });
                        }
                    });
                });

                // Separador bajo el encabezado
                col.Item()
                   .PaddingTop(5)
                   .BorderBottom(1)
                   .BorderColor(Colors.Grey.Lighten2)
                   .Text("");
            });
        }

        // ---------------------------------------------------------------------
        // FOOTER
        // ---------------------------------------------------------------------
        private void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().Text(t =>
                {
                    t.Span("DIDE - Universidad Técnica de Ambato").FontSize(9);
                });

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Página ").FontSize(9);
                    x.CurrentPageNumber().FontSize(9);
                    x.Span(" de ").FontSize(9);
                    x.TotalPages().FontSize(9);
                });
            });
        }

        // ---------------------------------------------------------------------
        // CONTENT
        // ---------------------------------------------------------------------
        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(10).Column(content =>
            {
                // ---------- Resumen ejecutivo ----------
                content.Item().PaddingBottom(10).Element(c =>
                {
                    c.Text("Resumen ejecutivo")
                     .FontSize(13)
                     .SemiBold();
                });

                content.Item().PaddingBottom(10).Element(c =>
                {
                    c.DefaultTextStyle(x => x.FontSize(11))
                     .Text(t =>
                     {
                         t.Span("Este reporte presenta un análisis estadístico de la producción científica ");
                         t.Span("registrada en el sistema de gestión de artículos de la DIDE, ");
                         t.Span("incluyendo indicadores clave, distribución temporal, áreas de conocimiento ");
                         t.Span("y líneas de investigación involucradas.");
                     });
                });

                // ---------- 1. KPIs ----------
                if (_model.IncludeKpis)
                {
                    content.Item().PaddingTop(8).PaddingBottom(4).Element(c =>
                    {
                        c.Text("1. Indicadores clave de producción")
                         .FontSize(12)
                         .SemiBold();
                    });

                    content.Item().PaddingBottom(6).Element(c =>
                    {
                        c.DefaultTextStyle(x => x.FontSize(10))
                         .Text(t =>
                         {
                             t.Span("En esta sección se presentan los principales indicadores globales: total de artículos, ");
                             t.Span("publicaciones en el año actual, artículos en cuartiles Q1–Q2 y número de artículos indexados en Scopus.");
                         });
                    });

                    content.Item().Element(cont =>
                    {
                        cont.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(BodyCell).Text($"Total artículos: {_model.Kpis.TotalArticles}");
                            table.Cell().Element(BodyCell).Text($"Publicados este año: {_model.Kpis.PublishedThisYear}");
                            table.Cell().Element(BodyCell).Text($"Q1 + Q2: {_model.Kpis.Q1Q2Count}");
                            table.Cell().Element(BodyCell).Text($"OA: {_model.Kpis.OpenAccessCount} / Scopus: {_model.Kpis.IndexedInScopusCount}");
                        });
                    });

                    // Espacio después de la sección
                    content.Item().PaddingTop(10).Text("");
                }

                // ---------- 2. Distribución por cuartiles de calidad ----------
                if (_model.IncludeQuartiles &&
                    _chartQuartiles != null &&
                    _model.QuartileStats != null &&
                    _model.QuartileStats.TotalArticles > 0)
                {
                    var s = _model.QuartileStats;
                    double total = s.TotalArticles;

                    double P(int count) =>
                        total == 0 ? 0 : Math.Round(count / total * 100.0, 2);

                    content.Item().PaddingTop(8).PaddingBottom(4).Element(c =>
                    {
                        c.Text("2. Distribución por cuartiles de calidad")
                         .FontSize(12)
                         .SemiBold();
                    });

                    content.Item().PaddingBottom(4).Element(c =>
                    {
                        c.DefaultTextStyle(x => x.FontSize(10))
                         .Text(t =>
                         {
                             t.Span("La siguiente gráfica de dona resume la distribución de artículos según su cuartil de calidad (Q1–Q4), ");
                             t.Span("incluyendo aquellos sin clasificación, permitiendo apreciar el peso relativo de cada grupo sobre el total.");
                         });
                    });

                    // Gráfico de dona de cuartiles
                    content.Item().PaddingBottom(6).Element(c =>
                    {
                        c.Padding(5)
                         .AlignCenter()
                         .Width(320)
                         .Border(1)
                         .BorderColor(Colors.Grey.Lighten2)
                         .Image(_chartQuartiles)
                         .FitWidth();
                    });

                    // Tabla de cuartiles
                    content.Item().Element(cont =>
                    {
                        cont.Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();   // Cuartil
                                cols.ConstantColumn(70); // Artículos
                                cols.ConstantColumn(90); // %
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Cuartil");
                                header.Cell().Element(HeaderCell).Text("Artículos");
                                header.Cell().Element(HeaderCell).Text("% sobre total");
                            });

                            table.Cell().Element(BodyCell).Text("Q1");
                            table.Cell().Element(BodyCell).Text(s.Q1Count.ToString());
                            table.Cell().Element(BodyCell).Text($"{P(s.Q1Count):0.##}%");

                            table.Cell().Element(BodyCell).Text("Q2");
                            table.Cell().Element(BodyCell).Text(s.Q2Count.ToString());
                            table.Cell().Element(BodyCell).Text($"{P(s.Q2Count):0.##}%");

                            table.Cell().Element(BodyCell).Text("Q3");
                            table.Cell().Element(BodyCell).Text(s.Q3Count.ToString());
                            table.Cell().Element(BodyCell).Text($"{P(s.Q3Count):0.##}%");

                            table.Cell().Element(BodyCell).Text("Q4");
                            table.Cell().Element(BodyCell).Text(s.Q4Count.ToString());
                            table.Cell().Element(BodyCell).Text($"{P(s.Q4Count):0.##}%");

                            table.Cell().Element(BodyCell).Text("Sin cuartil");
                            table.Cell().Element(BodyCell).Text(s.NoQuartileCount.ToString());
                            table.Cell().Element(BodyCell).Text($"{P(s.NoQuartileCount):0.##}%");
                        });
                    });

                    content.Item().PaddingTop(10).Text("");
                }

                // ---------- 3. Distribución anual ----------
                if (_model.IncludeByYear && _model.ByYear.Any())
                {
                    content.Item().PaddingTop(8).PaddingBottom(4).Element(c =>
                    {
                        c.Text("3. Distribución anual de artículos")
                         .FontSize(12)
                         .SemiBold();
                    });

                    content.Item().PaddingBottom(4).Element(c =>
                    {
                        c.DefaultTextStyle(x => x.FontSize(10))
                         .Text(t =>
                         {
                             t.Span("La siguiente gráfica y tabla resumen el número de artículos registrados ");
                             t.Span("por año de creación en el sistema, permitiendo observar tendencias de crecimiento o disminución.");
                         });
                    });

                    // Gráfica por año (más pequeña, centrada, con borde)
                    if (_chartByYear != null)
                    {
                        content.Item().PaddingBottom(6).Element(c =>
                        {
                            c.Padding(5)
                             .AlignCenter()
                             .Width(360)
                             .Border(1)
                             .BorderColor(Colors.Grey.Lighten2)
                             .Image(_chartByYear)
                             .FitWidth();
                        });
                    }

                    // Tabla por año
                    content.Item().Element(cont =>
                    {
                        cont.Table(table =>
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

                            foreach (var item in _model.ByYear.OrderBy(x => x.Year))
                            {
                                table.Cell().Element(BodyCell).Text(item.Year.ToString());
                                table.Cell().Element(BodyCell).Text(item.Count.ToString());
                            }
                        });
                    });

                    content.Item().PaddingTop(10).Text("");
                }

                // ---------- 4. Áreas de conocimiento ----------
                if (_model.IncludeByField && _model.ByField.Any())
                {
                    content.Item().PaddingTop(8).PaddingBottom(4).Element(c =>
                    {
                        c.Text("4. Distribución por área de conocimiento (Top 30)")
                         .FontSize(12)
                         .SemiBold();
                    });

                    content.Item().PaddingBottom(4).Element(c =>
                    {
                        c.DefaultTextStyle(x => x.FontSize(10))
                         .Text(t =>
                         {
                             t.Span("Se muestran las áreas amplias, específicas y detalladas con mayor número de artículos, ");
                             t.Span("lo que permite identificar focos estratégicos de investigación dentro de la institución.");
                         });
                    });

                    if (_chartByField != null)
                    {
                        content.Item().PaddingBottom(6).Element(c =>
                        {
                            c.Padding(5)
                             .AlignCenter()
                             .Width(360)
                             .Border(1)
                             .BorderColor(Colors.Grey.Lighten2)
                             .Image(_chartByField)
                             .FitWidth();
                        });
                    }

                    content.Item().Element(cont =>
                    {
                        cont.Table(table =>
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

                            foreach (var item in _model.ByField)
                            {
                                table.Cell().Element(BodyCell).Text(item.BroadFieldName ?? "-");
                                table.Cell().Element(BodyCell).Text(item.SpecificFieldName ?? "-");
                                table.Cell().Element(BodyCell).Text(item.DetailedFieldName ?? "-");
                                table.Cell().Element(BodyCell).Text(item.Count.ToString());
                            }
                        });
                    });

                    content.Item().PaddingTop(10).Text("");
                }

                // ---------- 5. Líneas de investigación ----------
                if (_model.IncludeByResearchLine && _model.ByResearchLine.Any())
                {
                    content.Item().PaddingTop(8).PaddingBottom(4).Element(c =>
                    {
                        c.Text("5. Distribución por línea de investigación (Top 30)")
                         .FontSize(12)
                         .SemiBold();
                    });

                    content.Item().PaddingBottom(4).Element(c =>
                    {
                        c.DefaultTextStyle(x => x.FontSize(10))
                         .Text(t =>
                         {
                             t.Span("En esta sección se identifican las líneas de investigación con mayor volumen de artículos, ");
                             t.Span("insumo clave para la planificación y priorización de recursos académicos y de investigación.");
                         });
                    });

                    if (_chartByResearchLine != null)
                    {
                        content.Item().PaddingBottom(6).Element(c =>
                        {
                            c.Padding(5)
                             .AlignCenter()
                             .Width(360)
                             .Border(1)
                             .BorderColor(Colors.Grey.Lighten2)
                             .Image(_chartByResearchLine)
                             .FitWidth();
                        });
                    }

                    content.Item().Element(cont =>
                    {
                        cont.Table(table =>
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

                            foreach (var item in _model.ByResearchLine)
                            {
                                table.Cell().Element(BodyCell).Text(item.ResearchLineName);
                                table.Cell().Element(BodyCell).Text(item.Count.ToString());
                            }
                        });
                    });
                }
            });
        }

        // ---------------------------------------------------------------------
        // Helpers de estilo
        // ---------------------------------------------------------------------
        private static IContainer HeaderCell(IContainer container) =>
            container.DefaultTextStyle(x => x.FontSize(10).SemiBold())
                     .PaddingVertical(2)
                     .BorderBottom(1)
                     .BorderColor(Colors.Grey.Lighten2);

        private static IContainer BodyCell(IContainer container) =>
            container.DefaultTextStyle(x => x.FontSize(10))
                     .PaddingVertical(1);

        // ---------------------------------------------------------------------
        // Construcción de gráficas con ScottPlot
        // ---------------------------------------------------------------------
        private byte[]? BuildYearChart()
        {
            if (!_model.IncludeByYear || _model.ByYear == null || !_model.ByYear.Any())
                return null;

            var ordered = _model.ByYear.OrderBy(x => x.Year).ToList();

            double[] values = ordered.Select(x => (double)x.Count).ToArray();
            string[] labels = ordered.Select(x => x.Year.ToString()).ToArray();
            double[] positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();

            var plt = new ScottPlot.Plot(600, 260);
            var bar = plt.AddBar(values, positions);
            bar.FillColor = System.Drawing.Color.FromArgb(0x7A, 0x1E, 0x19);

            plt.XTicks(positions, labels);
            plt.XLabel("Año");
            plt.YLabel("Número de artículos");
            plt.SetAxisLimits(yMin: 0);

            var tempFile = Path.Combine(Path.GetTempPath(), $"chart-year-{Guid.NewGuid()}.png");
            plt.SaveFig(tempFile);

            var bytes = File.ReadAllBytes(tempFile);
            File.Delete(tempFile);

            return bytes;
        }

        private byte[]? BuildFieldChart()
        {
            if (!_model.IncludeByField || _model.ByField == null || !_model.ByField.Any())
                return null;

            // Agrupamos por área amplia
            var groups = _model.ByField
                .GroupBy(f => string.IsNullOrWhiteSpace(f.BroadFieldName) ? "Sin área" : f.BroadFieldName!)
                .Select(g => new { Name = g.Key, Count = g.Sum(x => x.Count) })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            if (!groups.Any())
                return null;

            double[] values = groups.Select(x => (double)x.Count).ToArray();
            string[] labels = groups.Select(x => x.Name).ToArray();
            double[] positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();

            var plt = new ScottPlot.Plot(600, 260);
            var bar = plt.AddBar(values, positions);
            bar.FillColor = System.Drawing.Color.FromArgb(0x9F, 0x3B, 0x31);

            plt.XTicks(positions, labels);
            plt.XLabel("Área amplia");
            plt.YLabel("Número de artículos");
            plt.SetAxisLimits(yMin: 0);
            plt.Layout(bottom: 80); // espacio para labels largos

            var tempFile = Path.Combine(Path.GetTempPath(), $"chart-field-{Guid.NewGuid()}.png");
            plt.SaveFig(tempFile);

            var bytes = File.ReadAllBytes(tempFile);
            File.Delete(tempFile);

            return bytes;
        }

        private byte[]? BuildResearchLineChart()
        {
            if (!_model.IncludeByResearchLine || _model.ByResearchLine == null || !_model.ByResearchLine.Any())
                return null;

            var groups = _model.ByResearchLine
                .GroupBy(r => string.IsNullOrWhiteSpace(r.ResearchLineName) ? "Sin línea" : r.ResearchLineName!)
                .Select(g => new { Name = g.Key, Count = g.Sum(x => x.Count) })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            if (!groups.Any())
                return null;

            double[] values = groups.Select(x => (double)x.Count).ToArray();
            string[] labels = groups.Select(x => x.Name).ToArray();
            double[] positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();

            var plt = new ScottPlot.Plot(620, 300);
            var bar = plt.AddBar(values, positions);
            bar.FillColor = System.Drawing.Color.FromArgb(0xC2, 0x5B, 0x4A);

            plt.YTicks(positions, labels);
            plt.YLabel("Línea de investigación");
            plt.XLabel("Número de artículos");
            plt.SetAxisLimits(xMin: 0);
            plt.Layout(left: 200); // espacio para labels en Y

            var tempFile = Path.Combine(Path.GetTempPath(), $"chart-research-{Guid.NewGuid()}.png");
            plt.SaveFig(tempFile);

            var bytes = File.ReadAllBytes(tempFile);
            File.Delete(tempFile);

            return bytes;
        }

        private byte[]? BuildQuartileChart()
        {
            if (!_model.IncludeQuartiles || _model.QuartileStats == null || _model.QuartileStats.TotalArticles <= 0)
                return null;

            var s = _model.QuartileStats;

            var labels = new List<string>();
            var values = new List<double>();

            void AddSlice(string label, int count)
            {
                if (count > 0)
                {
                    labels.Add(label);
                    values.Add(count);
                }
            }

            AddSlice("Q1", s.Q1Count);
            AddSlice("Q2", s.Q2Count);
            AddSlice("Q3", s.Q3Count);
            AddSlice("Q4", s.Q4Count);
            AddSlice("Sin cuartil", s.NoQuartileCount);

            if (values.Count == 0)
                return null;

            var plt = new ScottPlot.Plot(500, 260);
            var pie = plt.AddPie(values.ToArray());
            pie.SliceLabels = labels.ToArray();
            pie.ShowLabels = true;
            pie.DonutSize = 0.5; // dona

            var tempFile = Path.Combine(Path.GetTempPath(), $"chart-quartiles-{Guid.NewGuid()}.png");
            plt.SaveFig(tempFile);

            var bytes = File.ReadAllBytes(tempFile);
            File.Delete(tempFile);

            return bytes;
        }
    }
}
