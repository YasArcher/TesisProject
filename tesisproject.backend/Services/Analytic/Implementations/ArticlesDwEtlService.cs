using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Analytic.Contracts;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Analytic.Implementations;

public sealed class ArticlesDwEtlService(
    IUnifiedArticlesDwSource source,
    ArticlesDwContext dw,
    ILogger<ArticlesDwEtlService> logger,
    IOperationExecutionHistoryService history,
    IUnifiedArticleUserContext userContext) : IArticlesDwEtlService
{
    private const string InvalidPublicationYear = "PUBLICATION_YEAR_INVALID";

    public async Task<ServiceResult<NoContent>> RunFullLoadAsync(CancellationToken ct = default)
    {
        var execution = await history.StartAsync(new(
            OperationExecutionTypes.Etl, OperationCodes.ArticlesDwFullLoad,
            ExecutedByAppUserId: await userContext.GetAppUserIdAsync(ct),
            ExecutedByName: userContext.DisplayName,
            Source: userContext.IsAuthenticated ? "HTTP" : "System"), ct);
        var timer = Stopwatch.StartNew();
        var warnings = new WarningCollector();
        IDbContextTransaction? transaction = null;
        try
        {
            logger.LogInformation("Articles DW ETL - Full load started");
            var snapshot = await ReadSnapshotAsync(ct);
            var load = BuildLoad(snapshot, warnings);
            if (dw.Database.IsRelational()) transaction = await dw.Database.BeginTransactionAsync(ct);

            StageClear();
            await dw.SaveChangesAsync(ct);
            StageLoad(load);
            await dw.SaveChangesAsync(ct);

            timer.Stop();
            var result = new ArticlesDwFullLoadHistoryResult(timer.ElapsedMilliseconds, load.Counts, warnings.Snapshot());
            await history.CompleteSuccessAsync(execution.ExecutionId, result, ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            logger.LogInformation("Articles DW ETL - Full load finished");
            return ServiceResult<NoContent>.Ok(new NoContent());
        }
        catch (OperationCanceledException)
        {
            timer.Stop();
            await RollbackAndRecordFailureAsync(transaction, execution.ExecutionId,
                "ARTICLES_DW_ETL_CANCELED", "The Articles DW full load was canceled.", timer.ElapsedMilliseconds, warnings);
            throw;
        }
        catch (Exception)
        {
            timer.Stop();
            await RollbackAndRecordFailureAsync(transaction, execution.ExecutionId,
                "ARTICLES_DW_ETL_FULL_LOAD_FAILED", "The Articles DW full load failed.", timer.ElapsedMilliseconds, warnings);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private async Task<Snapshot> ReadSnapshotAsync(CancellationToken ct)
    {
        // Deliberately sequential: the source owns one Unified EF context. Exactly one query per collection.
        var dates = await source.GetDateBoundsAsync(ct);
        var publications = await source.ListArticlePublicationsAsync(ct);
        var authors = await source.ListArticleAuthorsAsync(ct);
        var indexings = await source.ListArticleIndexingsAsync(ct);
        var venues = await source.ListVenuesAsync(ct);
        var metrics = await source.ListVenueMetricsAsync(ct);
        var terms = await source.ListAcademicTermsAsync(ct);
        var statuses = await source.ListPublicationStatusesAsync(ct);
        var lines = await source.ListResearchLinesAsync(ct);
        var broad = await source.ListBroadFieldsAsync(ct);
        var specific = await source.ListSpecificFieldsAsync(ct);
        var detailed = await source.ListDetailedFieldsAsync(ct);
        var faculties = await source.ListFacultiesAsync(ct);
        var indexingSources = await source.ListIndexingSourcesAsync(ct);
        return new(dates, publications, authors, indexings, venues, metrics, terms, statuses, lines,
            broad, specific, detailed, faculties, indexingSources);
    }

    private static LoadSet BuildLoad(Snapshot s, WarningCollector warnings)
    {
        ValidateSnapshot(s);
        var productIds = s.Publications.Select(x => x.ProductId).ToHashSet();
        if (s.Authors.Any(x => !productIds.Contains(x.ProductId)) || s.Indexings.Any(x => !productIds.Contains(x.ProductId)))
            throw new InvalidOperationException("Articles DW source returned a child row outside its publication set.");

        var dates = s.Publications.Select(x => x.CreatedAt.Date)
            .Concat(s.Publications.Where(x => x.PublishedAt.HasValue).Select(x => x.PublishedAt!.Value.Date))
            .Distinct().OrderBy(x => x).Select(x => new DimDate
            { DateKey = DateKey(x), Date = x, Year = x.Year, Month = x.Month, Day = x.Day }).ToList();
        var dateByKey = dates.ToDictionary(x => x.DateKey);

        var productTypes = s.Publications.Select(x => x.ProductTypeId).Distinct().OrderBy(x => x).Select(id => id switch
        {
            (int)BaseProductTypeId.ScientificProduction => new DimProductType
                { ProductTypeId = id, Name = nameof(BaseProductTypeId.ScientificProduction), IsActive = true },
            (int)BaseProductTypeId.RegionalProduction => new DimProductType
                { ProductTypeId = id, Name = nameof(BaseProductTypeId.RegionalProduction), IsActive = true },
            _ => throw new InvalidOperationException($"Unsupported Articles ProductTypeId {id}.")
        }).ToList();
        var productTypeById = productTypes.ToDictionary(x => x.ProductTypeId);

        var authors = BuildAuthors(s.Authors);
        var authorById = authors.ToDictionary(x => x.AuthorId);
        var journals = BuildStrings(s.Publications.Select(x => x.Journal), x => new DimJournal { Name = x });
        var journalByName = journals.ToDictionary(x => x.Name, StringComparer.Ordinal);
        var databases = BuildStrings(s.Publications.Select(x => x.IndexingDatabase), x => new DimIndexingDatabase { Name = x });
        var databaseByName = databases.ToDictionary(x => x.Name, StringComparer.Ordinal);
        var quartiles = BuildStrings(s.Publications
            .Where(x => x.ProductTypeId == (int)BaseProductTypeId.ScientificProduction).Select(x => x.Quartile),
            x => new DimQuartile { Code = x });
        var quartileByCode = quartiles.ToDictionary(x => x.Code, StringComparer.Ordinal);

        var facultySource = s.Faculties.ToDictionary(x => x.FacultyId);
        var faculties = s.Publications.SelectMany(x => new[] { x.ArticleFacultyId, x.ProjectFacultyId })
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x).Select(id =>
            {
                if (!facultySource.TryGetValue(id, out var f))
                    throw new InvalidOperationException($"FacultyId {id} used by an Article was not returned by the source.");
                return new DimFaculty { FacultyId = id, FacultyCode = f.Acronym, FacultyName = f.Name };
            }).ToList();
        var facultyById = faculties.ToDictionary(x => x.FacultyId);

        var venueSource = s.Venues.ToDictionary(x => x.VenueId);
        var venues = s.Publications.Where(x => x.VenueId.HasValue).Select(x => x.VenueId!.Value)
            .Distinct().OrderBy(x => x).Select(id =>
            {
                if (!venueSource.TryGetValue(id, out var v))
                    throw new InvalidOperationException($"VenueId {id} used by an Article was not returned by the source.");
                return new DimVenue { VenueId = id, Name = v.Name, IssnCode = v.IssnCode, Issue = v.Issue,
                    Volume = v.Volume, Url = v.Url, Type = v.Type };
            }).ToList();
        var venueById = venues.ToDictionary(x => x.VenueId);

        var terms = s.Terms.OrderBy(x => x.AcademicTermId)
            .Select(x => new DimAcademicTerm { AcademicTermId = x.AcademicTermId, Name = x.Name }).ToList();
        var termById = terms.ToDictionary(x => x.AcademicTermId);
        var statuses = s.Statuses.OrderBy(x => x.PublicationStatusId)
            .Select(x => new DimPublicationStatus { PublicationStatusId = x.PublicationStatusId, Name = x.Name }).ToList();
        var statusById = statuses.ToDictionary(x => x.PublicationStatusId);
        var lines = s.Lines.OrderBy(x => x.ResearchLineId)
            .Select(x => new DimResearchLine { ResearchLineId = x.ResearchLineId, Name = x.Name }).ToList();
        var lineById = lines.ToDictionary(x => x.ResearchLineId);
        var fields = BuildFields(s);
        var fieldByIds = fields.ToDictionary(x => (x.BroadFieldId, x.SpecificFieldId, x.DetailedFieldId));
        var indexingSources = s.IndexingSources.OrderBy(x => x.IndexingSourceId)
            .Select(x => new DimIndexingSource { IndexingSourceId = x.IndexingSourceId, Name = x.Name, IsActive = x.IsActive }).ToList();
        var indexingSourceById = indexingSources.ToDictionary(x => x.IndexingSourceId);

        var articles = s.Publications.OrderBy(x => x.ProductId).Select(p =>
        {
            var type2 = IsType2(p);
            if (!type2 && p.PublicationYear is null && !string.IsNullOrWhiteSpace(p.PublicationYearRaw))
                warnings.Add(InvalidPublicationYear);
            return new DimArticle
            {
                ProductId = p.ProductId, ArticleId = p.ArticleId, ProductTypeId = p.ProductTypeId,
                Title = p.Title, IsActive = p.IsActive, CreatedAt = p.CreatedAt,
                Doi = type2 ? null : Clean(p.Doi), PublicationYear = type2 ? null : p.PublicationYear,
                YearRaw = type2 ? null : Clean(p.PublicationYearRaw), IssnIsbn = Clean(p.IssnIsbn),
                PublicationUrl = Clean(p.PublicationUrl), ExternalSource = Clean(p.ExternalSource),
                ExternalId = Clean(p.ExternalId), ProceedingsName = Clean(p.ProceedingsName),
                Proceedings = Clean(p.Proceedings), EventName = Clean(p.EventName), GroupName = Clean(p.GroupName),
                Filiacion = Clean(p.Filiacion)
            };
        }).ToList();
        var articleByProduct = articles.ToDictionary(x => x.ProductId);
        var authorCount = s.Authors.GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => x.Count());
        var indexingCount = s.Indexings.GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => x.Count());

        var publications = s.Publications.OrderBy(x => x.ProductId).Select(p => new FactArticlePublication
        {
            Article = articleByProduct[p.ProductId], ProductId = p.ProductId, ProductType = productTypeById[p.ProductTypeId],
            Journal = Find(journalByName, p.Journal), IndexingDatabase = Find(databaseByName, p.IndexingDatabase),
            Quartile = IsType2(p) ? null : Find(quartileByCode, p.Quartile), Venue = Find(venueById, p.VenueId),
            AcademicTerm = Find(termById, p.AcademicTermId), PublicationStatus = Find(statusById, p.PublicationStatusId),
            ResearchLine = Find(lineById, p.ResearchLineId),
            Field = Find(fieldByIds, (p.BroadFieldId, p.SpecificFieldId, p.DetailedFieldId)),
            ArticleFaculty = Find(facultyById, p.ArticleFacultyId), ProjectFaculty = Find(facultyById, p.ProjectFacultyId),
            ProjectId = p.ProjectId, CreatedDate = dateByKey[DateKey(p.CreatedAt)],
            PublishedDate = p.PublishedAt.HasValue ? dateByKey[DateKey(p.PublishedAt.Value)] : null,
            ArticleCount = 1, AuthorCount = authorCount.GetValueOrDefault(p.ProductId),
            IndexingCount = indexingCount.GetValueOrDefault(p.ProductId), PageCount = p.PageCount,
            Sjr = IsType2(p) ? null : p.Sjr, IsProjectResultFlag = p.ProjectId.HasValue,
            IsOpenAccessFlag = p.IsOpenAccess, HasInterculturalFlag = p.HasInterculturalComponent
        }).ToList();

        var factAuthors = s.Authors.OrderBy(x => x.ProductAuthorId).Select(x => new FactArticleAuthor
        {
            ProductAuthorId = x.ProductAuthorId, Article = articleByProduct[x.ProductId], Author = authorById[x.AuthorId],
            AuthorOrder = x.AuthorOrder, IsPrimaryAuthor = x.IsPrimaryAuthor, Participation = Clean(x.Participation),
            NameSnapshot = Clean(x.NameSnapshot), AffiliationSnapshot = Clean(x.AffiliationSnapshot)
        }).ToList();
        var factIndexings = s.Indexings.OrderBy(x => x.ProductId).ThenBy(x => x.IndexingSourceId).Select(x =>
        {
            if (!indexingSourceById.TryGetValue(x.IndexingSourceId, out var dim))
                throw new InvalidOperationException($"IndexingSourceId {x.IndexingSourceId} was not returned by the source.");
            return new FactArticleIndexing { Article = articleByProduct[x.ProductId], IndexingSource = dim };
        }).ToList();
        var metrics = s.Metrics.Where(x => venueById.ContainsKey(x.VenueId)).OrderBy(x => x.VenueId).ThenBy(x => x.Year)
            .Select(x => new FactVenueMetricYear { Venue = venueById[x.VenueId], Year = x.Year,
                Sjr = x.Sjr, Quartile = Clean(x.Quartile) }).ToList();

        return new(dates, authors, journals, productTypes, faculties, databases, quartiles, articles, venues,
            terms, statuses, lines, fields, indexingSources, publications, factAuthors, factIndexings, metrics);
    }

    private static void ValidateSnapshot(Snapshot s)
    {
        Unique(s.Publications, x => x.ProductId, "ProductId");
        Unique(s.Authors, x => x.ProductAuthorId, "ProductAuthorId");
        Unique(s.Authors, x => (x.ProductId, x.AuthorId), "ProductId/AuthorId");
        Unique(s.Indexings, x => (x.ProductId, x.IndexingSourceId), "ProductId/IndexingSourceId");
        Unique(s.Venues, x => x.VenueId, "VenueId");
        Unique(s.Metrics, x => (x.VenueId, x.Year), "VenueId/Year");
        Unique(s.Terms, x => x.AcademicTermId, "AcademicTermId");
        Unique(s.Statuses, x => x.PublicationStatusId, "PublicationStatusId");
        Unique(s.Lines, x => x.ResearchLineId, "ResearchLineId");
        Unique(s.Broad, x => x.BroadFieldId, "BroadFieldId");
        Unique(s.Specific, x => x.SpecificFieldId, "SpecificFieldId");
        Unique(s.Detailed, x => x.DetailedFieldId, "DetailedFieldId");
        Unique(s.Faculties, x => x.FacultyId, "FacultyId");
        Unique(s.IndexingSources, x => x.IndexingSourceId, "IndexingSourceId");
    }

    private static List<DimAuthor> BuildAuthors(IReadOnlyList<ArticlesDwAuthorRow> rows)
    {
        var result = new List<DimAuthor>();
        foreach (var group in rows.GroupBy(x => x.AuthorId).OrderBy(x => x.Key))
        {
            var identities = group.Select(x => new AuthorIdentity(x.IsInstitutional, x.AppUserId, x.IdAsp,
                x.ExternalResearcherId, Clean(x.ExternalFullName), Clean(x.Orcid))).Distinct().ToList();
            if (identities.Count != 1)
                throw new InvalidOperationException($"AuthorId {group.Key} has contradictory canonical identity data.");
            var x = identities[0];
            if (x.IsInstitutional ? x.AppUserId is null || x.ExternalResearcherId is not null
                    : x.ExternalResearcherId is null || x.AppUserId is not null)
                throw new InvalidOperationException($"AuthorId {group.Key} violates the AppUser XOR ExternalResearcher rule.");
            result.Add(new DimAuthor { AuthorId = group.Key, IsInstitutional = x.IsInstitutional,
                AppUserId = x.AppUserId, IdAsp = x.IdAsp, ExternalResearcherId = x.ExternalResearcherId,
                ExternalFullName = x.ExternalFullName, Orcid = x.Orcid });
        }
        return result;
    }

    private static List<DimField> BuildFields(Snapshot s)
    {
        var broad = s.Broad.ToDictionary(x => x.BroadFieldId);
        var specific = s.Specific.ToDictionary(x => x.SpecificFieldId);
        var detailed = s.Detailed.ToDictionary(x => x.DetailedFieldId);
        var result = new List<DimField>();
        var combinations = s.Publications.Where(x => x.BroadFieldId.HasValue)
            .Select(x => (Broad: x.BroadFieldId!.Value, Specific: x.SpecificFieldId, Detailed: x.DetailedFieldId))
            .Distinct().OrderBy(x => x.Broad).ThenBy(x => x.Specific).ThenBy(x => x.Detailed);
        foreach (var ids in combinations)
        {
            if (!broad.TryGetValue(ids.Broad, out var b))
                throw new InvalidOperationException($"BroadFieldId {ids.Broad} was not returned by the source.");
            ArticlesDwSpecificFieldRow? sp = null;
            ArticlesDwDetailedFieldRow? de = null;
            if (ids.Specific.HasValue && (!specific.TryGetValue(ids.Specific.Value, out sp) || sp.BroadFieldId != ids.Broad))
                throw new InvalidOperationException($"SpecificFieldId {ids.Specific} does not belong to BroadFieldId {ids.Broad}.");
            if (ids.Detailed.HasValue && (!detailed.TryGetValue(ids.Detailed.Value, out de) || ids.Specific is null || de.SpecificFieldId != ids.Specific))
                throw new InvalidOperationException($"DetailedFieldId {ids.Detailed} does not belong to SpecificFieldId {ids.Specific}.");
            result.Add(new DimField { BroadFieldId = ids.Broad, BroadFieldName = b.Name,
                SpecificFieldId = ids.Specific, SpecificFieldName = sp?.Name,
                DetailedFieldId = ids.Detailed, DetailedFieldName = de?.Name });
        }
        return result;
    }

    private void StageClear()
    {
        dw.FactArticleAuthors.RemoveRange(dw.FactArticleAuthors);
        dw.FactArticleIndexings.RemoveRange(dw.FactArticleIndexings);
        dw.FactVenueMetricYears.RemoveRange(dw.FactVenueMetricYears);
        dw.FactArticlePublications.RemoveRange(dw.FactArticlePublications);
        dw.DimArticles.RemoveRange(dw.DimArticles); dw.DimVenues.RemoveRange(dw.DimVenues);
        dw.DimAcademicTerms.RemoveRange(dw.DimAcademicTerms); dw.DimPublicationStatuses.RemoveRange(dw.DimPublicationStatuses);
        dw.DimResearchLines.RemoveRange(dw.DimResearchLines); dw.DimFields.RemoveRange(dw.DimFields);
        dw.DimIndexingSources.RemoveRange(dw.DimIndexingSources); dw.DimDates.RemoveRange(dw.DimDates);
        dw.DimAuthors.RemoveRange(dw.DimAuthors); dw.DimJournals.RemoveRange(dw.DimJournals);
        dw.DimProductTypes.RemoveRange(dw.DimProductTypes); dw.DimFaculties.RemoveRange(dw.DimFaculties);
        dw.DimIndexingDatabases.RemoveRange(dw.DimIndexingDatabases); dw.DimQuartiles.RemoveRange(dw.DimQuartiles);
    }

    private void StageLoad(LoadSet x)
    {
        dw.AddRange(x.Dates); dw.AddRange(x.Authors); dw.AddRange(x.Journals); dw.AddRange(x.ProductTypes);
        dw.AddRange(x.Faculties); dw.AddRange(x.Databases); dw.AddRange(x.Quartiles); dw.AddRange(x.Articles);
        dw.AddRange(x.Venues); dw.AddRange(x.Terms); dw.AddRange(x.Statuses); dw.AddRange(x.Lines);
        dw.AddRange(x.Fields); dw.AddRange(x.IndexingSources); dw.AddRange(x.Publications);
        dw.AddRange(x.FactAuthors); dw.AddRange(x.FactIndexings); dw.AddRange(x.Metrics);
    }

    private async Task RollbackAndRecordFailureAsync(IDbContextTransaction? transaction, Guid executionId,
        string code, string message, long duration, WarningCollector warnings)
    {
        if (transaction is not null)
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            catch (Exception ex) { logger.LogError(ex, "Articles DW ETL - rollback failed."); }
        }
        dw.ChangeTracker.Clear();
        try
        {
            await history.CompleteFailureAsync(executionId, code, message,
                new ArticlesDwFullLoadHistoryResult(duration, null, warnings.Snapshot()), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Articles DW ETL - failed to persist FAILED status for {ExecutionId}.", executionId);
        }
    }

    private static bool IsType2(ArticlesDwPublicationRow x) => x.ProductTypeId == (int)BaseProductTypeId.RegionalProduction;
    private static int DateKey(DateTime value) => value.Year * 10000 + value.Month * 100 + value.Day;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static T? Find<T>(IReadOnlyDictionary<string, T> map, string? value) where T : class
        => Clean(value) is { } key && map.TryGetValue(key, out var found) ? found : null;
    private static T? Find<TKey, T>(IReadOnlyDictionary<TKey, T> map, TKey? key) where TKey : struct where T : class
        => key.HasValue && map.TryGetValue(key.Value, out var found) ? found : null;
    private static T? Find<T1, T2, T3, T>(IReadOnlyDictionary<(T1, T2?, T3?), T> map, (T1?, T2?, T3?) key)
        where T1 : struct where T2 : struct where T3 : struct where T : class
        => key.Item1.HasValue && map.TryGetValue((key.Item1.Value, key.Item2, key.Item3), out var found) ? found : null;

    private static List<T> BuildStrings<T>(IEnumerable<string?> values, Func<string, T> factory)
    {
        var cleaned = values.Select(Clean).Where(x => x is not null).Cast<string>()
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var ambiguity = cleaned.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (ambiguity is not null)
            throw new InvalidOperationException($"Ambiguous values differ only by case: {string.Join(", ", ambiguity)}.");
        return cleaned.Select(factory).ToList();
    }

    private static void Unique<T, TKey>(IEnumerable<T> rows, Func<T, TKey> key, string label) where TKey : notnull
    {
        var duplicate = rows.GroupBy(key).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate {label} '{duplicate.Key}' in Articles DW source.");
    }

    private sealed record AuthorIdentity(bool IsInstitutional, int? AppUserId, int? IdAsp,
        int? ExternalResearcherId, string? ExternalFullName, string? Orcid);
    private sealed record Snapshot(ArticlesDwDateBounds DateBounds,
        IReadOnlyList<ArticlesDwPublicationRow> Publications, IReadOnlyList<ArticlesDwAuthorRow> Authors,
        IReadOnlyList<ArticlesDwIndexingRow> Indexings, IReadOnlyList<ArticlesDwVenueRow> Venues,
        IReadOnlyList<ArticlesDwVenueMetricRow> Metrics, IReadOnlyList<ArticlesDwAcademicTermRow> Terms,
        IReadOnlyList<ArticlesDwPublicationStatusRow> Statuses, IReadOnlyList<ArticlesDwResearchLineRow> Lines,
        IReadOnlyList<ArticlesDwBroadFieldRow> Broad, IReadOnlyList<ArticlesDwSpecificFieldRow> Specific,
        IReadOnlyList<ArticlesDwDetailedFieldRow> Detailed, IReadOnlyList<ArticlesDwFacultyRow> Faculties,
        IReadOnlyList<ArticlesDwIndexingSourceRow> IndexingSources);
    private sealed record LoadSet(List<DimDate> Dates, List<DimAuthor> Authors, List<DimJournal> Journals,
        List<DimProductType> ProductTypes, List<DimFaculty> Faculties, List<DimIndexingDatabase> Databases,
        List<DimQuartile> Quartiles, List<DimArticle> Articles, List<DimVenue> Venues,
        List<DimAcademicTerm> Terms, List<DimPublicationStatus> Statuses, List<DimResearchLine> Lines,
        List<DimField> Fields, List<DimIndexingSource> IndexingSources, List<FactArticlePublication> Publications,
        List<FactArticleAuthor> FactAuthors, List<FactArticleIndexing> FactIndexings, List<FactVenueMetricYear> Metrics)
    {
        public ArticlesDwFullLoadCounts Counts => new(Dates.Count, Authors.Count, Journals.Count, ProductTypes.Count,
            Faculties.Count, Databases.Count, Quartiles.Count, Articles.Count, Venues.Count, Terms.Count,
            Statuses.Count, Lines.Count, Fields.Count, IndexingSources.Count, Publications.Count,
            FactAuthors.Count, FactIndexings.Count, Metrics.Count);
    }

    private sealed class WarningCollector
    {
        private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);
        public void Add(string code) => counts[code] = counts.GetValueOrDefault(code) + 1;
        public IReadOnlyList<DwEtlWarningCount> Snapshot() => counts.OrderBy(x => x.Key)
            .Select(x => new DwEtlWarningCount(x.Key, x.Value)).ToList();
    }
}
