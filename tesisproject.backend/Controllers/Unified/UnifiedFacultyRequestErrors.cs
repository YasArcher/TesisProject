using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified;

// HTTP keeps the historical field names; services receive explicit external IDs.
internal static class UnifiedFacultyRequestErrors
{
    internal static ServiceResult<T> ForHttpField<T>(this ServiceResult<T> result, string externalField, string httpField)
    {
        if (result.ValidationErrors is null || !result.ValidationErrors.Keys.Any(k => string.Equals(k, externalField, StringComparison.OrdinalIgnoreCase)))
            return result;
        var validation = new Dictionary<string, string[]>();
        foreach (var (key, messages) in result.ValidationErrors)
        {
            var target = string.Equals(key, externalField, StringComparison.OrdinalIgnoreCase) ? httpField : key;
            validation[target] = validation.TryGetValue(target, out var prior) ? prior.Concat(messages).ToArray() : messages;
        }
        return new ServiceResult<T>
        {
            Success = result.Success, Data = result.Data, Message = result.Message,
            Error = result.Error, ErrorCode = result.ErrorCode, ValidationErrors = validation
        };
    }
}
