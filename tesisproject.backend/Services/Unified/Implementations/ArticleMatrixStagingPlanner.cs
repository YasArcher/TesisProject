using System.Text.Json;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.backend.Services.Unified.Implementations;

// Article-owned conversion. It never writes to Product or Article.
public static class ArticleMatrixStagingPlanner
{
    public static List<ArticleStagingRow> BuildRows(RegistrationMatrix matrix,
        IReadOnlySet<string> existingDois, IReadOnlyList<RegistrationMatrixRowParticipantDto> participants)
    {
        var seenDois = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var suppliedParticipants = participants.ToDictionary(x => x.RegistrationMatrixRowId);
        var columns = matrix.Columns.OrderBy(x => x.DisplayOrder).ThenBy(x => x.FieldId).ToList();
        var doiField = columns.FirstOrDefault(x => x.Field?.EntityName == "Article" &&
            string.Equals(x.Field.FieldKey, "Doi", StringComparison.OrdinalIgnoreCase));
        var rows = new List<ArticleStagingRow>(matrix.Rows.Count);

        foreach (var row in matrix.Rows.OrderBy(x => x.RowNumber))
        {
            var cells = row.Cells.ToDictionary(x => x.FieldId);
            var missing = columns.Where(c => c.Field?.IsRequired == true &&
                (!cells.TryGetValue(c.FieldId, out var cell) || string.IsNullOrWhiteSpace(cell.RawValue)))
                .Select(c => c.Field?.FieldLabel ?? c.FieldId.ToString()).ToList();
            var doi = doiField is not null && cells.TryGetValue(doiField.FieldId, out var doiCell)
                ? NormalizeDoi(doiCell.RawValue) : null;
            var duplicate = doi is not null && (existingDois.Contains(doi) || !seenDois.Add(doi));
            if (doi is { Length: > 20 }) missing.Add("DOI excede 20 caracteres");

            suppliedParticipants.TryGetValue(row.RegistrationMatrixRowId, out var participantGroup);
            var snapshot = new
            {
                Fields = columns.Select(c => new
                {
                    c.FieldId,
                    FieldKey = c.Field?.FieldKey ?? string.Empty,
                    EntityName = c.Field?.EntityName ?? string.Empty,
                    DataType = c.Field?.DataType ?? string.Empty,
                    IsRequired = c.Field?.IsRequired ?? false,
                    RawValue = cells.TryGetValue(c.FieldId, out var cell) ? cell.RawValue : null
                }).ToList(),
                Participants = participantGroup?.Participants ?? []
            };
            rows.Add(new ArticleStagingRow
            {
                RegistrationMatrixRowId = row.RegistrationMatrixRowId,
                RowNumber = row.RowNumber,
                Status = missing.Count > 0 ? "Invalid" : duplicate ? "Duplicate" : "Ready",
                DoiNormalized = doi is { Length: <= 20 } ? doi : null,
                ValidationMessage = missing.Count > 0 ? $"Completa o corrige: {string.Join(", ", missing)}." :
                    duplicate ? "DOI ya registrado o repetido en este lote." : null,
                SnapshotJson = JsonSerializer.Serialize(snapshot)
            });
        }
        return rows;
    }

    public static string? NormalizeDoi(string? value)
    {
        var doi = value?.Trim();
        if (string.IsNullOrWhiteSpace(doi)) return null;
        foreach (var prefix in new[] { "https://doi.org/", "http://doi.org/", "doi:" })
            if (doi.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                doi = doi[prefix.Length..].Trim();
                break;
            }
        return doi.ToLowerInvariant();
    }
}
