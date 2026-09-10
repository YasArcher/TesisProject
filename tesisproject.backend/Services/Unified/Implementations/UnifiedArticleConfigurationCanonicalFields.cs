using tesisproject.shared.Enums;

namespace tesisproject.backend.Services.Unified.Implementations;

// Configuration descriptors only. Product/Article values are never written by configuration.
internal static class UnifiedArticleConfigurationCanonicalFields
{
    internal static BaseProductAttributeId? Attribute(string entity, params string?[] names)
    {
        if (!string.Equals(entity.Trim(), "Article", StringComparison.OrdinalIgnoreCase)) return null;
        foreach (var name in names)
        {
            var id = UnifiedArticleFormSelector.Normalize(name) switch
            {
                "title" or "titulo" or "título" => BaseProductAttributeId.Title,
                "authors" or "autores" => BaseProductAttributeId.Authors,
                "journal" or "journalname" or "nombrerevista" or "revista" => BaseProductAttributeId.Journal,
                "indexingdatabase" or "basededatos" => BaseProductAttributeId.IndexingDatabase,
                "sjr" or "impacto" or "impactosjr" => BaseProductAttributeId.Sjr,
                "quartile" or "cuartil" => BaseProductAttributeId.Quartile,
                "issn" or "issnisbn" or "issncode" => BaseProductAttributeId.IssnIsbn,
                "doi" => BaseProductAttributeId.Doi,
                "year" or "año" => BaseProductAttributeId.Year,
                "publicationurl" or "consultationurl" or "url" => BaseProductAttributeId.ConsultationUrl,
                _ => (BaseProductAttributeId?)null
            };
            if (id.HasValue) return id;
        }
        return null;
    }
    internal static string Column(BaseProductAttributeId attribute) => attribute switch
    {
        BaseProductAttributeId.IssnIsbn => "Issn", BaseProductAttributeId.ConsultationUrl => "PublicationUrl",
        _ => attribute.ToString()
    };
}
