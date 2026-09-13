using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.shared.Enums;

public static class ArticlesDwSourceTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        var connectionString = Environment.GetEnvironmentVariable("ARTICLES_DW_SOURCE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            check(typeof(IUnifiedArticlesDwSource).GetMethods().Length == 14,
                "Articles DW source contract exposes fourteen batch reads.");
            return;
        }

        var counter = new QueryCounter();
        var options = new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connectionString)
            .AddInterceptors(counter)
            .Options;

        await using var context = new UnifiedDideDbContext(options);
        await context.Database.OpenConnectionAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var token = Guid.NewGuid().ToString("N");
        var project = await context.Projects.AsNoTracking().OrderBy(x => x.ProjectId).FirstAsync();
        var articleFaculty = new Faculty { Name = $"Article faculty {token}", Acronym = $"A{token[..6]}" };
        var venue = new Venue
        {
            Name = $"Venue {token}", IssnCode = $"ISSN-{token[..8]}", IssueNumber = "7",
            VolumeNumber = "11", JournalUrl = $"https://venue.test/{token}", Type = "Journal"
        };
        var term = new AcademicTerm { Name = $"Term {token}" };
        var status = new PublicationStatus { Name = $"S{token[..8]}" };
        var line = new ResearchLine { Name = $"Research line {token}" };
        var broad = new BroadField { Name = $"Broad {token}" };
        var specific = new SpecificField { Name = $"Specific {token}", BroadField = broad, Code = token[..8] };
        var detailed = new DetailedField { Name = $"Detailed {token}", SpecificField = specific, Code = token[8..16] };
        var indexingSource = new IndexingSource
        {
            Name = $"Normalized source {token}", Abbreviation = $"N{token[..5]}",
            ReferenceUrl = $"https://index.test/{token}", IsActive = true
        };
        var appUser = new AppUser { IdAsp = await NextIdAspAsync(context) };
        var externalResearcher = new ExternalResearcher
        {
            FullName = $"External {token}", Email = $"{token}@external.test"
        };

        context.AddRange(articleFaculty, venue, term, status, line, specific, detailed,
            indexingSource, appUser, externalResearcher);
        await context.SaveChangesAsync();

        var institutionalAuthor = new Author { AppUserId = appUser.IdUser, Orcid = $"I-{token}" };
        var externalAuthor = new Author { ExternalResearcherId = externalResearcher.ExternalResearcherId, Orcid = $"E-{token}" };
        context.AddRange(institutionalAuthor, externalAuthor);
        await context.SaveChangesAsync();

        var withArticle = new Product
        {
            ProductTypeId = (int)BaseProductTypeId.ScientificProduction,
            ProjectId = project.ProjectId,
            Title = $"Article product {token}",
            CreatedAt = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc)
        };
        var withoutArticle = new Product
        {
            ProductTypeId = (int)BaseProductTypeId.ScientificProduction,
            Title = $"No extension {token}",
            CreatedAt = new DateTime(2024, 3, 4, 5, 6, 7, DateTimeKind.Utc)
        };
        var regional = new Product
        {
            ProductTypeId = (int)BaseProductTypeId.RegionalProduction,
            Title = $"Regional {token}",
            CreatedAt = new DateTime(2024, 4, 5, 6, 7, 8, DateTimeKind.Utc)
        };
        context.AddRange(withArticle, withoutArticle, regional);
        await context.SaveChangesAsync();

        var definitions = await context.ProductAttributeDefinitions.AsNoTracking()
            .Where(x => x.ProductTypeId == (int)BaseProductTypeId.ScientificProduction ||
                        x.ProductTypeId == (int)BaseProductTypeId.RegionalProduction)
            .ToDictionaryAsync(x => (x.ProductTypeId, x.ProductAttributeId));

        AddValues(context, withArticle, definitions, new Dictionary<BaseProductAttributeId, string>
        {
            [BaseProductAttributeId.Journal] = $"Journal {token}",
            [BaseProductAttributeId.IndexingDatabase] = $"Text database {token}",
            [BaseProductAttributeId.Sjr] = "3.125",
            [BaseProductAttributeId.Quartile] = "Q1",
            [BaseProductAttributeId.IssnIsbn] = $"ISSN-P-{token[..8]}",
            [BaseProductAttributeId.Doi] = $"10.9999/{token}",
            [BaseProductAttributeId.Year] = "2024",
            [BaseProductAttributeId.ConsultationUrl] = $"https://publication.test/{token}"
        });
        AddValues(context, withoutArticle, definitions, new Dictionary<BaseProductAttributeId, string>
        {
            [BaseProductAttributeId.Journal] = $"No extension journal {token}",
            [BaseProductAttributeId.Doi] = $"10.9998/{token}",
            [BaseProductAttributeId.Year] = "2023"
        });
        AddValues(context, regional, definitions, new Dictionary<BaseProductAttributeId, string>
        {
            [BaseProductAttributeId.Journal] = $"Regional journal {token}",
            [BaseProductAttributeId.IndexingDatabase] = $"Regional database {token}",
            [BaseProductAttributeId.IssnIsbn] = $"R-{token[..8]}",
            [BaseProductAttributeId.ConsultationUrl] = $"https://regional.test/{token}"
        });

        var article = new Article
        {
            ProductId = withArticle.Id,
            PublishedAt = new DateTime(2024, 5, 6, 0, 0, 0, DateTimeKind.Utc),
            PageCount = 12,
            IsOpenAccess = true,
            HasInterculturalComponent = true,
            VenueId = venue.VenueId,
            AcademicTermId = term.AcademicTermId,
            PublicationStatusId = status.PublicationStatusId,
            ResearchLineId = line.ResearchLineId,
            BroadFieldId = broad.BroadFieldId,
            SpecificFieldId = specific.SpecificFieldId,
            DetailedFieldId = detailed.DetailedFieldId,
            FacultyId = articleFaculty.FacultyId,
            ProceedingsName = $"Proceedings {token}",
            Proceedings = $"Collection {token}",
            EventName = $"Event {token}",
            GroupName = $"Group {token}",
            Filiacion = $"Affiliation {token}",
            ExternalSource = "QA",
            ExternalId = token
        };
        context.Add(article);
        context.AddRange(
            new ProductAuthor
            {
                ProductId = withArticle.Id, AuthorId = institutionalAuthor.AuthorId,
                AuthorOrder = 1, IsPrimaryAuthor = true, Participation = "Autor",
                NameSnapshot = $"Institutional snapshot {token}", AffiliationSnapshot = "UTA"
            },
            new ProductAuthor
            {
                ProductId = withArticle.Id, AuthorId = externalAuthor.AuthorId,
                AuthorOrder = 2, Participation = "Coautor",
                NameSnapshot = $"External snapshot {token}", AffiliationSnapshot = "External institute"
            },
            new ProductAuthor
            {
                ProductId = withoutArticle.Id, AuthorId = institutionalAuthor.AuthorId,
                AuthorOrder = 1, IsPrimaryAuthor = true,
                NameSnapshot = $"No extension author {token}"
            });
        context.Add(new ArticleIndexing { Article = article, IndexingSourceId = indexingSource.Id });
        context.Add(new VenueMetric { VenueId = venue.VenueId, Year = 2024, SJR = 8.75m, Quartile = "Q3" });
        await context.SaveChangesAsync();

        var source = new UnifiedArticlesDwSource(context);
        counter.Reset();
        var publications = await OneQueryAsync(() => source.ListArticlePublicationsAsync(), counter, check, "publications");
        var ids = new[] { withArticle.Id, withoutArticle.Id, regional.Id };
        var selected = publications.Where(x => ids.Contains(x.ProductId)).ToDictionary(x => x.ProductId);

        check(selected.Count == 3, "All type 1/2 Products must appear, including Products without Article extension.");
        var publication = selected[withArticle.Id];
        check(publication.ArticleId == article.Id && publication.ProductId == withArticle.Id && publication.IsProjectResult,
            "ProductId is publication identity; ArticleId remains nullable extension identity.");
        check(publication.Journal == $"Journal {token}" && publication.Journal != venue.Name &&
              publication.IndexingDatabase == $"Text database {token}" && publication.IndexingDatabase != indexingSource.Name,
            "Journal/Venue and textual/normalized indexing sources must remain separate.");
        check(publication.ArticleFacultyId == articleFaculty.FacultyId &&
              publication.ProjectFacultyId == project.FacultyId &&
              publication.ArticleFacultyId != publication.ProjectFacultyId,
            "Article and Project faculties must remain separate.");

        var noExtension = selected[withoutArticle.Id];
        check(noExtension.ArticleId is null && noExtension.PublishedAt is null && noExtension.PageCount is null &&
              noExtension.IsOpenAccess is null && noExtension.HasInterculturalComponent is null &&
              noExtension.VenueId is null && noExtension.AcademicTermId is null &&
              noExtension.PublicationStatusId is null && noExtension.ResearchLineId is null &&
              noExtension.BroadFieldId is null && noExtension.SpecificFieldId is null && noExtension.DetailedFieldId is null,
            "Products without Article extension must preserve structural nulls.");
        check(noExtension.Journal == $"No extension journal {token}" && noExtension.Doi == $"10.9998/{token}" &&
              noExtension.PublicationYear == 2023,
            "Products without Article extension must preserve canonical ProductValues.");

        var regionalPublication = selected[regional.Id];
        check(regionalPublication.Sjr is null && regionalPublication.SjrRaw is null &&
              regionalPublication.Quartile is null && regionalPublication.Doi is null &&
              regionalPublication.PublicationYear is null && regionalPublication.PublicationYearRaw is null,
            "RegionalProduction must not infer attributes 5, 6, 8 or 9.");

        var authors = await OneQueryAsync(() => source.ListArticleAuthorsAsync(), counter, check, "authors");
        var productAuthors = authors.Where(x => x.ProductId == withArticle.Id).OrderBy(x => x.AuthorOrder).ToList();
        check(productAuthors.Count == 2 && productAuthors[0].IsInstitutional &&
              productAuthors[0].AppUserId == appUser.IdUser && productAuthors[0].IdAsp == appUser.IdAsp &&
              productAuthors[0].NameSnapshot == $"Institutional snapshot {token}" && productAuthors[0].AffiliationSnapshot == "UTA",
            "Institutional author must use AppUser.IdUser/IdAsp and preserve ProductAuthor snapshots.");
        check(!productAuthors[1].IsInstitutional && productAuthors[1].ExternalResearcherId == externalResearcher.ExternalResearcherId &&
              productAuthors[1].ExternalFullName == externalResearcher.FullName,
            "External author must resolve through ExternalResearcher.");
        check(authors.Any(x => x.ProductId == withoutArticle.Id && x.AuthorId == institutionalAuthor.AuthorId),
            "Products without Article extension must preserve ProductAuthors.");

        var indexings = await OneQueryAsync(() => source.ListArticleIndexingsAsync(), counter, check, "indexings");
        check(indexings.Any(x => x.ProductId == withArticle.Id && x.IndexingSourceId == indexingSource.Id && x.Name == indexingSource.Name),
            "Normalized ArticleIndexing must load independently from ProductValue attribute 4.");
        check(indexings.All(x => x.ProductId != withoutArticle.Id),
            "Product without Article extension must have no ArticleIndexing rows.");

        var venues = await OneQueryAsync(() => source.ListVenuesAsync(), counter, check, "venues");
        check(venues.Any(x => x.VenueId == venue.VenueId && x.Name == venue.Name && x.Issue == "7" && x.Volume == "11"),
            "Venue projection must preserve structural catalog values.");
        var metrics = await OneQueryAsync(() => source.ListVenueMetricsAsync(), counter, check, "venue metrics");
        check(metrics.Any(x => x.VenueId == venue.VenueId && x.Year == 2024 && x.Sjr == 8.75m && x.Quartile == "Q3") &&
              publication.Sjr == 3.125m && publication.Quartile == "Q1",
            "VenueMetric and ProductValue quality metrics must remain independent.");

        await OneQueryAsync(() => source.ListAcademicTermsAsync(), counter, check, "academic terms");
        await OneQueryAsync(() => source.ListPublicationStatusesAsync(), counter, check, "publication statuses");
        await OneQueryAsync(() => source.ListResearchLinesAsync(), counter, check, "research lines");
        await OneQueryAsync(() => source.ListBroadFieldsAsync(), counter, check, "broad fields");
        await OneQueryAsync(() => source.ListSpecificFieldsAsync(), counter, check, "specific fields");
        await OneQueryAsync(() => source.ListDetailedFieldsAsync(), counter, check, "detailed fields");
        await OneQueryAsync(() => source.ListFacultiesAsync(), counter, check, "faculties");
        await OneQueryAsync(() => source.ListIndexingSourcesAsync(), counter, check, "indexing sources");
        var bounds = await OneQueryAsync(() => source.GetDateBoundsAsync(), counter, check, "date bounds");
        check(bounds.MinProductCreatedAt <= withArticle.CreatedAt && bounds.MaxProductCreatedAt >= regional.CreatedAt &&
              bounds.MinPublishedAt <= article.PublishedAt && bounds.MaxPublishedAt >= article.PublishedAt,
            "Date bounds must use Product.CreatedAt and Article.PublishedAt only.");

        await transaction.RollbackAsync();
    }

    private static async Task<int> NextIdAspAsync(UnifiedDideDbContext context) =>
        (await context.AppUsers.MaxAsync(x => (int?)x.IdAsp) ?? 900000000) + 1;

    private static void AddValues(
        UnifiedDideDbContext context,
        Product product,
        IReadOnlyDictionary<(int ProductTypeId, int ProductAttributeId), ProductAttributeDefinition> definitions,
        IReadOnlyDictionary<BaseProductAttributeId, string> values)
    {
        foreach (var pair in values)
        {
            var definition = definitions[(product.ProductTypeId, (int)pair.Key)];
            context.ProductValues.Add(new ProductValue
            {
                ProductId = product.Id,
                AttributeDefinitionId = definition.Id,
                Value = pair.Value
            });
        }
    }

    private static async Task<T> OneQueryAsync<T>(
        Func<Task<T>> operation,
        QueryCounter counter,
        Action<bool, string> check,
        string operationName)
    {
        var before = counter.ReaderCommands;
        var result = await operation();
        check(counter.ReaderCommands - before == 1, $"Articles DW {operationName} must execute one batch query.");
        return result;
    }

    private sealed class QueryCounter : DbCommandInterceptor
    {
        public int ReaderCommands { get; private set; }
        public void Reset() => ReaderCommands = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ReaderCommands++;
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommands++;
            return ValueTask.FromResult(result);
        }
    }
}

