using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedArticlesDwSource(UnifiedDideDbContext context) : IUnifiedArticlesDwSource
{
    private static readonly int ScientificProduction = (int)BaseProductTypeId.ScientificProduction;
    private static readonly int RegionalProduction = (int)BaseProductTypeId.RegionalProduction;

    public async Task<ArticlesDwDateBounds> GetDateBoundsAsync(CancellationToken ct = default)
    {
        var bounds = await context.ArticleReads.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new ArticlesDwDateBounds(
                group.Min(article => (DateTime?)article.CreatedAt),
                group.Max(article => (DateTime?)article.CreatedAt),
                group.Min(article => article.PublishedAt),
                group.Max(article => article.PublishedAt)))
            .SingleOrDefaultAsync(ct);

        return bounds ?? new ArticlesDwDateBounds(null, null, null, null);
    }

    public async Task<IReadOnlyList<ArticlesDwPublicationRow>> ListArticlePublicationsAsync(CancellationToken ct = default) =>
        await context.ArticleReads.AsNoTracking()
            .Select(article => new ArticlesDwPublicationRow(
                article.ProductId,
                article.ArticleId,
                article.ProductTypeId,
                article.Title,
                article.IsActive,
                article.CreatedAt,
                article.Journal,
                article.IndexingDatabase,
                article.Sjr,
                article.SjrRaw,
                article.Quartile,
                article.Issn,
                article.Doi,
                article.Year,
                article.YearRaw,
                article.PublicationUrl,
                article.ProjectId,
                article.ProjectId != null,
                article.ProjectFacultyId,
                article.PublishedAt,
                article.PageCount,
                article.IsOpenAccess,
                article.HasInterculturalComponent,
                article.VenueId,
                article.AcademicTermId,
                article.PublicationStatusId,
                article.ResearchLineId,
                article.BroadFieldId,
                article.SpecificFieldId,
                article.DetailedFieldId,
                article.FacultyId,
                article.ProceedingsName,
                article.Proceedings,
                article.EventName,
                article.GroupName,
                article.Filiacion,
                article.ExternalSource,
                article.ExternalId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwAuthorRow>> ListArticleAuthorsAsync(CancellationToken ct = default) =>
        await context.ProductAuthors.AsNoTracking()
            .Where(productAuthor =>
                productAuthor.Product.ProductTypeId == ScientificProduction ||
                productAuthor.Product.ProductTypeId == RegionalProduction)
            .Select(productAuthor => new ArticlesDwAuthorRow(
                productAuthor.Id,
                productAuthor.ProductId,
                productAuthor.AuthorId,
                productAuthor.Author.AppUserId != null,
                productAuthor.Author.AppUser == null ? null : productAuthor.Author.AppUser.IdUser,
                productAuthor.Author.AppUser == null ? null : productAuthor.Author.AppUser.IdAsp,
                productAuthor.Author.ExternalResearcherId,
                productAuthor.Author.ExternalResearcher == null
                    ? null
                    : productAuthor.Author.ExternalResearcher.FullName,
                productAuthor.Author.Orcid,
                productAuthor.AuthorOrder,
                productAuthor.IsPrimaryAuthor,
                productAuthor.Participation,
                productAuthor.NameSnapshot,
                productAuthor.AffiliationSnapshot))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwIndexingRow>> ListArticleIndexingsAsync(CancellationToken ct = default) =>
        await context.ArticleIndexings.AsNoTracking()
            .Where(indexing =>
                indexing.Article.Product.ProductTypeId == ScientificProduction ||
                indexing.Article.Product.ProductTypeId == RegionalProduction)
            .Select(indexing => new ArticlesDwIndexingRow(
                indexing.Article.ProductId,
                indexing.ArticleId,
                indexing.IndexingSourceId,
                indexing.IndexingSource.Name,
                indexing.IndexingSource.Abbreviation,
                indexing.IndexingSource.IsActive))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwVenueRow>> ListVenuesAsync(CancellationToken ct = default) =>
        await context.Venues.AsNoTracking()
            .Select(venue => new ArticlesDwVenueRow(
                venue.VenueId,
                venue.Name,
                venue.IssnCode,
                venue.IssueNumber,
                venue.VolumeNumber,
                venue.JournalUrl,
                venue.Type))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwVenueMetricRow>> ListVenueMetricsAsync(CancellationToken ct = default) =>
        await context.VenueMetrics.AsNoTracking()
            .Select(metric => new ArticlesDwVenueMetricRow(
                metric.VenueId,
                metric.Year,
                metric.SJR,
                metric.Quartile))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwAcademicTermRow>> ListAcademicTermsAsync(CancellationToken ct = default) =>
        await context.AcademicTerms.AsNoTracking()
            .Select(term => new ArticlesDwAcademicTermRow(
                term.AcademicTermId,
                term.ExternalPeriodId,
                term.Name,
                term.StartDate,
                term.EndDate))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwPublicationStatusRow>> ListPublicationStatusesAsync(CancellationToken ct = default) =>
        await context.PublicationStatuses.AsNoTracking()
            .Select(status => new ArticlesDwPublicationStatusRow(status.PublicationStatusId, status.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwResearchLineRow>> ListResearchLinesAsync(CancellationToken ct = default) =>
        await context.ResearchLines.AsNoTracking()
            .Select(line => new ArticlesDwResearchLineRow(line.ResearchLineId, line.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwBroadFieldRow>> ListBroadFieldsAsync(CancellationToken ct = default) =>
        await context.BroadFields.AsNoTracking()
            .Select(field => new ArticlesDwBroadFieldRow(field.BroadFieldId, field.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwSpecificFieldRow>> ListSpecificFieldsAsync(CancellationToken ct = default) =>
        await context.SpecificFields.AsNoTracking()
            .Select(field => new ArticlesDwSpecificFieldRow(
                field.SpecificFieldId,
                field.BroadFieldId,
                field.Code,
                field.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwDetailedFieldRow>> ListDetailedFieldsAsync(CancellationToken ct = default) =>
        await context.DetailedFields.AsNoTracking()
            .Select(field => new ArticlesDwDetailedFieldRow(
                field.DetailedFieldId,
                field.SpecificFieldId,
                field.Code,
                field.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwFacultyRow>> ListFacultiesAsync(CancellationToken ct = default) =>
        await context.Faculties.AsNoTracking()
            .Select(faculty => new ArticlesDwFacultyRow(
                faculty.FacultyId,
                faculty.ExternalFacultyId,
                faculty.ParentFacultyId,
                faculty.Acronym,
                faculty.Name,
                faculty.IsActive))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ArticlesDwIndexingSourceRow>> ListIndexingSourcesAsync(CancellationToken ct = default) =>
        await context.IndexingSources.AsNoTracking()
            .Select(source => new ArticlesDwIndexingSourceRow(
                source.Id,
                source.Name,
                source.Abbreviation,
                source.ReferenceUrl,
                source.IsActive))
            .ToListAsync(ct);
}
