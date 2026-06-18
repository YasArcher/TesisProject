// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace tesisproject.shared.Validation;

public sealed class DynamicFieldValidationRuleSet
{
    public List<DynamicFieldValidationRule> Rules { get; set; } = [];
}

public sealed class DynamicFieldValidationRule
{
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Message { get; set; }
}

public sealed class DynamicFieldValidationValue
{
    public string? Text { get; set; }
    public int? Int { get; set; }
    public decimal? Decimal { get; set; }
    public DateTime? Date { get; set; }
    public bool? Bool { get; set; }
    public string? Json { get; set; }

    public string AsText()
    {
        if (!string.IsNullOrWhiteSpace(Text))
        {
            return Text.Trim();
        }

        if (Int.HasValue)
        {
            return Int.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (Decimal.HasValue)
        {
            return Decimal.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (Date.HasValue)
        {
            return Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (Bool.HasValue)
        {
            return Bool.Value ? "true" : "false";
        }

        return Json?.Trim() ?? string.Empty;
    }

    public bool HasValue()
        => !string.IsNullOrWhiteSpace(Text)
            || Int.HasValue
            || Decimal.HasValue
            || Date.HasValue
            || Bool.HasValue
            || !string.IsNullOrWhiteSpace(Json);
}

public static class DynamicFieldValidationEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(DynamicFieldValidationRuleSet ruleSet)
        => JsonSerializer.Serialize(ruleSet, JsonOptions);

    public static DynamicFieldValidationRuleSet Parse(string? validationRule)
    {
        if (string.IsNullOrWhiteSpace(validationRule))
        {
            return new DynamicFieldValidationRuleSet();
        }

        try
        {
            return JsonSerializer.Deserialize<DynamicFieldValidationRuleSet>(validationRule, JsonOptions)
                ?? new DynamicFieldValidationRuleSet();
        }
        catch
        {
            return new DynamicFieldValidationRuleSet
            {
                Rules =
                [
                    new DynamicFieldValidationRule
                    {
                        Type = "regex",
                        Value = validationRule,
                        Message = "El valor no cumple el formato requerido."
                    }
                ]
            };
        }
    }

    public static string? Validate(
        string fieldLabel,
        string? dataType,
        bool isRequired,
        int? maxLength,
        string? validationRule,
        DynamicFieldValidationValue value)
    {
        var label = string.IsNullOrWhiteSpace(fieldLabel) ? "Campo" : fieldLabel.Trim();
        var text = value.AsText();

        if (isRequired && !value.HasValue())
        {
            return $"{label} es obligatorio.";
        }

        if (!value.HasValue())
        {
            return null;
        }

        if (maxLength.HasValue && text.Length > maxLength.Value)
        {
            return $"{label} no debe superar {maxLength.Value} caracteres.";
        }

        if (IsNumericDataType(dataType) && !IsNumericValue(value))
        {
            return $"{label} debe contener un número válido.";
        }

        if (IsDateDataType(dataType) && !value.Date.HasValue && !DateTime.TryParse(text, out _))
        {
            return $"{label} debe contener una fecha válida.";
        }

        if (IsDateDataType(dataType) && ResolveDate(value, text, out var resolvedDate) && resolvedDate.Date > DateTime.UtcNow.Date)
        {
            return $"{label} no puede ser una fecha futura.";
        }

        foreach (var rule in Parse(validationRule).Rules)
        {
            var message = ValidateRule(label, rule, value, text);
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }
        }

        return null;
    }

    private static string? ValidateRule(
        string label,
        DynamicFieldValidationRule rule,
        DynamicFieldValidationValue value,
        string text)
    {
        var type = (rule.Type ?? string.Empty).Trim().ToLowerInvariant();
        var configuredMessage = string.IsNullOrWhiteSpace(rule.Message) ? null : rule.Message.Trim();

        return type switch
        {
            "numeric" when !Regex.IsMatch(text, @"^\d+$") => configuredMessage ?? $"{label} solo debe contener números.",
            "numericexactlength" when TryInt(rule.Value, out var numericExact) && (!Regex.IsMatch(text, @"^\d+$") || text.Length != numericExact) => configuredMessage ?? $"{label} debe contener exactamente {numericExact} números.",
            "letters" when !Regex.IsMatch(text, @"^[\p{L}\s]+$") => configuredMessage ?? $"{label} solo debe contener letras.",
            "alphanumeric" when !Regex.IsMatch(text, @"^[\p{L}\p{N}\s]+$") => configuredMessage ?? $"{label} solo debe contener letras y números.",
            "exactlength" when TryInt(rule.Value, out var exact) && text.Length != exact => configuredMessage ?? $"{label} debe tener exactamente {exact} caracteres.",
            "minlength" when TryInt(rule.Value, out var minLength) && text.Length < minLength => configuredMessage ?? $"{label} debe tener al menos {minLength} caracteres.",
            "maxlength" when TryInt(rule.Value, out var maxLength) && text.Length > maxLength => configuredMessage ?? $"{label} no debe superar {maxLength} caracteres.",
            "email" when !Regex.IsMatch(text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") => configuredMessage ?? $"{label} debe contener un correo válido.",
            "url" when !Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") => configuredMessage ?? $"{label} debe contener una URL válida.",
            "regex" when !string.IsNullOrWhiteSpace(rule.Value) && !Regex.IsMatch(text, rule.Value) => configuredMessage ?? $"{label} no cumple el formato requerido.",
            "nofuturedate" when ResolveDate(value, text, out var date) && date.Date > DateTime.UtcNow.Date => configuredMessage ?? $"{label} no puede ser una fecha futura.",
            "minnumber" when TryDecimal(rule.Value, out var minNumber) && ResolveDecimal(value, text, out var number) && number < minNumber => configuredMessage ?? $"{label} debe ser mayor o igual a {minNumber}.",
            "maxnumber" when TryDecimal(rule.Value, out var maxNumber) && ResolveDecimal(value, text, out var number) && number > maxNumber => configuredMessage ?? $"{label} debe ser menor o igual a {maxNumber}.",
            _ => null
        };
    }

    private static bool IsNumericDataType(string? dataType)
        => string.Equals(dataType, "int", StringComparison.OrdinalIgnoreCase)
           || string.Equals(dataType, "decimal", StringComparison.OrdinalIgnoreCase);

    private static bool IsDateDataType(string? dataType)
        => string.Equals(dataType, "date", StringComparison.OrdinalIgnoreCase)
           || string.Equals(dataType, "datetime", StringComparison.OrdinalIgnoreCase);

    private static bool IsNumericValue(DynamicFieldValidationValue value)
        => value.Int.HasValue
           || value.Decimal.HasValue
           || decimal.TryParse(value.AsText(), NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private static bool ResolveDecimal(DynamicFieldValidationValue value, string text, out decimal number)
    {
        if (value.Decimal.HasValue)
        {
            number = value.Decimal.Value;
            return true;
        }

        if (value.Int.HasValue)
        {
            number = value.Int.Value;
            return true;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
    }

    private static bool ResolveDate(DynamicFieldValidationValue value, string text, out DateTime date)
    {
        if (value.Date.HasValue)
        {
            date = value.Date.Value;
            return true;
        }

        return DateTime.TryParse(text, out date);
    }

    private static bool TryInt(string? value, out int result)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryDecimal(string? value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
}

