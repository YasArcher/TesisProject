using System.Reflection;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Validation;

namespace tesisproject.backend.Services.Unified.Implementations;

internal static class UnifiedArticleRegistrationValidation
{
    internal sealed record ResolvedValue(int FieldId, DynamicFieldValueInputDto Value);

    internal static List<ResolvedValue> Validate(FormDefinition? form, string entityName,
        List<DynamicFieldValueInputDto>? inputs, params object[] physicalSources)
    {
        var fields = form?.Fields.Where(f => f.IsVisible && f.IsEditable && f.Field is
            { IsActive: true, IsVisible: true, IsEditable: true }
            && string.Equals(f.Field.EntityName.Trim(), entityName, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
        var resolved = new Dictionary<int, DynamicFieldValueInputDto>();
        foreach (var input in inputs ?? [])
        {
            var matches = fields.Where(f => (input.FieldId is null || input.FieldId == f.FieldId)
                && (string.IsNullOrWhiteSpace(input.FieldKey) || string.Equals(input.FieldKey.Trim(), f.Field!.FieldKey, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if ((input.FieldId is null && string.IsNullOrWhiteSpace(input.FieldKey)) || matches.Count != 1)
                throw new ArticleRegistrationException("Campo dinámico desconocido o ambiguo.");
            var field = matches[0].Field!;
            if (!field.IsDynamic || (entityName == "Article" && CanonicalProperty(field) is not null))
                throw new ArticleRegistrationException($"{field.FieldKey} debe enviarse en su propiedad canónica, no como campo dinámico.");
            if (!resolved.TryAdd(field.FieldId, input))
                throw new ArticleRegistrationException($"Campo dinámico repetido: {field.FieldKey}.");
            if (new object?[] { input.ValueString, input.ValueInt, input.ValueDecimal, input.ValueDate, input.ValueBit, input.ValueJson }
                .Count(v => v is not null) > 1)
                throw new ArticleRegistrationException($"{field.FieldKey} debe contener un único valor tipado.");
        }
        foreach (var definition in fields)
        {
            var field = definition.Field!;
            var canonical = entityName == "Article" ? CanonicalProperty(field) : null;
            var value = field.IsDynamic && canonical is null
                ? ToValidation(resolved.GetValueOrDefault(field.FieldId))
                : Physical(canonical ?? field.PhysicalColumnName ?? field.FieldKey, physicalSources);
            var error = DynamicFieldValidationEngine.Validate(field.FieldLabel, field.DataType,
                definition.IsRequired, field.MaxLength, field.ValidationRule, value);
            if (error is not null) throw new ArticleRegistrationException(error);
        }
        return resolved.Select(x => new ResolvedValue(x.Key, x.Value)).ToList();
    }

    // These known base fields must never be written to ArticleDynamicFieldValues.
    private static string? CanonicalProperty(FieldCatalogEntry field)
    {
        if (UnifiedArticleFormSelector.Normalize(field.PhysicalTableName) is "venues" or "dbovenues"
            && string.Equals(field.PhysicalColumnName, "Name", StringComparison.OrdinalIgnoreCase)) return "JournalName";
        foreach (var key in new[] { field.FieldKey, field.PhysicalColumnName, field.FieldLabel })
        {
            var property = UnifiedArticleFormSelector.Normalize(key) switch
            {
                "journal" or "journalname" or "nombrerevista" or "revista" => "JournalName",
                "indexingdatabase" or "basededatos" => "IndexingDatabase",
                "sjr" or "impacto" or "impactosjr" => "Sjr",
                "quartile" or "cuartil" => "Quartile",
                "issn" or "issnisbn" or "issncode" => "IssnCode",
                "doi" => "Doi", "year" or "año" => "Year",
                "publicationurl" or "consultationurl" or "url" => "PublicationUrl",
                "title" => "Title", _ => null
            };
            if (property is not null) return property;
        }
        return null;
    }

    private static DynamicFieldValidationValue Physical(string key, object[] sources)
    {
        // Explicit legacy compatibility: this flag is derived from the concrete project reference.
        if (key.Equals("IsProjectResult", StringComparison.OrdinalIgnoreCase))
            return new() { Bool = sources.OfType<ArticleAggregateCoreDto>().Single().ProjectId.HasValue };
        foreach (var source in sources)
        {
            var property = source.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null) continue;
            return property.GetValue(source) switch
            {
                string s => new() { Text = s }, int i => new() { Int = i }, short s => new() { Int = s },
                byte b => new() { Int = b }, decimal d => new() { Decimal = d },
                DateTime d => new() { Date = d }, bool b => new() { Bool = b }, _ => new()
            };
        }
        return new();
    }

    private static DynamicFieldValidationValue ToValidation(DynamicFieldValueInputDto? v) => v is null ? new() : new()
    { Text = v.ValueString, Int = v.ValueInt, Decimal = v.ValueDecimal, Date = v.ValueDate, Bool = v.ValueBit, Json = v.ValueJson };
}

internal sealed class ArticleRegistrationException(string message,
    tesisproject.shared.Responses.ErrorType error = tesisproject.shared.Responses.ErrorType.Validation,
    string? code = "ARTICLE_REGISTER_VALIDATION_FAILED") : Exception(message)
{
    internal tesisproject.shared.Responses.ErrorType Error { get; } = error;
    internal string? Code { get; } = code;
}
