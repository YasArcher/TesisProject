using System.Globalization;
using System.Text;
using System.Text.Json;

namespace tesisproject.backend.Services.Implementations
{
    internal static class ScopusExternalApiSupport
    {
        public static IReadOnlyList<string> BuildAuthorSearchQueries(string authorInput)
        {
            var tokens = Tokenize(authorInput);
            if (tokens.Count == 0)
            {
                return [];
            }

            var queries = new List<string>();

            void Add(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (!queries.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    queries.Add(value);
                }
            }

            if (tokens.Count == 1)
            {
                Add($"AUTHLASTNAME({Quote($"{tokens[0]}*")})");
                return queries;
            }

            AddAuthorInterpretationQueries(tokens, Add);

            return queries;
        }

        private static void AddAuthorInterpretationQueries(IReadOnlyList<string> tokens, Action<string?> add)
        {
            var forwardSurname = tokens[^1];
            var forwardGiven = tokens[0];
            AddAuthorQueryVariant(forwardSurname, forwardGiven, add);

            if (tokens.Count >= 3 && tokens[^1].Length == 1)
            {
                AddAuthorQueryVariant(tokens[^2], tokens[0], add);
            }

            var invertedSurname = tokens[0];
            var invertedGiven = tokens.Count >= 3 && tokens[^1].Length == 1
                ? tokens[1]
                : tokens[1];

            AddAuthorQueryVariant(invertedSurname, invertedGiven, add);

            if (tokens.Count >= 3)
            {
                var compoundSurname = string.Join(' ', tokens.Take(2));
                AddAuthorQueryVariant(compoundSurname, tokens[2].Length == 1 ? tokens[1] : tokens[2], add);

                var trailingSurname = string.Join(' ', tokens.Skip(tokens.Count - 2));
                AddAuthorQueryVariant(trailingSurname, tokens[0], add);
            }
        }

        private static void AddAuthorQueryVariant(string surname, string given, Action<string?> add)
        {
            if (string.IsNullOrWhiteSpace(surname) || string.IsNullOrWhiteSpace(given))
            {
                return;
            }

            var givenInitial = given[..1];
            add($"AUTHLASTNAME({Quote(surname)}) AND AUTHFIRST({Quote(given)})");
            add($"AUTHLASTNAME({Quote(surname)}) AND AUTHFIRST({Quote(givenInitial)})");
            add($"AUTHLASTNAME({Quote(surname)})");
        }

        public static IReadOnlyList<string> BuildAffiliationSearchQueries(string institutionName)
        {
            var variants = BuildInstitutionVariants(institutionName);
            var queries = new List<string>();

            foreach (var variant in variants)
            {
                var exact = NormalizeWhitespace(variant);
                if (string.IsNullOrWhiteSpace(exact))
                {
                    continue;
                }

                queries.Add($"AFFIL({Quote(exact)})");
            }

            return queries
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<ScopusEntityCandidate> ParseAuthorCandidates(string rawJson, string authorInput, string? institutionName = null)
        {
            using var document = JsonDocument.Parse(rawJson);
            var candidates = new List<ScopusEntityCandidate>();

            if (!TryGetEntryArray(document.RootElement, out var entries))
            {
                return candidates;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                var id = ExtractNumericIdentifier(
                    TryGetString(entry, "dc:identifier"),
                    TryGetString(entry, "eid"),
                    TryGetString(entry, "author-id"));

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var displayName = FirstNonEmpty(
                    TryGetNestedString(entry, "preferred-name", "indexed-name"),
                    TryGetNestedString(entry, "preferred-name", "ce:indexed-name"),
                    JoinName(
                        TryGetNestedString(entry, "preferred-name", "given-name"),
                        TryGetNestedString(entry, "preferred-name", "surname")),
                    TryGetString(entry, "dc:title"));

                var affiliation = FirstNonEmpty(
                    TryGetNestedString(entry, "affiliation-current", "affiliation-name"),
                    TryGetNestedString(entry, "affiliation-current", "preferred-name"),
                    TryGetString(entry, "affiliation-name"));

                var score = ScoreAuthorCandidate(authorInput, displayName, affiliation, institutionName);
                var documentCount = TryGetInt(TryGetString(entry, "document-count")) ?? 0;

                candidates.Add(new ScopusEntityCandidate(id, displayName ?? id, affiliation, score, documentCount));
            }

            return candidates
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.DocumentCount)
                .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
        }

        public static IReadOnlyList<ScopusEntityCandidate> ParseAffiliationCandidates(string rawJson, string institutionName)
        {
            using var document = JsonDocument.Parse(rawJson);
            var candidates = new List<ScopusEntityCandidate>();

            if (!TryGetEntryArray(document.RootElement, out var entries))
            {
                return candidates;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                var id = ExtractNumericIdentifier(
                    TryGetString(entry, "dc:identifier"),
                    TryGetString(entry, "eid"),
                    TryGetString(entry, "affiliation-id"));

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var displayName = FirstNonEmpty(
                    TryGetString(entry, "affiliation-name"),
                    TryGetNestedString(entry, "preferred-name", "$"),
                    TryGetString(entry, "dc:title"),
                    TryGetNestedString(entry, "name-variant", "$"));

                var location = JoinLocation(
                    TryGetString(entry, "city"),
                    TryGetString(entry, "country"));

                var score = ScoreInstitutionCandidate(institutionName, displayName, location);
                var documentCount = TryGetInt(TryGetString(entry, "document-count")) ?? 0;

                candidates.Add(new ScopusEntityCandidate(id, displayName ?? id, location, score, documentCount));
            }

            return candidates
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.DocumentCount)
                .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
        }

        public static string BuildAuthorDocumentQuery(IEnumerable<string> authorIds)
            => string.Join(" OR ", authorIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => $"AU-ID({id})"));

        public static IReadOnlyList<string> BuildAuthorArticleQueries(string authorInput, string? institutionName = null)
        {
            var tokens = Tokenize(authorInput);
            if (tokens.Count == 0)
            {
                return [];
            }

            var queries = new List<string>();
            var institutionFilter = BuildInstitutionFilterExpression(institutionName);

            void Add(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                var finalValue = string.IsNullOrWhiteSpace(institutionFilter)
                    ? value
                    : $"({value}) AND ({institutionFilter})";

                if (!queries.Contains(finalValue, StringComparer.OrdinalIgnoreCase))
                {
                    queries.Add(finalValue);
                }
            }

            if (tokens.Count == 1)
            {
                Add($"AUTHOR-NAME({Quote(tokens[0])})");
                Add($"AUTHLASTNAME({Quote($"{tokens[0]}*")})");
                return queries;
            }

            AddArticleAuthorInterpretation(tokens, Add);
            return queries;
        }

        public static string BuildAffiliationDocumentQuery(IEnumerable<string> affiliationIds)
            => string.Join(" OR ", affiliationIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => $"AF-ID({id})"));

        public static IReadOnlyList<string> BuildInstitutionVariants(string institutionName)
        {
            var fallback = new[]
            {
                "Universidad Técnica de Ambato",
                "Universidad Tecnica de Ambato",
                "Technical University of Ambato"
            };

            var variants = new List<string>();
            if (!string.IsNullOrWhiteSpace(institutionName))
            {
                variants.Add(institutionName);
                variants.Add(RemoveDiacritics(institutionName));
            }

            variants.AddRange(fallback);

            return variants
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeWhitespace)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? BuildInstitutionFilterExpression(string? institutionName)
        {
            if (string.IsNullOrWhiteSpace(institutionName))
            {
                return null;
            }

            var filters = BuildInstitutionVariants(institutionName)
                .Select(variant => NormalizeWhitespace(variant))
                .Where(variant => !string.IsNullOrWhiteSpace(variant))
                .Select(variant => $"AFFILORG({Quote(variant)})")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return filters.Count == 0
                ? null
                : string.Join(" OR ", filters);
        }

        public static IReadOnlyList<string> BuildAffiliationArticleQueries(string institutionName)
        {
            var variants = BuildInstitutionVariants(institutionName);
            var queries = new List<string>();

            foreach (var variant in variants)
            {
                var exact = NormalizeWhitespace(variant);
                if (string.IsNullOrWhiteSpace(exact))
                {
                    continue;
                }

                queries.Add($"AFFILORG({Quote(exact)}) AND AFFILCOUNTRY({Quote("Ecuador")})");
                queries.Add($"AFFILORG({Quote(exact)})");
            }

            return queries
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void AddArticleAuthorInterpretation(IReadOnlyList<string> tokens, Action<string?> add)
        {
            var forwardSurnameIndex = tokens.Count - 1;
            if (tokens[^1].Length == 1 && tokens.Count >= 3)
            {
                forwardSurnameIndex = tokens.Count - 2;
            }

            var forwardSurname = tokens[forwardSurnameIndex];
            var forwardGivenTokens = tokens
                .Where((_, index) => index != forwardSurnameIndex)
                .ToArray();
            AddArticleAuthorVariant(forwardSurname, string.Join(' ', forwardGivenTokens), add);

            var invertedSurname = tokens[0];
            var invertedGivenTokens = tokens.Skip(1).ToArray();
            AddArticleAuthorVariant(invertedSurname, string.Join(' ', invertedGivenTokens), add);
        }

        private static void AddArticleAuthorVariant(string surname, string given, Action<string?> add)
        {
            if (string.IsNullOrWhiteSpace(surname) || string.IsNullOrWhiteSpace(given))
            {
                return;
            }

            var normalizedGiven = NormalizeWhitespace(given);
            var compact = NormalizeWhitespace($"{normalizedGiven} {surname}");
            add($"AUTHOR-NAME({Quote($"{surname}, {normalizedGiven}")})");
            add($"AUTHOR-NAME({Quote(compact)})");

            var parts = normalizedGiven.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 1)
            {
                add($"AUTHOR-NAME({Quote($"{surname}, {parts[0]}")})");
                add($"AUTHOR-NAME({Quote($"{parts[0]} {surname}")})");
            }
        }

        public static IReadOnlyList<string> SelectBestAuthorIds(
            IReadOnlyList<ScopusEntityCandidate> candidates,
            string authorInput,
            string? institutionName = null)
        {
            if (candidates.Count == 0)
            {
                return [];
            }

            var ordered = candidates
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.DocumentCount)
                .ToList();

            var best = ordered[0];
            if (best.Score < 30)
            {
                return [];
            }

            var selected = ordered
                .Where(candidate =>
                    candidate.Score >= Math.Max(30, best.Score - 10)
                    && candidate.DocumentCount >= 0)
                .Take(2)
                .Select(x => x.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (selected.Count == 0)
            {
                selected.Add(best.Id);
            }

            return selected;
        }

        public static IReadOnlyList<string> SelectBestAffiliationIds(IReadOnlyList<ScopusEntityCandidate> candidates)
        {
            if (candidates.Count == 0)
            {
                return [];
            }

            var ordered = candidates
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.DocumentCount)
                .ToList();

            var best = ordered[0];
            if (best.Score < 45)
            {
                return [];
            }

            return ordered
                .Where(candidate => candidate.Score >= Math.Max(45, best.Score - 8))
                .Take(2)
                .Select(x => x.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryGetEntryArray(JsonElement root, out JsonElement entries)
        {
            entries = default;
            return root.TryGetProperty("search-results", out var searchResults)
                && searchResults.TryGetProperty("entry", out entries)
                && entries.ValueKind == JsonValueKind.Array;
        }

        private static string NormalizeWhitespace(string value)
            => string.Join(' ', value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        private static IReadOnlyList<string> Tokenize(string value)
            => NormalizeWhitespace(RemoveDiacritics(value))
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        private static string Quote(string value)
            => $"\"{value.Replace("\"", string.Empty)}\"";

        private static string RemoveDiacritics(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static int ScoreAuthorCandidate(string authorInput, string? displayName, string? affiliation, string? institutionName)
        {
            var score = 0;
            var inputTokens = Tokenize(authorInput);
            var normalizedDisplayName = RemoveDiacritics(displayName ?? string.Empty).ToUpperInvariant();
            var normalizedName = RemoveDiacritics($"{displayName} {affiliation}".Trim()).ToUpperInvariant();

            foreach (var token in inputTokens)
            {
                if (normalizedName.Contains(token.ToUpperInvariant(), StringComparison.Ordinal))
                {
                    score += 20;
                }
            }

            if (inputTokens.Count > 0 && normalizedDisplayName.Contains(inputTokens[0].ToUpperInvariant(), StringComparison.Ordinal))
            {
                score += 15;
            }

            if (inputTokens.Count > 1 && normalizedDisplayName.Contains(inputTokens[^1].ToUpperInvariant(), StringComparison.Ordinal))
            {
                score += 20;
            }

            if (inputTokens.Count > 1 && inputTokens.All(token => normalizedDisplayName.Contains(token.ToUpperInvariant(), StringComparison.Ordinal)))
            {
                score += 30;
            }

            if (string.Equals(
                NormalizeWhitespace(RemoveDiacritics(displayName ?? string.Empty)),
                NormalizeWhitespace(RemoveDiacritics(authorInput)),
                StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            if (inputTokens.Count > 1)
            {
                var first = inputTokens[0];
                var last = inputTokens[^1];
                if (normalizedName.Contains(first.ToUpperInvariant(), StringComparison.Ordinal) &&
                    normalizedName.Contains(last.ToUpperInvariant(), StringComparison.Ordinal))
                {
                    score += 25;
                }
            }

            if (!string.IsNullOrWhiteSpace(institutionName))
            {
                var institutionVariants = BuildInstitutionVariants(institutionName);
                var normalizedAffiliation = RemoveDiacritics(affiliation ?? string.Empty).ToUpperInvariant();
                if (institutionVariants.Any(variant =>
                    normalizedAffiliation.Contains(RemoveDiacritics(variant).ToUpperInvariant(), StringComparison.Ordinal)))
                {
                    score += 40;
                }
            }

            return score;
        }

        private static int ScoreInstitutionCandidate(string institutionName, string? displayName, string? location)
        {
            var score = 0;
            var inputTokens = Tokenize(institutionName);
            var normalizedTarget = RemoveDiacritics($"{displayName} {location}").ToUpperInvariant();

            foreach (var token in inputTokens)
            {
                if (normalizedTarget.Contains(token.ToUpperInvariant(), StringComparison.Ordinal))
                {
                    score += 20;
                }
            }

            if (normalizedTarget.Contains("AMBATO", StringComparison.Ordinal))
            {
                score += 25;
            }

            if (normalizedTarget.Contains("ECUADOR", StringComparison.Ordinal))
            {
                score += 15;
            }

            if (string.Equals(
                NormalizeWhitespace(RemoveDiacritics(displayName ?? string.Empty)),
                NormalizeWhitespace(RemoveDiacritics(institutionName)),
                StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            return score;
        }

        private static string? ExtractNumericIdentifier(params string?[] candidates)
        {
            foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var digits = new string(candidate!.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrWhiteSpace(digits))
                {
                    return digits;
                }
            }

            return null;
        }

        private static string? TryGetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property)
                ? property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : property.ToString()
                : null;
        }

        private static string? TryGetNestedString(JsonElement element, params string[] path)
        {
            var current = element;
            foreach (var segment in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                {
                    return null;
                }
            }

            return current.ValueKind == JsonValueKind.String
                ? current.GetString()
                : current.ToString();
        }

        private static int? TryGetInt(string? value)
            => int.TryParse(value, out var parsed) ? parsed : null;

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        private static string? JoinName(string? given, string? surname)
        {
            var parts = new[] { given, surname }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            return parts.Length == 0 ? null : string.Join(' ', parts);
        }

        private static string? JoinLocation(string? city, string? country)
        {
            var parts = new[] { city, country }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            return parts.Length == 0 ? null : string.Join(", ", parts);
        }
    }

    internal sealed record ScopusEntityCandidate(
        string Id,
        string DisplayName,
        string? SecondaryText,
        int Score,
        int DocumentCount)
    {
        public tesisproject.shared.DTOs.ExternalApis.ExternalApiResolutionCandidateDto ToDto()
            => new()
            {
                Id = Id,
                DisplayName = DisplayName,
                SecondaryText = SecondaryText,
                Score = Score,
                DocumentCount = DocumentCount
            };
    }
}
