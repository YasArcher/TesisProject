using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Implementations.DataMigrations;
using tesisproject.backend.Services.Unified.Interfaces;

namespace tesisproject.backend.Services.Unified.DataMigrations.ArticlesInitialCatalog;

public sealed class UnifiedArticlesCatalogsV1(UnifiedDideDbContext context) : IDataMigration
{
    public string Code => OperationCodes.UnifiedArticlesCatalogsV1;
    public string Description => "Catálogos iniciales canónicos de Articles Unified.";
    public string Version => "1";
    public int Order => 200;

    public async Task<object?> ApplyAsync(CancellationToken ct = default)
    {
        await ValidateProjectsCatalogDependencyAsync(ct);
        var sections = new List<ArticlesCatalogSeedSectionResult>(ArticlesCatalogSeedData.Tables.Count);
        foreach (var table in ArticlesCatalogSeedData.Tables)
            sections.Add(await table.ApplyAsync(context, ct));

        return new UnifiedArticlesCatalogsResult(
            sections.Sum(x => x.Inserted),
            sections.Sum(x => x.Unchanged),
            sections.ToDictionary(x => x.Catalog, x => x.Total, StringComparer.Ordinal));
    }

    private async Task ValidateProjectsCatalogDependencyAsync(CancellationToken ct)
    {
        var productTypes = await context.ProductTypes.AsNoTracking()
            .Where(x => x.Id == 1 || x.Id == 2)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        if (productTypes.Count != 2 ||
            productTypes[0] is not { Id: 1, Name: "PRODUCCIÓN CIENTÍFICA", IsActive: true, IsLocked: false } ||
            productTypes[1] is not { Id: 2, Name: "PRODUCCIÓN REGIONAL", IsActive: true, IsLocked: false })
            DependencyFailure("ProductTypes 1 and 2");

        var expectedAttributes = new Dictionary<int, (int DataType, string Name)>
        {
            [1] = (0, "TÍTULO"),
            [2] = (0, "AUTORES"),
            [3] = (0, "REVISTA"),
            [4] = (0, "BASE DE DATOS"),
            [5] = (1, "IMPACTO / SJR"),
            [6] = (0, "CUARTIL"),
            [7] = (0, "ISSN / ISBN"),
            [8] = (0, "DOI"),
            [9] = (1, "AÑO"),
            [10] = (3, "URL A LA FECHA DE CONSULTA")
        };
        var attributes = await context.ProductAttributes.AsNoTracking()
            .Where(x => x.Id >= 1 && x.Id <= 10)
            .ToListAsync(ct);
        if (attributes.Count != expectedAttributes.Count || attributes.Any(x =>
                !expectedAttributes.TryGetValue(x.Id, out var expected) ||
                (int)x.DataType != expected.DataType || x.Name != expected.Name || x.Unit != null ||
                !x.IsActive || x.IsLocked))
            DependencyFailure("ProductAttributes 1-10");

        var expectedDefinitions = new Dictionary<int, (int ProductTypeId, int ProductAttributeId, int DisplayOrder)>
        {
            [1] = (1, 1, 1), [2] = (1, 2, 2), [3] = (1, 3, 3), [4] = (1, 4, 4),
            [5] = (1, 5, 5), [6] = (1, 6, 6), [7] = (1, 7, 7), [8] = (1, 8, 8),
            [9] = (1, 9, 9), [10] = (1, 10, 10),
            [11] = (2, 1, 1), [12] = (2, 2, 2), [13] = (2, 3, 3), [14] = (2, 4, 4),
            [15] = (2, 7, 5), [16] = (2, 10, 6)
        };
        var definitions = await context.ProductAttributeDefinitions.AsNoTracking()
            .Where(x => x.ProductTypeId == 1 || x.ProductTypeId == 2)
            .ToListAsync(ct);
        if (definitions.Count != expectedDefinitions.Count || definitions.Any(x =>
                !expectedDefinitions.TryGetValue(x.Id, out var expected) ||
                x.ProductTypeId != expected.ProductTypeId ||
                x.ProductAttributeId != expected.ProductAttributeId ||
                x.DisplayOrder != expected.DisplayOrder || x.IsRequired))
            DependencyFailure("ProductAttributeDefinitions for ProductTypes 1 and 2");
    }

    private static void DependencyFailure(string catalog)
        => throw new InvalidOperationException(
            $"PROJECTS_INITIAL_CATALOG_V1 dependency is missing or inconsistent: {catalog}.");
}

internal sealed record UnifiedArticlesCatalogsResult(
    int Inserted,
    int Unchanged,
    IReadOnlyDictionary<string, int> Catalogs);
