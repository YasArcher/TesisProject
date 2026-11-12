using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Analytic.Implementations;

// Modulo articulos: cada agregado se ejecuta en SQL para no cargar todas las publicaciones en memoria.
public sealed class ArticleAnalyticsService(ArticlesDwContext dw) : IArticleAnalyticsService
{
    public async Task<ArticleAnalyticsSummaryDto> GetSummaryAsync(int? year, string? search, CancellationToken ct)
    {
        var facts = dw.FactArticlePublications.AsNoTracking().AsQueryable();
        if (year.HasValue)
            facts = facts.Where(x => x.PublishedDate != null
                ? x.PublishedDate.Year == year.Value
                : x.Article.PublicationYear == year.Value);

        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            facts = facts.Where(x => x.Article.Title.Contains(search)
                || (x.Article.Doi != null && x.Article.Doi.Contains(search))
                || (x.Journal != null && x.Journal.Name.Contains(search))
                || (x.ArticleFaculty != null && x.ArticleFaculty.FacultyName != null
                    && x.ArticleFaculty.FacultyName.Contains(search)));

        var counts = await facts.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Doi = group.Count(x => x.Article.Doi != null && x.Article.Doi != ""),
            OpenAccess = group.Count(x => x.IsOpenAccessFlag == true),
            Authors = group.Count(x => x.AuthorCount > 0),
            Indexing = group.Count(x => x.IndexingCount > 0),
            Faculty = group.Count(x => x.ArticleFacultyKey != null)
        }).SingleOrDefaultAsync(ct);

        var years = await facts.GroupBy(x => x.PublishedDate != null
                ? (int?)x.PublishedDate.Year
                : x.Article.PublicationYear)
            .Select(group => new { Year = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Year)
            .ToListAsync(ct);

        var faculties = await facts.GroupBy(x => x.ArticleFaculty != null
                ? x.ArticleFaculty.FacultyName : null)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name)
            .ToListAsync(ct);

        var indexing = await facts.GroupBy(x => x.IndexingDatabase != null
                ? x.IndexingDatabase.Name : null)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name)
            .ToListAsync(ct);

        var statuses = await facts.GroupBy(x => x.PublicationStatus != null
                ? x.PublicationStatus.Name : null)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name)
            .ToListAsync(ct);

        return new ArticleAnalyticsSummaryDto
        {
            TotalArticles = counts?.Total ?? 0,
            WithDoi = counts?.Doi ?? 0,
            OpenAccess = counts?.OpenAccess ?? 0,
            WithAuthors = counts?.Authors ?? 0,
            WithIndexing = counts?.Indexing ?? 0,
            WithFaculty = counts?.Faculty ?? 0,
            ByPublicationYear = years.Select(x => new ArticleAnalyticsGroupDto
                { Label = x.Year?.ToString() ?? "Sin ano", Count = x.Count }).ToList(),
            ByFaculty = faculties.Select(x => new ArticleAnalyticsGroupDto
                { Label = Label(x.Name, "Sin facultad"), Count = x.Count }).ToList(),
            ByIndexingDatabase = indexing.Select(x => new ArticleAnalyticsGroupDto
                { Label = Label(x.Name, "Sin indexacion"), Count = x.Count }).ToList(),
            ByPublicationStatus = statuses.Select(x => new ArticleAnalyticsGroupDto
                { Label = Label(x.Name, "Sin estado"), Count = x.Count }).ToList()
        };
    }

    private static string Label(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
