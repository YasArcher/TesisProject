using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Services.Unified.Implementations;

internal static class ArticleFormCatalogTests
{
    internal static async Task RunAsync(Action<bool, string> check)
    {
        await using var context = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseInMemoryDatabase($"ArticleFormCatalogs-{Guid.NewGuid()}").Options);
        context.AddRange(
            new FieldCatalogEntry { FieldId = 11, EntityName = "Article", FieldKey = "AcademicTermId", FieldLabel = "Periodo academico", DataType = "fk", SourceType = "CatalogReference", ReferenceTableName = "dbo.AcademicTerms", IsActive = true, IsVisible = true },
            new FieldCatalogEntry { FieldId = 12, EntityName = "Article", FieldKey = "FacultyId", FieldLabel = "Facultad", DataType = "fk", SourceType = "CatalogReference", ReferenceTableName = "dbo.Faculties", IsActive = true, IsVisible = true },
            new AcademicTerm { AcademicTermId = 42, Name = "2025-1" },
            new Faculty { FacultyId = 7, Name = "Ingenieria" });
        await context.SaveChangesAsync();

        var service = new UnifiedArticleConfigurationService(
            new UnifiedArticleConfigurationRepository(context), null!, null!);
        var terms = await service.GetCatalogItems(11, null, CancellationToken.None);
        check(terms.Success && terms.Data?.SingleOrDefault() is { Id: 42, Name: "2025-1" },
            "A schema-qualified AcademicTerms reference must return the configured article form options.");
        var faculties = await service.GetCatalogItems(12, null, CancellationToken.None);
        check(faculties.Success && faculties.Data?.SingleOrDefault() is { Id: 7, Name: "Ingenieria" },
            "A schema-qualified Faculties reference must return the configured article form options.");
    }
}
