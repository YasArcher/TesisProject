using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Articles;
using tesisproject.backend.Data.Articles.Entities;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Repositories.Implementations
{
    public sealed class ArticleReadRepository : IArticleReadRepository
    {
        private readonly ArticlesDbContext _ctx;

        public ArticleReadRepository(ArticlesDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<(IReadOnlyList<Article> Items, int TotalCount)> GetPageAsync(
            ArticleListQuery query,
            CancellationToken ct = default)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var filtered = ApplyFilters(_ctx.Articles.AsNoTracking(), query);
            var totalCount = await filtered.CountAsync(ct);

            var items = await ApplyOrdering(filtered, query)
                .Include(article => article.Venue)
                .Include(article => article.PublicationStatus)
                .Include(article => article.ResearchLine)
                .Include(article => article.Faculty)
                .Include(article => article.AcademicTerm)
                .Include(article => article.Participants)
                .Include(article => article.Indexings)
                    .ThenInclude(indexing => indexing.IndexingSource)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsSplitQuery()
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<Article?> GetDetailAsync(int articleId, CancellationToken ct = default)
        {
            return await _ctx.Articles
                .AsNoTracking()
                .Include(article => article.Venue)
                    .ThenInclude(venue => venue!.VenueMetrics)
                .Include(article => article.AcademicTerm)
                .Include(article => article.PublicationStatus)
                .Include(article => article.ResearchLine)
                .Include(article => article.BroadField)
                .Include(article => article.SpecificField)
                .Include(article => article.DetailedField)
                .Include(article => article.Faculty)
                .Include(article => article.Participants)
                .Include(article => article.Indexings)
                    .ThenInclude(indexing => indexing.IndexingSource)
                .Include(article => article.Files)
                .Include(article => article.DynamicFieldValues)
                    .ThenInclude(value => value.Field)
                .AsSplitQuery()
                .FirstOrDefaultAsync(article => article.Id == articleId, ct);
        }

        private static IQueryable<Article> ApplyFilters(IQueryable<Article> query, ArticleListQuery filters)
        {
            var search = string.IsNullOrWhiteSpace(filters.SearchTerm) ? filters.Search : filters.SearchTerm;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(article =>
                    (article.Title != null && article.Title.Contains(term)) ||
                    (article.Doi != null && article.Doi.Contains(term)) ||
                    (article.ExternalId != null && article.ExternalId.Contains(term)));
            }

            if (filters.Year.HasValue)
                query = query.Where(article => article.Year == filters.Year.Value);
            if (filters.VenueId.HasValue)
                query = query.Where(article => article.VenueId == filters.VenueId.Value);
            if (filters.PublicationStatusId.HasValue)
                query = query.Where(article => article.PublicationStatusId == filters.PublicationStatusId.Value);
            if (!string.IsNullOrWhiteSpace(filters.PublicationStatusKey))
                query = query.Where(article => article.PublicationStatus != null && article.PublicationStatus.Name == filters.PublicationStatusKey);
            if (filters.FacultyId.HasValue)
                query = query.Where(article => article.FacultyId == filters.FacultyId.Value);
            if (filters.ResearchLineId.HasValue)
                query = query.Where(article => article.ResearchLineId == filters.ResearchLineId.Value);
            if (filters.BroadFieldId.HasValue)
                query = query.Where(article => article.BroadFieldId == filters.BroadFieldId.Value);
            if (filters.SpecificFieldId.HasValue)
                query = query.Where(article => article.SpecificFieldId == filters.SpecificFieldId.Value);
            if (filters.DetailedFieldId.HasValue)
                query = query.Where(article => article.DetailedFieldId == filters.DetailedFieldId.Value);
            if (filters.IndexingSourceId.HasValue)
                query = query.Where(article => article.Indexings.Any(indexing => indexing.IndexingSourceId == filters.IndexingSourceId.Value));

            return query;
        }

        private static IOrderedQueryable<Article> ApplyOrdering(IQueryable<Article> query, ArticleListQuery filters)
        {
            var sortBy = filters.SortBy?.Trim().ToLowerInvariant();
            return (sortBy, filters.SortDesc) switch
            {
                ("title", false) => query.OrderBy(article => article.Title),
                ("title", true) => query.OrderByDescending(article => article.Title),
                ("year", false) => query.OrderBy(article => article.Year).ThenBy(article => article.Title),
                ("year", true) => query.OrderByDescending(article => article.Year).ThenBy(article => article.Title),
                ("publishedat", false) => query.OrderBy(article => article.PublishedAt),
                ("publishedat", true) => query.OrderByDescending(article => article.PublishedAt),
                (_, false) => query.OrderBy(article => article.CreatedAt),
                _ => query.OrderByDescending(article => article.CreatedAt)
            };
        }
    }
}
