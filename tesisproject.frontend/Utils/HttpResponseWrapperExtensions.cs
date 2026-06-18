using tesisproject.frontend.Services;

namespace tesisproject.frontend.Utils
{
    public static class HttpResponseWrapperExtensions
    {
        public static string ToSuccessMessage<T>(
            this HttpResponseWrapper<T>? result,
            string fallback)
        {
            if (result is null)
                return fallback;

            return string.IsNullOrWhiteSpace(result.Message)
                ? fallback
                : result.Message.Trim();
        }

        public static string ToErrorMessage<T>(
            this HttpResponseWrapper<T>? result,
            string fallback)
        {
            if (result is null)
                return fallback;

            var message = string.IsNullOrWhiteSpace(result.Message)
                ? fallback
                : result.Message.Trim();

            var validationSummary = ToValidationSummary(result.ValidationErrors);
            if (!string.IsNullOrWhiteSpace(validationSummary))
            {
                return $"{message} {validationSummary}";
            }

            if (string.IsNullOrWhiteSpace(result.Message) && !string.IsNullOrWhiteSpace(result.ErrorCode))
            {
                return $"{fallback} (Código: {result.ErrorCode})";
            }

            return message;
        }

        public static string ToValidationSummary(
            Dictionary<string, string[]>? validationErrors)
        {
            if (validationErrors is null || validationErrors.Count == 0)
                return string.Empty;

            var entries = validationErrors
                .Where(kvp => kvp.Value is { Length: > 0 })
                .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")
                .ToList();

            if (entries.Count == 0)
                return string.Empty;

            return $"Validaciones: {string.Join(" | ", entries)}";
        }
    }
}