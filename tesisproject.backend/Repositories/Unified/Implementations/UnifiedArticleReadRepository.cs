using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.ReadModels;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedArticleReadRepository(UnifiedDideDbContext context) : IUnifiedArticleReadRepository
{
    public IQueryable<ArticleReadModel> Query() => context.ArticleReads.AsNoTracking();

    public async Task<(IReadOnlyList<ArticleReadAggregate> Items, int TotalCount)> GetPageAsync(ArticleListQuery filters, CancellationToken ct = default)
    {
        // Existing HTTP DTOs identify the structural Article, not a bare Product without an extension.
        var query = Query().Where(a => a.ArticleId.HasValue);
        var search = string.IsNullOrWhiteSpace(filters.SearchTerm) ? filters.Search : filters.SearchTerm;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.Title.Contains(term) || (a.Doi != null && a.Doi.Contains(term)) || (a.ExternalId != null && a.ExternalId.Contains(term)));
        }
        if (filters.Year.HasValue) query = query.Where(a => a.Year == filters.Year);
        if (filters.VenueId.HasValue) query = query.Where(a => a.VenueId == filters.VenueId);
        if (filters.PublicationStatusId.HasValue) query = query.Where(a => a.PublicationStatusId == filters.PublicationStatusId);
        if (!string.IsNullOrWhiteSpace(filters.PublicationStatusKey))
            query = query.Where(a => context.Set<Product>().Any(p => p.Id == a.ProductId && p.Article!.PublicationStatus!.Name == filters.PublicationStatusKey));
        if (filters.FacultyId.HasValue) query = query.Where(a => a.FacultyId == filters.FacultyId);
        if (filters.ResearchLineId.HasValue) query = query.Where(a => a.ResearchLineId == filters.ResearchLineId);
        if (filters.BroadFieldId.HasValue) query = query.Where(a => a.BroadFieldId == filters.BroadFieldId);
        if (filters.SpecificFieldId.HasValue) query = query.Where(a => a.SpecificFieldId == filters.SpecificFieldId);
        if (filters.DetailedFieldId.HasValue) query = query.Where(a => a.DetailedFieldId == filters.DetailedFieldId);
        if (filters.IndexingSourceId.HasValue)
            query = query.Where(a => context.Set<Product>().Any(p => p.Id == a.ProductId && p.Article!.Indexings.Any(i => i.IndexingSourceId == filters.IndexingSourceId)));
        var total = await query.CountAsync(ct);
        IOrderedQueryable<ArticleReadModel> ordered = (filters.SortBy?.Trim().ToLowerInvariant(), filters.SortDesc) switch
        {
            ("title", false) => query.OrderBy(a => a.Title), ("title", true) => query.OrderByDescending(a => a.Title),
            ("year", false) => query.OrderBy(a => a.Year).ThenBy(a => a.Title), ("year", true) => query.OrderByDescending(a => a.Year).ThenBy(a => a.Title),
            ("publishedat", false) => query.OrderBy(a => a.PublishedAt), ("publishedat", true) => query.OrderByDescending(a => a.PublishedAt),
            (_, false) => query.OrderBy(a => a.CreatedAt), _ => query.OrderByDescending(a => a.CreatedAt)
        };
        var rows = await ordered.ThenBy(a => a.ProductId).Skip((filters.Page - 1) * filters.PageSize).Take(filters.PageSize).ToListAsync(ct);
        var ids = rows.Select(r => r.ProductId).ToArray();
        var products = await Graph().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        return (rows.Where(r => products.ContainsKey(r.ProductId)).Select(r => new ArticleReadAggregate(r, products[r.ProductId])).ToList(), total);
    }

    public async Task<ArticleReadAggregate?> GetDetailAsync(int articleId, CancellationToken ct = default)
    {
        var row = await Query().SingleOrDefaultAsync(a => a.ArticleId == articleId, ct);
        if (row is null) return null;
        var product = await Graph().SingleOrDefaultAsync(p => p.Id == row.ProductId, ct);
        return product is null ? null : new ArticleReadAggregate(row, product);
    }

    private IQueryable<Product> Graph() => context.Set<Product>().AsNoTracking()
        .Include(p => p.Authors)
        .Include(p => p.Article).ThenInclude(a => a!.Venue)
        .Include(p => p.Article).ThenInclude(a => a!.AcademicTerm)
        .Include(p => p.Article).ThenInclude(a => a!.PublicationStatus)
        .Include(p => p.Article).ThenInclude(a => a!.ResearchLine)
        .Include(p => p.Article).ThenInclude(a => a!.Faculty)
        .Include(p => p.Article).ThenInclude(a => a!.Files)
        .Include(p => p.Article).ThenInclude(a => a!.DynamicFieldValues).ThenInclude(v => v.Field)
        .Include(p => p.Article).ThenInclude(a => a!.Indexings).ThenInclude(i => i.IndexingSource)
        .AsSplitQuery();
}
