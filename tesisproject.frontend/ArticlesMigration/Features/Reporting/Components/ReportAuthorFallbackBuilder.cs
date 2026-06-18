using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Reporting.Components;

public static class ReportAuthorFallbackBuilder
{
    public const string AvailabilityMessage = "Se muestra un resumen autoral derivado del tablero institucional mientras se completa la analítica detallada.";
    public const string ErrorMessage = "La analítica detallada de autores no respondió como esperaba. Se muestra un resumen autoral derivado del tablero institucional.";

    public static bool ShouldUseFallback(
        AuthorReportingDashboardDto dashboard,
        InstitutionalReportingDashboardDto? institutionalDashboard)
    {
        return dashboard.Kpis.TotalAuthors == 0
            && (institutionalDashboard?.ArticlesByAuthor.Count ?? 0) > 0;
    }

    public static AuthorReportingDashboardDto? Build(InstitutionalReportingDashboardDto? institutionalDashboard)
    {
        if (institutionalDashboard is null)
        {
            return null;
        }

        var authorItems = institutionalDashboard.ArticlesByAuthor
            .Where(x => x.TotalArticles > 0)
            .ToList();

        if (authorItems.Count == 0)
        {
            return null;
        }

        var tracedArticles = institutionalDashboard.AuthorTraceCoverage.ArticlesWithAuthorTrace;
        var totalArticles = institutionalDashboard.ScientificProduction.TotalArticles;
        var totalAuthorArticleLinks = authorItems.Sum(x => x.TotalArticles);

        return new AuthorReportingDashboardDto
        {
            Kpis = new AuthorReportingKpiDto
            {
                TotalAuthors = authorItems.Count,
                TotalArticles = totalArticles,
                ArticlesWithAuthorTrace = tracedArticles,
                ArticlesWithoutAuthorTrace = institutionalDashboard.AuthorTraceCoverage.ArticlesWithoutAuthorTrace,
                TotalAuthorArticleLinks = totalAuthorArticleLinks,
                PrimaryAuthorLinks = 0,
                CoauthorLinks = 0,
                AuthorsWithOrcid = 0,
                AuthorsWithAffiliation = 0,
                AverageAuthorsPerArticle = tracedArticles <= 0
                    ? 0
                    : Math.Round(totalAuthorArticleLinks / (decimal)tracedArticles, 2)
            },
            FilterOptions = new AuthorReportingFilterOptionsDto
            {
                Authors = authorItems.Select(x => x.Name).ToList()
            },
            Authors = authorItems
                .Select((x, index) => new AuthorReportingSummaryDto
                {
                    AuthorKey = index + 1,
                    AuthorName = x.Name,
                    Affiliation = "Sin detalle",
                    ParticipantType = "Sin detalle",
                    TotalArticles = x.TotalArticles,
                    PrimaryAuthorArticles = 0,
                    CoauthorArticles = 0
                })
                .ToList(),
            Publications = new List<AuthorPublicationDto>(),
            Coauthors = new List<AuthorCoauthorDto>(),
            ArticlesByAffiliation = new List<ReportingSummaryItemDto>(),
            ArticlesByFaculty = institutionalDashboard.ArticlesByFaculty
                .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
                .ToList(),
            ArticlesByIndexingSource = institutionalDashboard.ArticlesByIndexingSource
                .Select(x => new ReportingSummaryItemDto { Name = x.IndexingSourceName, TotalArticles = x.TotalArticles })
                .ToList(),
            ArticlesByQuartile = institutionalDashboard.ArticlesByQuartile
                .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
                .ToList(),
            ArticlesByMonth = institutionalDashboard.ArticlesByMonth
                .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
                .ToList()
        };
    }
}
