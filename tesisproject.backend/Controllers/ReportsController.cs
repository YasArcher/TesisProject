using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
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
        static ReportsController()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private readonly DwDbContext _dw;
        private readonly IWebHostEnvironment _env;

        public ReportsController(DwDbContext dw, IWebHostEnvironment env)
        {
            _dw = dw;
            _env = env;
        }

        // =====================================================================
        // 1) ENDPOINTS JSON BÁSICOS (compatibilidad)
        // =====================================================================

        [HttpGet("articles/by-year")]
        public async Task<ActionResult<IEnumerable<ArticlesByYearDto>>> GetArticlesByYear(CancellationToken ct)
        {
            var query = await _dw.FactArticlePublications
                .Where(f => f.CreatedDate != null)
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

            var totalArticles = await facts
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var publishedThisYear = await facts
                .Where(f => f.PublicationDate != null && f.PublicationDate!.Year == nowYear)
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var q1q2Count = await facts
                .Where(f => f.Quartile == "Q1" || f.Quartile == "Q2")
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var openAccessCount = await facts
                .Where(f => f.IsOpenAccess)
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

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

        [HttpGet("articles/quality/quartiles")]
        public async Task<ActionResult<ArticlesQuartileStatsDto>> GetQuartileStats(CancellationToken ct)
        {
            var facts = _dw.FactArticlePublications.AsQueryable();

            var groups = await facts
                .GroupBy(f => f.Quartile)
                .Select(g => new
                {
                    Quartile = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .ToListAsync(ct);

            var total = groups.Sum(x => x.Count);

            int q1 = groups.FirstOrDefault(x => x.Quartile == "Q1")?.Count ?? 0;
            int q2 = groups.FirstOrDefault(x => x.Quartile == "Q2")?.Count ?? 0;
            int q3 = groups.FirstOrDefault(x => x.Quartile == "Q3")?.Count ?? 0;
            int q4 = groups.FirstOrDefault(x => x.Quartile == "Q4")?.Count ?? 0;

            int noQuartile = groups
                .Where(x => string.IsNullOrEmpty(x.Quartile) || x.Quartile == "N/A")
                .Sum(x => x.Count);

            double Percent(double value) =>
                total == 0 ? 0 : Math.Round((value / total) * 100.0, 2);

            var dto = new ArticlesQuartileStatsDto
            {
                TotalArticles = total,
                Q1Count = q1,
                Q2Count = q2,
                Q3Count = q3,
                Q4Count = q4,
                NoQuartileCount = noQuartile,
                Q1Q2Percent = Percent(q1 + q2),
                Q3Q4Percent = Percent(q3 + q4),
                NoQuartilePercent = Percent(noQuartile)
            };

            return Ok(dto);
        }

        [HttpGet("articles/efficiency/time-to-publication")]
        public async Task<ActionResult<ArticlesTimeToPublicationStatsDto>> GetTimeToPublicationStats(
            CancellationToken ct)
        {
            var facts = _dw.FactArticlePublications.AsQueryable();

            var diffs = await facts
                .Where(f => f.CreatedDate != null && f.PublicationDate != null)
                .Select(f => EF.Functions.DateDiffDay(
                    f.CreatedDate!.Date,
                    f.PublicationDate!.Date
                ))
                .Where(days => days >= 0)
                .Cast<int>()
                .ToListAsync(ct);

            if (diffs.Count == 0)
            {
                return Ok(new ArticlesTimeToPublicationStatsDto
                {
                    ArticlesWithDatesCount = 0
                });
            }

            diffs.Sort();

            int n = diffs.Count;
            double avg = diffs.Average();
            int min = diffs.First();
            int max = diffs.Last();

            int Percentile(double p)
            {
                if (n == 0) return 0;
                var index = (int)Math.Round((p / 100.0) * (n - 1));
                if (index < 0) index = 0;
                if (index >= n) index = n - 1;
                return diffs[index];
            }

            int p50 = Percentile(50);
            int p75 = Percentile(75);
            int p90 = Percentile(90);

            int bucket0To180 = diffs.Count(d => d <= 180);
            int bucket181To365 = diffs.Count(d => d > 180 && d <= 365);
            int bucketMore365 = diffs.Count(d => d > 365);

            var dto = new ArticlesTimeToPublicationStatsDto
            {
                ArticlesWithDatesCount = n,
                AverageDays = Math.Round(avg, 2),
                MinDays = min,
                MaxDays = max,
                P50 = p50,
                P75 = p75,
                P90 = p90,
                Bucket0To180 = bucket0To180,
                Bucket181To365 = bucket181To365,
                BucketMore365 = bucketMore365
            };

            return Ok(dto);
        }

        [HttpGet("articles/access/openaccess-indexing")]
        public async Task<ActionResult<ArticlesOpenAccessIndexingStatsDto>> GetOpenAccessIndexingStats(
            CancellationToken ct)
        {
            var facts = _dw.FactArticlePublications.AsQueryable();

            var total = await facts
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var openAccess = await facts
                .Where(f => f.IsOpenAccess)
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var scopusIndexed = await _dw.FactArticleIndexings
                .Where(fi => fi.IndexingSource!.Name == "Scopus")
                .Select(fi => fi.ArticleKey)
                .Distinct()
                .CountAsync(ct);

            double Percent(int value) =>
                total == 0 ? 0 : Math.Round((value / (double)total) * 100.0, 2);

            var dto = new ArticlesOpenAccessIndexingStatsDto
            {
                TotalArticles = total,
                OpenAccessCount = openAccess,
                OpenAccessPercent = Percent(openAccess),
                ScopusIndexedCount = scopusIndexed,
                ScopusIndexedPercent = Percent(scopusIndexed)
            };

            return Ok(dto);
        }

        // =====================================================================
        // 1.b) ENDPOINT UNIFICADO DE DASHBOARD CON FILTROS (tipo Power BI)
        // =====================================================================

        [HttpPost("articles/dashboard")]
        public async Task<ActionResult<ArticlesDashboardDto>> GetArticlesDashboard(
            [FromBody] ArticlesDashboardFilterDto filter,
            CancellationToken ct)
        {
            // Punto de partida: todos los hechos de publicaciones
            var facts = _dw.FactArticlePublications.AsQueryable();

            // ---------------------------
            // 1) Filtros por fecha
            // ---------------------------

            // CreatedFrom
            if (filter.CreatedFrom.HasValue)
            {
                var from = filter.CreatedFrom.Value.Date;

                facts = facts.Where(f =>
                    f.CreatedDate != null &&
                    f.CreatedDate.Date >= from   // 👈 DimDate.Date vs DateTime
                );
            }

            // CreatedTo
            if (filter.CreatedTo.HasValue)
            {
                var to = filter.CreatedTo.Value.Date;

                facts = facts.Where(f =>
                    f.CreatedDate != null &&
                    f.CreatedDate.Date <= to     // 👈 DimDate.Date vs DateTime
                );
            }

            // PublicationFrom
            if (filter.PublicationFrom.HasValue)
            {
                var fromPub = filter.PublicationFrom.Value.Date;

                facts = facts.Where(f =>
                    f.PublicationDate != null &&
                    f.PublicationDate.Date >= fromPub   // 👈 DimDate.Date vs DateTime
                );
            }

            // PublicationTo
            if (filter.PublicationTo.HasValue)
            {
                var toPub = filter.PublicationTo.Value.Date;

                facts = facts.Where(f =>
                    f.PublicationDate != null &&
                    f.PublicationDate.Date <= toPub     // 👈 DimDate.Date vs DateTime
                );
            }

            // ---------------------------
            // 2) Filtros por dimensiones
            // ---------------------------
            if (filter.AcademicTermKeys?.Any() == true)
            {
                facts = facts.Where(f =>
                    f.AcademicTermKey != null &&
                    filter.AcademicTermKeys.Contains(f.AcademicTermKey.Value));
            }

            if (filter.ProjectKeys?.Any() == true)
            {
                facts = facts.Where(f =>
                    f.ProjectKey != null &&
                    filter.ProjectKeys.Contains(f.ProjectKey.Value));
            }

            if (filter.ResearchLineKeys?.Any() == true)
            {
                facts = facts.Where(f =>
                    f.ResearchLineKey != null &&
                    filter.ResearchLineKeys.Contains(f.ResearchLineKey.Value));
            }

            if (filter.FieldKeys?.Any() == true)
            {
                facts = facts.Where(f =>
                    f.FieldKey != null &&
                    filter.FieldKeys.Contains(f.FieldKey.Value));
            }

            if (filter.PublicationStatusKeys?.Any() == true)
            {
                facts = facts.Where(f =>
                    f.PublicationStatusKey != null &&
                    filter.PublicationStatusKeys.Contains(f.PublicationStatusKey.Value));
            }

            if (filter.VenueKeys?.Any() == true)
            {
                facts = facts.Where(f =>
                    f.VenueKey != null &&
                    filter.VenueKeys.Contains(f.VenueKey.Value));
            }

            // Open Access
            if (filter.IsOpenAccess.HasValue)
            {
                facts = facts.Where(f => f.IsOpenAccess == filter.IsOpenAccess.Value);
            }

            // ---------------------------
            // 3) Filtro por cuartiles
            // ---------------------------
            if (filter.Quartiles?.Any() == true)
            {
                var requested = filter.Quartiles
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .Select(q => q.Trim().ToUpper())
                    .ToList();

                bool includeNoQuartile = requested.Contains("SIN_CUARTIL") || requested.Contains("NOQ");

                var quartileCodes = requested
                    .Where(q => q == "Q1" || q == "Q2" || q == "Q3" || q == "Q4")
                    .ToList();

                if (includeNoQuartile && quartileCodes.Count > 0)
                {
                    facts = facts.Where(f =>
                        (f.Quartile != null && quartileCodes.Contains(f.Quartile)) ||
                        (f.Quartile == null || f.Quartile == "" || f.Quartile == "N/A"));
                }
                else if (includeNoQuartile)
                {
                    facts = facts.Where(f => f.Quartile == null || f.Quartile == "" || f.Quartile == "N/A");
                }
                else if (quartileCodes.Count > 0)
                {
                    facts = facts.Where(f => f.Quartile != null && quartileCodes.Contains(f.Quartile));
                }
            }

            // ---------------------------
            // 4) Filtro por fuentes de indexación
            // ---------------------------
            if (filter.IndexingSourceKeys?.Any() == true)
            {
                var filteredArticleKeys = await _dw.FactArticleIndexings
                    .Where(fi => filter.IndexingSourceKeys.Contains(fi.IndexingSourceKey))
                    .Select(fi => fi.ArticleKey)
                    .Distinct()
                    .ToListAsync(ct);

                if (filteredArticleKeys.Count == 0)
                {
                    return Ok(new ArticlesDashboardDto
                    {
                        AppliedFilter = filter
                    });
                }

                facts = facts.Where(f => filteredArticleKeys.Contains(f.ArticleKey));
            }

            // ===================== KPIs =====================
            var nowYear = DateTime.UtcNow.Year;

            var totalArticles = await facts
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var publishedThisYear = await facts
                .Where(f => f.PublicationDate != null && f.PublicationDate!.Year == nowYear)
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var q1q2Count = await facts
                .Where(f => f.Quartile == "Q1" || f.Quartile == "Q2")
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var openAccessCount = await facts
                .Where(f => f.IsOpenAccess)
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var filteredArticleKeysForScopus = await facts
                .Select(f => f.ArticleKey)
                .Distinct()
                .ToListAsync(ct);

            var scopusIndexed = 0;
            if (filteredArticleKeysForScopus.Count > 0)
            {
                scopusIndexed = await _dw.FactArticleIndexings
                    .Where(fi =>
                        fi.IndexingSource!.Name == "Scopus" &&
                        filteredArticleKeysForScopus.Contains(fi.ArticleKey))
                    .Select(fi => fi.ArticleKey)
                    .Distinct()
                    .CountAsync(ct);
            }

            var kpis = new ArticlesKpiSummaryDto
            {
                TotalArticles = totalArticles,
                PublishedThisYear = publishedThisYear,
                Q1Q2Count = q1q2Count,
                OpenAccessCount = openAccessCount,
                IndexedInScopusCount = scopusIndexed
            };

            // ===================== Distribución por año =====================
            var byYear = await facts
                .Where(f => f.CreatedDate != null)
                .GroupBy(f => f.CreatedDate!.Year)
                .Select(g => new ArticlesByYearDto
                {
                    Year = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderBy(x => x.Year)
                .ToListAsync(ct);

            // ===================== Distribución por áreas =====================
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
                .ToListAsync(ct);

            // ===================== Distribución por líneas de investigación =====================
            var byResearchLine = await facts
                .Where(f => f.ResearchLineKey != null)
                .GroupBy(f => f.ResearchLine!.Name)
                .Select(g => new ArticlesByResearchLineDto
                {
                    ResearchLineName = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            // ===================== Quartiles =====================
            var quartileGroups = await facts
                .GroupBy(f => f.Quartile)
                .Select(g => new
                {
                    Quartile = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .ToListAsync(ct);

            var totalQ = quartileGroups.Sum(x => x.Count);

            int q1 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q1")?.Count ?? 0;
            int q2 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q2")?.Count ?? 0;
            int q3 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q3")?.Count ?? 0;
            int q4 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q4")?.Count ?? 0;

            int noQuartile = quartileGroups
                .Where(x => string.IsNullOrEmpty(x.Quartile) || x.Quartile == "N/A")
                .Sum(x => x.Count);

            double PercentQ(int value) =>
                totalQ == 0 ? 0 : Math.Round((value / (double)totalQ) * 100.0, 2);

            var quartilesDto = new ArticlesQuartileStatsDto
            {
                TotalArticles = totalQ,
                Q1Count = q1,
                Q2Count = q2,
                Q3Count = q3,
                Q4Count = q4,
                NoQuartileCount = noQuartile,
                Q1Q2Percent = PercentQ(q1 + q2),
                Q3Q4Percent = PercentQ(q3 + q4),
                NoQuartilePercent = PercentQ(noQuartile)
            };

            // ===================== OpenAccess / Indexing (sobre facts filtrado) =====================
            var totalForAccess = totalArticles;

            double Percent(int value) =>
                totalForAccess == 0 ? 0 : Math.Round((value / (double)totalForAccess) * 100.0, 2);

            var accessDto = new ArticlesOpenAccessIndexingStatsDto
            {
                TotalArticles = totalForAccess,
                OpenAccessCount = openAccessCount,
                OpenAccessPercent = Percent(openAccessCount),
                ScopusIndexedCount = scopusIndexed,
                ScopusIndexedPercent = Percent(scopusIndexed)
            };

            // ===================== Time to publication (sobre facts filtrado) =====================
            var diffs = await facts
                .Where(f => f.CreatedDate != null && f.PublicationDate != null)
                .Select(f => EF.Functions.DateDiffDay(
                    f.CreatedDate!.Date,
                    f.PublicationDate!.Date
                ))
                .Where(days => days >= 0)
                .Cast<int>()
                .ToListAsync(ct);

            ArticlesTimeToPublicationStatsDto timeDto;

            if (diffs.Count == 0)
            {
                timeDto = new ArticlesTimeToPublicationStatsDto
                {
                    ArticlesWithDatesCount = 0
                };
            }
            else
            {
                diffs.Sort();

                int n = diffs.Count;
                double avg = diffs.Average();
                int min = diffs.First();
                int max = diffs.Last();

                int Percentile(double p)
                {
                    if (n == 0) return 0;
                    var index = (int)Math.Round((p / 100.0) * (n - 1));
                    if (index < 0) index = 0;
                    if (index >= n) index = n - 1;
                    return diffs[index];
                }

                int p50 = Percentile(50);
                int p75 = Percentile(75);
                int p90 = Percentile(90);

                int bucket0To180 = diffs.Count(d => d <= 180);
                int bucket181To365 = diffs.Count(d => d > 180 && d <= 365);
                int bucketMore365 = diffs.Count(d => d > 365);

                timeDto = new ArticlesTimeToPublicationStatsDto
                {
                    ArticlesWithDatesCount = n,
                    AverageDays = Math.Round(avg, 2),
                    MinDays = min,
                    MaxDays = max,
                    P50 = p50,
                    P75 = p75,
                    P90 = p90,
                    Bucket0To180 = bucket0To180,
                    Bucket181To365 = bucket181To365,
                    BucketMore365 = bucketMore365
                };
            }

            // ===================== Construimos el dashboard =====================
            var dashboard = new ArticlesDashboardDto
            {
                AppliedFilter = filter,
                Kpis = kpis,
                ByYear = byYear,
                ByField = byField,
                ByResearchLine = byResearchLine,
                Quartiles = quartilesDto,
                AccessIndexing = accessDto,
                TimeToPublication = timeDto
            };

            return Ok(dashboard);
        }

        // =====================================================================
        // 2) ENDPOINT PDF
        // =====================================================================

        [HttpGet("articles/statistics/pdf")]
        public async Task<IActionResult> GetStatisticsPdf(
            [FromQuery] int? minYear,
            [FromQuery] int? maxYear,
            [FromQuery] bool includeKpis = true,
            [FromQuery] bool includeByYear = true,
            [FromQuery] bool includeByField = true,
            [FromQuery] bool includeByResearchLine = true,
            [FromQuery] bool includeQuartiles = true,
            CancellationToken ct = default)
        {
            var nowYear = DateTime.UtcNow.Year;

            var facts = _dw.FactArticlePublications.AsQueryable();

            // Filtro por años (si aplica)
            if (minYear.HasValue || maxYear.HasValue)
            {
                var dates = _dw.DimDates.AsQueryable();

                var min = minYear ?? dates.Min(d => d.Year);
                var max = maxYear ?? dates.Max(d => d.Year);

                facts = facts
                    .Where(f => f.CreatedDate != null)
                    .Where(f =>
                        f.CreatedDate!.Year >= min &&
                        f.CreatedDate!.Year <= max);
            }

            // ===================== KPIs =====================
            var totalArticles = await facts
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var publishedThisYear = await facts
                .Where(f => f.PublicationDate != null && f.PublicationDate!.Year == nowYear)
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var q1q2Count = await facts
                .Where(f => f.Quartile == "Q1" || f.Quartile == "Q2")
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

            var openAccessCount = await facts
                .Where(f => f.IsOpenAccess)
                .SumAsync(f => (int?)f.ArticleCount) ?? 0;

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

            // ===================== Quartiles (para PDF) =====================
            ArticlesQuartileStatsDto? quartileStats = null;

            if (includeQuartiles)
            {
                var quartileGroups = await facts
                    .GroupBy(f => f.Quartile)
                    .Select(g => new
                    {
                        Quartile = g.Key,
                        Count = g.Sum(x => x.ArticleCount)
                    })
                    .ToListAsync(ct);

                var totalQ = quartileGroups.Sum(x => x.Count);

                int q1 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q1")?.Count ?? 0;
                int q2 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q2")?.Count ?? 0;
                int q3 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q3")?.Count ?? 0;
                int q4 = quartileGroups.FirstOrDefault(x => x.Quartile == "Q4")?.Count ?? 0;

                int noQuartile = quartileGroups
                    .Where(x => string.IsNullOrEmpty(x.Quartile) || x.Quartile == "N/A")
                    .Sum(x => x.Count);

                double Percent(int value) =>
                    totalQ == 0 ? 0 : Math.Round((value / (double)totalQ) * 100.0, 2);

                quartileStats = new ArticlesQuartileStatsDto
                {
                    TotalArticles = totalQ,
                    Q1Count = q1,
                    Q2Count = q2,
                    Q3Count = q3,
                    Q4Count = q4,
                    NoQuartileCount = noQuartile,
                    Q1Q2Percent = Percent(q1 + q2),
                    Q3Q4Percent = Percent(q3 + q4),
                    NoQuartilePercent = Percent(noQuartile)
                };
            }

            // ===================== Distribución por año =====================
            var byYear = await facts
                .Where(f => f.CreatedDate != null)
                .GroupBy(f => f.CreatedDate!.Year)
                .Select(g => new ArticlesByYearDto
                {
                    Year = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderBy(x => x.Year)
                .ToListAsync(ct);

            // ===================== Áreas =====================
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

            // ===================== Líneas de investigación =====================
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

            // -----------------------------------------------------------------
            // Construimos el modelo para el documento
            // -----------------------------------------------------------------
            var model = new ArticlesStatisticsModel
            {
                MinYear = minYear,
                MaxYear = maxYear,
                IncludeKpis = includeKpis,
                IncludeByYear = includeByYear,
                IncludeByField = includeByField,
                IncludeByResearchLine = includeByResearchLine,
                IncludeQuartiles = includeQuartiles,
                Kpis = kpis,
                ByYear = byYear,
                ByField = byField,
                ByResearchLine = byResearchLine,
                QuartileStats = quartileStats ?? new ArticlesQuartileStatsDto()
            };

            // -----------------------------------------------------------------
            // Rutas de imágenes (header y logo)
            // -----------------------------------------------------------------
            var basePath = !string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? _env.WebRootPath
                : _env.ContentRootPath;

            string? headerImagePath = null;
            string? logoPath = null;

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                headerImagePath = Path.Combine(basePath, "images", "header-report.png");
                logoPath = Path.Combine(basePath, "images", "logo-uta.png");
            }

            if (headerImagePath != null && !System.IO.File.Exists(headerImagePath))
                headerImagePath = null;

            if (logoPath != null && !System.IO.File.Exists(logoPath))
                logoPath = null;

            // -----------------------------------------------------------------
            // Documento QuestPDF
            // -----------------------------------------------------------------
            var document = new ArticlesStatisticsDocument(
                model,
                headerImagePath,
                logoPath);

            var pdfBytes = document.GeneratePdf();

            const string fileName = "Reporte_Estadistico_Articulos.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
