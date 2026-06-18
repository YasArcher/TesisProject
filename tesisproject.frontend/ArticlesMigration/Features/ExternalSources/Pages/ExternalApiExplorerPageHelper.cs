using System.Text.Json;
using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.frontend.Features.ExternalSources.Pages;

internal static class ExternalApiExplorerPageHelper
{
    public static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public static ExternalArticlePreviewDto[] BuildPreviewArticles(ExternalApiQueryResultDto? result)
    {
        if (result is null)
        {
            return [];
        }

        var directArticles = result.Articles
            .Where(HasMeaningfulArticleContent)
            .ToArray();

        if (directArticles.Length > 0)
        {
            return directArticles;
        }

        if (string.IsNullOrWhiteSpace(result.RawResponsePreview))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(result.RawResponsePreview);
            if (document.RootElement.TryGetProperty("search-results", out var searchResults)
                && searchResults.TryGetProperty("entry", out var entries)
                && entries.ValueKind == JsonValueKind.Array)
            {
                return entries.EnumerateArray()
                    .Select(entry => new ExternalArticlePreviewDto
                    {
                        Title = entry.TryGetProperty("dc:title", out var title) ? title.GetString() ?? "(sin título)" : "(sin título)",
                        Doi = entry.TryGetProperty("prism:doi", out var doi) ? doi.GetString() : null,
                        JournalName = entry.TryGetProperty("prism:publicationName", out var journal) ? journal.GetString() : null,
                        PublicationYear = TryExtractYear(entry.TryGetProperty("prism:coverDate", out var coverDate) ? coverDate.GetString() : null),
                        PublicationDate = entry.TryGetProperty("prism:coverDate", out var coverDateValue) ? coverDateValue.GetString() : null,
                        Authors = entry.TryGetProperty("dc:creator", out var creator) ? creator.GetString() : null
                    })
                    .Where(HasMeaningfulArticleContent)
                    .ToArray();
            }
        }
        catch
        {
        }

        return [];
    }

    private static bool HasMeaningfulArticleContent(ExternalArticlePreviewDto article)
    {
        return !string.IsNullOrWhiteSpace(article.Title)
               && !string.Equals(article.Title, "(sin título)", StringComparison.OrdinalIgnoreCase);
    }

    private static int? TryExtractYear(string? dateValue)
    {
        if (string.IsNullOrWhiteSpace(dateValue) || dateValue.Length <= 3)
        {
            return null;
        }

        var yearText = dateValue.Substring(0, 4);
        return int.TryParse(yearText, out var year) ? year : null;
    }
}
