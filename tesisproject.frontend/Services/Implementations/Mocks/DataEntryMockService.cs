using System.Text.Json;
using tesisproject.frontend.Models;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations.Mocks;

public sealed class DataEntryMockService : IDataEntryService
{
    public async Task<(IReadOnlyList<ArticleDto> Preview, IReadOnlyList<string> Errors)> ValidateAndPreviewAsync(
        Stream fileStream,
        CancellationToken ct = default)
    {
        try
        {
            var docs = await JsonSerializer.DeserializeAsync<List<ArticleDto>>(fileStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct) ?? new();

            var errors = new List<string>();
            foreach (var (doc, idx) in docs.Select((d, i) => (d, i + 1)))
            {
                if (string.IsNullOrWhiteSpace(doc.Title)) errors.Add($"Row {idx}: Title is required");
                if (doc.Year is < 1900 or > 2100) errors.Add($"Row {idx}: Year is out of range");
            }

            return (docs.Take(20).ToList(), errors);
        }
        catch (Exception ex)
        {
            return (Array.Empty<ArticleDto>(), new[] { $"Invalid JSON: {ex.Message}" });
        }
    }

    public Task<ImportResult> ImportAsync(IEnumerable<ArticleDto> items, CancellationToken ct = default)
    {
        var list = items.ToList();
        return Task.FromResult(new ImportResult(Inserted: list.Count, Skipped: 0, Errors: Array.Empty<string>()));
    }
}