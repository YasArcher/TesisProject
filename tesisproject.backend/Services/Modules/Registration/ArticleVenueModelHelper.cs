using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Implementations
{
    internal static class ArticleVenueModelHelper
    {
        private const string ArticleEntityName = "Article";
        private const string VenueFieldKey = "VenueId";

        private static readonly VenueFieldSeed[] VenueFieldSeeds =
        {
            new("JournalName", "Nombre de revista", "string", "CompositeData", "dbo.Venues", "Name", null, true, true, true, 160, 250, "Nombre de la revista o venue."),
            new("IssnCode", "ISSN", "string", "CompositeData", "dbo.Venues", "IssnCode", null, false, true, true, 161, 50, "ISSN o identificador editorial."),
            new("IssueNumber", "Número", "string", "CompositeData", "dbo.Venues", "IssueNumber", null, false, true, true, 162, 50, "Número o issue de la revista."),
            new("VolumeNumber", "Volumen", "string", "CompositeData", "dbo.Venues", "VolumeNumber", null, false, true, true, 163, 50, "Volumen de la revista."),
            new("JournalUrl", "URL de revista", "string", "CompositeData", "dbo.Venues", "JournalUrl", null, false, true, true, 164, 500, "URL de la revista o del recurso editorial."),
            new("Sjr", "SJR", "decimal", "CompositeData", "dbo.VenueMetrics", "SJR", null, false, true, true, 165, null, "Métrica SJR asociada al venue."),
            new("Quartile", "Cuartil", "string", "CompositeData", "dbo.VenueMetrics", "Quartile", null, false, true, true, 166, 20, "Cuartil de la revista para el año del artículo.")
        };

        public static IReadOnlyCollection<string> CompositeFieldKeys => VenueFieldSeeds.Select(x => x.FieldKey).ToList();

        public static bool IsCompositeVenueField(string? fieldKey)
            => VenueFieldSeeds.Any(x => string.Equals(x.FieldKey, fieldKey, StringComparison.OrdinalIgnoreCase));

        public static async Task EnsureVenueCompositeFieldsAsync(AppDbContext db, CancellationToken ct)
        {
            var existingFields = await db.FieldCatalogEntries
                .Where(x => x.EntityName == ArticleEntityName && (x.FieldKey == VenueFieldKey || CompositeFieldKeys.Contains(x.FieldKey)))
                .ToListAsync(ct);

            var existingByKey = existingFields
                .GroupBy(x => x.FieldKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(x => x.IsActive)
                        .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                        .ThenByDescending(x => x.FieldId)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);
            var created = false;

            foreach (var seed in VenueFieldSeeds)
            {
                if (existingByKey.ContainsKey(seed.FieldKey))
                {
                    continue;
                }

                db.FieldCatalogEntries.Add(new FieldCatalogEntry
                {
                    EntityName = ArticleEntityName,
                    FieldKey = seed.FieldKey,
                    FieldLabel = seed.FieldLabel,
                    DataType = seed.DataType,
                    SourceType = seed.SourceType,
                    PhysicalTableName = seed.PhysicalTableName,
                    PhysicalColumnName = seed.PhysicalColumnName,
                    ReferenceTableName = seed.ReferenceTableName,
                    IsSystemField = true,
                    IsDynamic = false,
                    IsRequired = seed.IsRequired,
                    IsVisible = seed.IsVisible,
                    IsEditable = seed.IsEditable,
                    IsFilterable = false,
                    IsActive = true,
                    DisplayOrder = seed.DisplayOrder,
                    MaxLength = seed.MaxLength,
                    HelpText = seed.HelpText,
                    CreatedAt = DateTime.UtcNow
                });

                created = true;
            }

            if (created)
            {
                await db.SaveChangesAsync(ct);
                existingFields = await db.FieldCatalogEntries
                    .Where(x => x.EntityName == ArticleEntityName && (x.FieldKey == VenueFieldKey || CompositeFieldKeys.Contains(x.FieldKey)))
                    .ToListAsync(ct);
                existingByKey = existingFields
                    .GroupBy(x => x.FieldKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderByDescending(x => x.IsActive)
                            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                            .ThenByDescending(x => x.FieldId)
                            .First(),
                        StringComparer.OrdinalIgnoreCase);
            }

            var form = await db.FormDefinitions
                .Include(x => x.Fields)
                .Where(x => x.EntityName == ArticleEntityName && x.IsActive)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.FormId)
                .FirstOrDefaultAsync(ct);

            if (form is null)
            {
                return;
            }

            var existingAssignments = form.Fields
                .GroupBy(x => x.FieldId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                        .ThenByDescending(x => x.FormFieldId)
                        .First());
            var anchor = form.Fields
                .OrderBy(x => x.DisplayOrder)
                .FirstOrDefault(x => existingByKey.TryGetValue(VenueFieldKey, out var venueField) && x.FieldId == venueField.FieldId);
            var hasCompositeAssignments = form.Fields.Any(x =>
                x.FieldId != 0
                && existingFields.Any(field =>
                    field.FieldId == x.FieldId
                    && IsCompositeVenueField(field.FieldKey)));

            var groupName = anchor?.GroupName ?? "Publicación / Venue";
            var baseDisplayOrder = anchor?.DisplayOrder ?? 160;
            var formChanged = false;
            var shouldSeedFormAssignments = anchor is not null && (anchor.IsVisible || anchor.IsEditable || anchor.IsRequired) && !hasCompositeAssignments;

            if (anchor is not null && (anchor.IsVisible || anchor.IsEditable || anchor.IsRequired))
            {
                anchor.IsVisible = false;
                anchor.IsEditable = false;
                anchor.IsRequired = false;
                anchor.UpdatedAt = DateTime.UtcNow;
                formChanged = true;
            }

            if (shouldSeedFormAssignments)
            {
                for (var i = 0; i < VenueFieldSeeds.Length; i++)
                {
                    var field = existingByKey[VenueFieldSeeds[i].FieldKey];
                    if (existingAssignments.ContainsKey(field.FieldId))
                    {
                        continue;
                    }

                    db.FormFieldDefinitions.Add(new FormFieldDefinition
                    {
                        FormId = form.FormId,
                        FieldId = field.FieldId,
                        IsVisible = true,
                        IsRequired = field.IsRequired,
                        IsEditable = true,
                        DisplayOrder = baseDisplayOrder + i + 1,
                        GroupName = groupName,
                        ColumnSpan = field.FieldKey is "JournalName" or "JournalUrl" ? 2 : 1,
                        CreatedAt = DateTime.UtcNow
                    });

                    formChanged = true;
                }
            }

            if (formChanged)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        public static bool HasVenueContent(ArticleVenueInputDto? venue)
        {
            if (venue is null)
            {
                return false;
            }

            return venue.VenueId.HasValue
                || !string.IsNullOrWhiteSpace(venue.JournalName)
                || !string.IsNullOrWhiteSpace(venue.IssnCode)
                || !string.IsNullOrWhiteSpace(venue.IssueNumber)
                || !string.IsNullOrWhiteSpace(venue.VolumeNumber)
                || !string.IsNullOrWhiteSpace(venue.JournalUrl)
                || !string.IsNullOrWhiteSpace(venue.Type);
        }

        public static bool HasMetricContent(ArticleVenueMetricInputDto? metric)
        {
            if (metric is null)
            {
                return false;
            }

            return metric.Year.HasValue
                || metric.Sjr.HasValue
                || !string.IsNullOrWhiteSpace(metric.Quartile);
        }

        public static async Task<int?> ResolveVenueIdAsync(
            AppDbContext db,
            ArticleVenueInputDto? venueInput,
            ArticleVenueMetricInputDto? metricInput,
            short? articleYear,
            CancellationToken ct)
        {
            if (!HasVenueContent(venueInput) && !HasMetricContent(metricInput))
            {
                return venueInput?.VenueId;
            }

            Venue? venue = null;

            if (venueInput?.VenueId is int explicitVenueId)
            {
                venue = await db.Venues.FirstOrDefaultAsync(x => x.VenueId == explicitVenueId, ct);
                if (venue is null)
                {
                    throw new InvalidOperationException("El VenueId informado no existe.");
                }
            }

            var issn = Normalize(venueInput?.IssnCode);
            var journalName = Normalize(venueInput?.JournalName);

            if (venue is null && !string.IsNullOrWhiteSpace(issn))
            {
                venue = await db.Venues.FirstOrDefaultAsync(x => x.IssnCode == issn, ct);
            }

            if (venue is null && !string.IsNullOrWhiteSpace(journalName))
            {
                venue = await db.Venues.FirstOrDefaultAsync(x => x.Name == journalName, ct);
            }

            if (venue is null)
            {
                if (string.IsNullOrWhiteSpace(journalName) && string.IsNullOrWhiteSpace(issn))
                {
                    throw new InvalidOperationException("Para registrar la revista debes informar al menos JournalName o IssnCode.");
                }

                venue = new Venue
                {
                    Name = string.IsNullOrWhiteSpace(journalName) ? "(Sin nombre)" : journalName!,
                    IssnCode = issn,
                    IssueNumber = Normalize(venueInput?.IssueNumber),
                    VolumeNumber = Normalize(venueInput?.VolumeNumber),
                    JournalUrl = Normalize(venueInput?.JournalUrl),
                    Type = string.IsNullOrWhiteSpace(venueInput?.Type) ? "Journal" : venueInput!.Type!.Trim()
                };

                db.Venues.Add(venue);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(journalName))
                {
                    venue.Name = journalName!;
                }

                venue.IssnCode = issn ?? venue.IssnCode;
                venue.IssueNumber = Normalize(venueInput?.IssueNumber) ?? venue.IssueNumber;
                venue.VolumeNumber = Normalize(venueInput?.VolumeNumber) ?? venue.VolumeNumber;
                venue.JournalUrl = Normalize(venueInput?.JournalUrl) ?? venue.JournalUrl;
                venue.Type = string.IsNullOrWhiteSpace(venueInput?.Type) ? venue.Type : venueInput!.Type!.Trim();
                await db.SaveChangesAsync(ct);
            }

            var metricYear = metricInput?.Year ?? articleYear;
            if (metricYear.HasValue && (metricInput?.Sjr.HasValue == true || !string.IsNullOrWhiteSpace(metricInput?.Quartile)))
            {
                var metric = await db.VenueMetrics.FirstOrDefaultAsync(x => x.VenueId == venue.VenueId && x.Year == metricYear.Value, ct);
                if (metric is null)
                {
                    metric = new VenueMetric
                    {
                        VenueId = venue.VenueId,
                        Year = metricYear.Value,
                        SJR = metricInput?.Sjr,
                        Quartile = Normalize(metricInput?.Quartile)
                    };
                    db.VenueMetrics.Add(metric);
                }
                else
                {
                    metric.SJR = metricInput?.Sjr ?? metric.SJR;
                    metric.Quartile = Normalize(metricInput?.Quartile) ?? metric.Quartile;
                }

                await db.SaveChangesAsync(ct);
            }

            return venue.VenueId;
        }

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private sealed record VenueFieldSeed(
            string FieldKey,
            string FieldLabel,
            string DataType,
            string SourceType,
            string PhysicalTableName,
            string PhysicalColumnName,
            string? ReferenceTableName,
            bool IsRequired,
            bool IsVisible,
            bool IsEditable,
            int DisplayOrder,
            int? MaxLength,
            string HelpText);
    }
}
