using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using tesisproject.backend.Data;

namespace tesisproject.backend.Services.Unified.Implementations.DataMigrations;

internal sealed record ArticlesCatalogSeedSectionResult(string Catalog, int Total, int Inserted, int Unchanged);

internal interface IArticlesCatalogSeedTable
{
    Task<ArticlesCatalogSeedSectionResult> ApplyAsync(UnifiedDideDbContext context, CancellationToken ct);
}

internal sealed class ArticlesCatalogSeedTable<TEntity, TSeed>(
    string name,
    string keyPropertyName,
    IReadOnlyList<TSeed> rows,
    params string[][] businessKeys) : IArticlesCatalogSeedTable
    where TEntity : class
{
    private static readonly PropertyInfo[] SeedProperties = typeof(TSeed).GetProperties();

    public async Task<ArticlesCatalogSeedSectionResult> ApplyAsync(
        UnifiedDideDbContext context,
        CancellationToken ct)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} is not mapped.");
        var seedProperties = SeedProperties.ToDictionary(x => x.Name, StringComparer.Ordinal);
        var entityProperties = SeedProperties.ToDictionary(
            x => x.Name,
            x => typeof(TEntity).GetProperty(x.Name) ?? throw new InvalidOperationException(
                $"{typeof(TEntity).Name}.{x.Name} is not mapped by the seed."),
            StringComparer.Ordinal);
        var keyProperty = seedProperties.TryGetValue(keyPropertyName, out var property)
            ? property
            : throw new InvalidOperationException($"Seed key {keyPropertyName} is not mapped.");
        var existing = await context.Set<TEntity>().AsNoTracking().ToListAsync(ct);
        var byKey = existing.ToDictionary(x => Normalize(entityProperties[keyPropertyName].GetValue(x))!);
        var missing = new List<TSeed>();
        var unchanged = 0;

        ValidateSeedBusinessKeys(seedProperties);
        foreach (var row in rows)
        {
            var key = Normalize(keyProperty.GetValue(row))!;
            if (byKey.TryGetValue(key, out var entity))
            {
                var mismatch = SeedProperties.FirstOrDefault(seedProperty =>
                    !Equals(
                        Normalize(seedProperty.GetValue(row)),
                        Normalize(entityProperties[seedProperty.Name].GetValue(entity))));
                if (mismatch is not null)
                    throw new InvalidOperationException(
                        $"CATALOG_ID_CONFLICT: {name} key {key}, field {mismatch.Name}.");
                unchanged++;
                continue;
            }

            foreach (var businessKey in businessKeys)
            {
                var seedBusinessKey = ComposeKey(row!, businessKey, seedProperties);
                var conflicting = existing.FirstOrDefault(candidate =>
                    ComposeKey(candidate, businessKey, entityProperties) == seedBusinessKey);
                if (conflicting is not null)
                {
                    var conflictingId = Normalize(entityProperties[keyPropertyName].GetValue(conflicting));
                    throw new InvalidOperationException(
                        $"CATALOG_ID_CONFLICT: {name} business key {seedBusinessKey} uses IDs {conflictingId} and {key}.");
                }
            }
            missing.Add(row);
        }

        if (missing.Count > 0)
            await BulkInsertAsync(
                context,
                entityType.GetSchema() ?? "dbo",
                entityType.GetTableName() ?? name,
                missing,
                ct);
        return new(name, rows.Count, missing.Count, unchanged);
    }

    private void ValidateSeedBusinessKeys(IReadOnlyDictionary<string, PropertyInfo> seedProperties)
    {
        foreach (var businessKey in businessKeys)
        {
            var duplicate = rows.GroupBy(x => ComposeKey(x!, businessKey, seedProperties), StringComparer.Ordinal)
                .FirstOrDefault(x => x.Count() > 1);
            if (duplicate is not null)
                throw new InvalidOperationException($"Duplicate seed business key in {name}: {duplicate.Key}.");
        }
    }

    private static string ComposeKey(
        object instance,
        IReadOnlyList<string> names,
        IReadOnlyDictionary<string, PropertyInfo> properties)
        => string.Join('\u001f', names.Select(x => Convert.ToString(
            Normalize(properties[x].GetValue(instance)),
            System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"));

    private static async Task BulkInsertAsync(
        UnifiedDideDbContext context,
        string schema,
        string table,
        IReadOnlyCollection<TSeed> rowsToInsert,
        CancellationToken ct)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        var transaction = (SqlTransaction?)context.Database.CurrentTransaction?.GetDbTransaction();
        using var bulk = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints,
            transaction)
        {
            DestinationTableName = $"[{schema}].[{table}]",
            BatchSize = 500
        };
        var data = new DataTable();
        foreach (var property in SeedProperties)
        {
            data.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            bulk.ColumnMappings.Add(property.Name, property.Name);
        }
        foreach (var row in rowsToInsert)
            data.Rows.Add(SeedProperties.Select(x => x.GetValue(row) ?? DBNull.Value).ToArray());
        await bulk.WriteToServerAsync(data, ct);
    }

    private static object? Normalize(object? value)
        => value is Enum enumValue ? Convert.ToInt32(enumValue) : value;
}
