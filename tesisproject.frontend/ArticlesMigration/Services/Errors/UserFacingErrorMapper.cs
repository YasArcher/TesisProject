using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace tesisproject.frontend.Services.Errors;

public static class UserFacingErrorMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<string> FromHttpResponseAsync(HttpResponseMessage response, string fallback, CancellationToken ct = default)
    {
        var raw = string.Empty;
        try
        {
            raw = await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            // Use fallback below.
        }

        return FromRawHttpError(response.StatusCode, raw, fallback);
    }

    public static string FromException(Exception exception, string fallback)
    {
        if (exception is null)
        {
            return fallback;
        }

        return ToUserMessage(exception.Message, fallback);
    }

    public static string FromRawHttpError(HttpStatusCode statusCode, string? rawBody, string fallback)
    {
        var extracted = ExtractMessage(rawBody);
        var statusFallback = statusCode switch
        {
            HttpStatusCode.BadRequest => "Revisa la información ingresada. Hay datos obligatorios o valores que necesitan corrección.",
            HttpStatusCode.Unauthorized => "Tu sesión expiró o no está activa. Inicia sesión nuevamente.",
            HttpStatusCode.Forbidden => "No tienes permisos para realizar esta acción. Solicita acceso al administrador del sistema.",
            HttpStatusCode.NotFound => "No encontré la información solicitada. Actualiza la pantalla e inténtalo nuevamente.",
            HttpStatusCode.Conflict => "La operación no se pudo completar porque ya existe un registro relacionado.",
            HttpStatusCode.RequestTimeout => "La operación tardó más de lo esperado. Inténtalo nuevamente en unos minutos.",
            HttpStatusCode.InternalServerError => "No pude completar la operación por un problema del servidor. Actualiza la pantalla e inténtalo nuevamente.",
            HttpStatusCode.ServiceUnavailable => "El servicio no está disponible en este momento. Inténtalo nuevamente en unos minutos.",
            _ => fallback
        };

        return ToUserMessage(extracted, string.IsNullOrWhiteSpace(statusFallback) ? fallback : statusFallback);
    }

    public static string ToUserMessage(string? rawMessage, string fallback)
    {
        var message = Clean(ExtractMessage(rawMessage));
        if (string.IsNullOrWhiteSpace(message))
        {
            return fallback;
        }

        if (ContainsAny(message, "AUTHOR_SUBMISSION_PROCESSING_FAILED"))
        {
            return "No pude registrar el envío porque una fila todavía tiene información incompleta o inconsistente. Abre las observaciones del staging, corrige el campo indicado y valida nuevamente.";
        }

        if (ContainsAny(message, "CLIENT_VENUE_RESOLUTION", "JournalName", "IssnCode"))
        {
            return "Para identificar la revista debes completar el nombre de la revista o el ISSN. Corrige ese dato en el staging y vuelve a validar.";
        }

        if (ContainsAny(message, "CLIENT_INVALID_VALUE", "REQUIRED_FIELD", "CLIENT_REQUIRED_FIELD"))
        {
            return "Hay campos obligatorios o valores con formato incorrecto. Revisa las observaciones marcadas en rojo y vuelve a validar.";
        }

        if (ContainsAny(message, "foreign key", "FK_", "INSERT statement conflicted"))
        {
            return "No pude completar la operación porque falta una referencia necesaria en catálogos o reportería. Actualiza la información y, si el problema continúa, solicita revisión técnica.";
        }

        if (ContainsAny(message, "Execution Timeout", "timeout", "HttpClient.Timeout", "request was canceled"))
        {
            return "La operación tardó más de lo esperado. Inténtalo nuevamente; si estás procesando muchos datos, espera unos minutos antes de repetir la acción.";
        }

        if (ContainsAny(message, "403", "Forbidden"))
        {
            return "No tienes permisos para realizar esta acción. Solicita acceso al administrador del sistema.";
        }

        if (ContainsAny(message, "401", "Unauthorized"))
        {
            return "Tu sesión expiró o no está activa. Inicia sesión nuevamente.";
        }

        if (LooksTechnical(message))
        {
            return fallback;
        }

        return message;
    }

    private static string? ExtractMessage(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;

            if (TryGetString(root, "message", out var message))
            {
                return message;
            }

            if (TryGetString(root, "detail", out var detail))
            {
                return detail;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in errors.EnumerateObject())
                {
                    if (item.Value.ValueKind == JsonValueKind.Array)
                    {
                        var first = item.Value.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind == JsonValueKind.String)
                        {
                            return first.GetString();
                        }
                    }
                }
            }

            if (TryGetString(root, "title", out var title))
            {
                return title;
            }
        }
        catch
        {
            // Plain text or composed exception message.
        }

        var bodyMatch = Regex.Match(trimmed, @"Body:\s*(\{.*\})", RegexOptions.Singleline);
        if (bodyMatch.Success)
        {
            return ExtractMessage(bodyMatch.Groups[1].Value) ?? trimmed;
        }

        return trimmed;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value.Trim();
        cleaned = Regex.Replace(cleaned, @"API Error\s+\d+\s*-\s*\w+\s*\|?\s*", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"API Error\s+\d+\s*-\s*\w+:\s*", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        return cleaned.Trim();
    }

    private static bool LooksTechnical(string value)
        => ContainsAny(value,
            "Microsoft.",
            "System.",
            "SqlException",
            "DbUpdateException",
            "StackTrace",
            "traceId",
            "RFC9110",
            "tools.ietf.org",
            "lambda_method",
            " at ",
            "Data Source=",
            "ConnectionId");

    private static bool ContainsAny(string value, params string[] tokens)
        => tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
}
