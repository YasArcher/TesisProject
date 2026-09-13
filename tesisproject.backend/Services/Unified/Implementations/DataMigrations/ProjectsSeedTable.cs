using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using tesisproject.backend.Data;

namespace tesisproject.backend.Services.Unified.Implementations.DataMigrations;

internal sealed record ProjectsSeedSectionResult(string Table, int Total, int Inserted, int Unchanged);

internal interface IProjectsSeedTable
{
    string Name { get; }
    int Count { get; }
    Task<ProjectsSeedSectionResult> ApplyAsync(UnifiedDideDbContext context, CancellationToken ct);
}

internal sealed class ProjectsSeedTable<TEntity, TSeed>(string name, IReadOnlyList<TSeed> rows)
    : IProjectsSeedTable where TEntity : class
{
    private static readonly PropertyInfo[] SeedProperties = typeof(TSeed).GetProperties();
    private static readonly PropertyInfo KeyProperty = SeedProperties.First(x => x.Name is "Id" or "ExternalResearcherId");
    public string Name => name;
    public int Count => rows.Count;

    public async Task<ProjectsSeedSectionResult> ApplyAsync(UnifiedDideDbContext context, CancellationToken ct)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} is not mapped.");
        var entityProperties = SeedProperties.ToDictionary(x => x.Name,
            x => typeof(TEntity).GetProperty(x.Name) ?? throw new InvalidOperationException(
                $"{typeof(TEntity).Name}.{x.Name} is not mapped by the seed."), StringComparer.Ordinal);
        var existing = await context.Set<TEntity>().AsNoTracking().ToListAsync(ct);
        var byKey = existing.ToDictionary(x => Normalize(entityProperties[KeyProperty.Name].GetValue(x))!);
        var missing = new List<TSeed>();
        var unchanged = 0;

        foreach (var row in rows)
        {
            var key = Normalize(KeyProperty.GetValue(row))!;
            if (!byKey.TryGetValue(key, out var entity))
            {
                missing.Add(row);
                continue;
            }
            var mismatch = SeedProperties.FirstOrDefault(property =>
                !Equals(Normalize(property.GetValue(row)), Normalize(entityProperties[property.Name].GetValue(entity))));
            if (mismatch is not null)
                throw new InvalidOperationException($"Ambiguous existing data in {Name}, key {key}, field {mismatch.Name}.");
            unchanged++;
        }

        if (missing.Count > 0)
            await BulkInsertAsync(context, entityType.GetSchema() ?? "dbo", entityType.GetTableName() ?? Name, missing, ct);
        return new(Name, rows.Count, missing.Count, unchanged);
    }

    private static async Task BulkInsertAsync(UnifiedDideDbContext context, string schema, string table,
        IReadOnlyCollection<TSeed> rowsToInsert, CancellationToken ct)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        var transaction = (SqlTransaction?)context.Database.CurrentTransaction?.GetDbTransaction();
        using var bulk = new SqlBulkCopy(connection,
            SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints, transaction)
        {
            DestinationTableName = $"[{schema}].[{table}]",
            BatchSize = 500
        };
        var data = new DataTable();
        foreach (var property in SeedProperties)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            data.Columns.Add(property.Name, type);
            bulk.ColumnMappings.Add(property.Name, property.Name);
        }
        foreach (var row in rowsToInsert)
            data.Rows.Add(SeedProperties.Select(x => x.GetValue(row) ?? DBNull.Value).ToArray());
        await bulk.WriteToServerAsync(data, ct);
    }

    private static object? Normalize(object? value)
        => value is Enum enumValue ? Convert.ToInt32(enumValue) : value;
}
