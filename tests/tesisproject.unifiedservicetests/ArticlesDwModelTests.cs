using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using tesisproject.backend.Data;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

internal static class ArticlesDwModelTests
{
    public static void Run(Action<bool, string> check)
    {
        var options = new DbContextOptionsBuilder<ArticlesDwContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ArticlesDwMetadataOnly;Trusted_Connection=True")
            .Options;
        using var context = new ArticlesDwContext(options);
        var model = context.Model;

        var article = Entity<DimArticle>(model);
        check(HasUniqueIndex(article, nameof(DimArticle.ProductId)), "DimArticle ProductId must be unique.");
        check(article.FindProperty(nameof(DimArticle.ArticleId))!.IsNullable, "DimArticle ArticleId must be nullable.");
        check(article.FindProperty("IsCurrent") is null, "DimArticle must not implement an SCD IsCurrent column.");

        var publication = Entity<FactArticlePublication>(model);
        check(publication.FindProperty(nameof(FactArticlePublication.ProjectId))!.IsNullable, "Article ProjectId must be nullable.");
        check(publication.FindProperty(nameof(FactArticlePublication.IsOpenAccessFlag))!.IsNullable
            && publication.FindProperty(nameof(FactArticlePublication.HasInterculturalFlag))!.IsNullable,
            "Nullable structural flags must preserve absence.");
        check(HasUniqueIndex(publication, nameof(FactArticlePublication.ArticleKey))
            && HasUniqueIndex(publication, nameof(FactArticlePublication.ProductId)),
            "ArticleKey and ProductId must each be unique in FactArticlePublications.");

        CheckForeignKey<FactArticlePublication, DimFaculty>(publication, nameof(FactArticlePublication.ArticleFacultyKey), check);
        CheckForeignKey<FactArticlePublication, DimFaculty>(publication, nameof(FactArticlePublication.ProjectFacultyKey), check);
        CheckForeignKey<FactArticlePublication, DimJournal>(publication, nameof(FactArticlePublication.JournalKey), check);
        CheckForeignKey<FactArticlePublication, DimVenue>(publication, nameof(FactArticlePublication.VenueKey), check);
        CheckForeignKey<FactArticlePublication, DimIndexingDatabase>(publication, nameof(FactArticlePublication.IndexingDatabaseKey), check);
        check(publication.GetForeignKeys().All(x => x.Properties.All(p => p.Name != nameof(FactArticlePublication.ProjectId))),
            "Article ProjectId must remain lineage without a cross-DW foreign key.");

        var author = Entity<FactArticleAuthor>(model);
        check(HasUniqueIndex(author, nameof(FactArticleAuthor.ProductAuthorId)), "ProductAuthorId must be unique.");
        check(HasUniqueIndex(author, nameof(FactArticleAuthor.ArticleKey), nameof(FactArticleAuthor.AuthorKey)),
            "An author must occur once per article.");
        CheckForeignKey<FactArticleAuthor, DimAuthor>(author, nameof(FactArticleAuthor.AuthorKey), check);

        var indexing = Entity<FactArticleIndexing>(model);
        check(HasUniqueIndex(indexing, nameof(FactArticleIndexing.ArticleKey), nameof(FactArticleIndexing.IndexingSourceKey)),
            "Article indexing relation must be unique.");
        CheckForeignKey<FactArticleIndexing, DimIndexingSource>(indexing, nameof(FactArticleIndexing.IndexingSourceKey), check);

        var venueMetric = Entity<FactVenueMetricYear>(model);
        check(HasUniqueIndex(venueMetric, nameof(FactVenueMetricYear.VenueKey), nameof(FactVenueMetricYear.Year)),
            "Venue metric must be unique by venue and year.");

        check(model.FindEntityType("tesisproject.shared.Entities.Analytics.Dw.Dimensions.DimProject") is null,
            "Articles DW must not introduce DimProject.");
        check(model.FindEntityType(typeof(FactProject)) is null,
            "Articles DW must not discover Projects facts.");
        check(model.GetEntityTypes().All(x => x.FindProperty("RegistrationSourceKey") is null),
            "Articles DW must not introduce RegistrationSource.");
        check(model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()).All(x => x.DeleteBehavior == DeleteBehavior.NoAction),
            "DW foreign keys must use NoAction delete behavior.");

        foreach (var conformed in new[]
        {
            typeof(DimDate), typeof(DimAuthor), typeof(DimJournal), typeof(DimProductType),
            typeof(DimFaculty), typeof(DimIndexingDatabase), typeof(DimQuartile)
        })
            check(model.GetEntityTypes().Count(x => x.ClrType == conformed) == 1,
                $"Conformed dimension {conformed.Name} must be reused exactly once.");
    }

    private static IEntityType Entity<T>(IModel model) =>
        model.FindEntityType(typeof(T)) ?? throw new InvalidOperationException($"Missing DW entity {typeof(T).Name}.");

    private static bool HasUniqueIndex(IEntityType entity, params string[] propertyNames) =>
        entity.GetIndexes().Any(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(propertyNames));

    private static IForeignKey ForeignKey(IEntityType entity, string propertyName) =>
        entity.GetForeignKeys().Single(x => x.Properties.Count == 1 && x.Properties[0].Name == propertyName);

    private static void CheckForeignKey<TDependent, TPrincipal>(
        IEntityType dependent,
        string propertyName,
        Action<bool, string> check)
    {
        var foreignKey = ForeignKey(dependent, propertyName);
        check(foreignKey.PrincipalEntityType.ClrType == typeof(TPrincipal),
            $"{typeof(TDependent).Name}.{propertyName} must reference {typeof(TPrincipal).Name}.");
    }
}
