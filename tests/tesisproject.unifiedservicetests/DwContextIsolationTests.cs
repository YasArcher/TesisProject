using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers;
using tesisproject.backend.Data;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

internal static class DwContextIsolationTests
{
    public static void Run(Action<bool, string> check)
    {
        using var projects = new ProjectsDwContext(new DbContextOptionsBuilder<ProjectsDwContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ProjectsDwMetadata;Trusted_Connection=True")
            .Options);
        using var articles = new ArticlesDwContext(new DbContextOptionsBuilder<ArticlesDwContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ArticlesDwMetadata;Trusted_Connection=True")
            .Options);

        var projectsModel = projects.Model;
        var articlesModel = articles.Model;

        check(projectsModel.GetEntityTypes().All(x => x.GetSchema() == "ProjectsDW"),
            "ProjectsDwContext must map every physical entity to ProjectsDW.");
        check(articlesModel.GetEntityTypes().All(x => x.GetSchema() == "ArticlesDW"),
            "ArticlesDwContext must map every physical entity to ArticlesDW.");

        foreach (var articleType in new[]
        {
            typeof(DimArticle), typeof(DimVenue), typeof(DimAcademicTerm),
            typeof(DimPublicationStatus), typeof(DimResearchLine), typeof(DimField),
            typeof(DimIndexingSource), typeof(FactArticlePublication),
            typeof(FactArticleAuthor), typeof(FactArticleIndexing), typeof(FactVenueMetricYear)
        })
            check(projectsModel.FindEntityType(articleType) is null,
                $"ProjectsDwContext must not contain {articleType.Name}.");

        foreach (var projectType in new[]
        {
            typeof(DimProjectState), typeof(DimFundingType), typeof(DimResearchCategory),
            typeof(FactProject), typeof(FactBudget), typeof(FactProduct),
            typeof(BridgeProjectResearchCategory), typeof(BridgeProductAuthor)
        })
            check(articlesModel.FindEntityType(projectType) is null,
                $"ArticlesDwContext must not contain {projectType.Name}.");

        foreach (var sharedType in new[]
        {
            typeof(DimDate), typeof(DimAuthor), typeof(DimJournal), typeof(DimProductType),
            typeof(DimFaculty), typeof(DimIndexingDatabase), typeof(DimQuartile)
        })
        {
            var projectMapping = projectsModel.FindEntityType(sharedType)!;
            var articleMapping = articlesModel.FindEntityType(sharedType)!;
            check(projectMapping.GetTableName() == articleMapping.GetTableName()
                  && projectMapping.GetSchema() == "ProjectsDW"
                  && articleMapping.GetSchema() == "ArticlesDW",
                $"{sharedType.Name} must have independent physical tables in both warehouses.");
        }

        check(NoCrossWarehouseForeignKeys(projectsModel) && NoCrossWarehouseForeignKeys(articlesModel),
            "No DW context may define a cross-warehouse foreign key.");
        check(typeof(ProjectsDwContext).Assembly.GetType("tesisproject.backend.Data.DwContext") is null,
            "Generic DwContext compatibility alias must not remain.");

        var fullLoadRoutes = typeof(ProjectsDwEtlController).GetMethod(nameof(ProjectsDwEtlController.RunFull))!
            .GetCustomAttributes(typeof(HttpPostAttribute), false)
            .Cast<HttpPostAttribute>()
            .Select(x => x.Template)
            .ToArray();
        check(fullLoadRoutes.Contains("~/api/etl/projects/full-load"),
            "Projects full-load must expose the explicit Projects endpoint.");
    }

    private static bool NoCrossWarehouseForeignKeys(IModel model) =>
        model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()).All(fk =>
            fk.DeclaringEntityType.GetSchema() == fk.PrincipalEntityType.GetSchema());
}
