using System.Text;
using System.Text.RegularExpressions;
using tesisproject.shared.DTOs.Algorithms.Response;

namespace tesisproject.backend.Services.Parsers
{
    public static class ResolutionParser
    {
        // ============================================================
        //                      HELPERS GENERALES
        // ============================================================
        public static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 1. Unificar saltos de línea
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            // 2. Reemplazar tabs por espacio simple
            text = text.Replace("\t", " ");

            // 3. Quitar espacios no separables
            text = text.Replace("\u00A0", " ");

            // 4. ✅ CRÍTICO: Unificar múltiples espacios en uno solo
            text = Regex.Replace(text, @" {2,}", " ");

            // 5. ✅ NUEVO: Reducir múltiples saltos de línea a máximo 2
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // 6. ✅ UNIFICACIÓN DE PÁRRAFOS
            var lines = text.Split('\n')
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrEmpty(l))
                           .ToList();

            var unifiedLines = new List<string>();
            var currentParagraph = new StringBuilder();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // Detectar si es una línea especial (título, etiqueta, enumeración)
                bool isSpecialLine = IsSpecialLine(line);

                if (isSpecialLine)
                {
                    // Guardar el párrafo anterior si existe
                    if (currentParagraph.Length > 0)
                    {
                        unifiedLines.Add(currentParagraph.ToString().Trim());
                        currentParagraph.Clear();
                    }

                    // Agregar la línea especial como párrafo independiente
                    unifiedLines.Add(line);
                    continue;
                }

                // Agregar la línea al párrafo actual
                if (currentParagraph.Length > 0)
                    currentParagraph.Append(" ");

                currentParagraph.Append(line);

                // Verificar si es fin de párrafo
                if (IsEndOfParagraph(line))
                {
                    unifiedLines.Add(currentParagraph.ToString().Trim());
                    currentParagraph.Clear();
                }
            }

            // Agregar el último párrafo si existe
            if (currentParagraph.Length > 0)
            {
                unifiedLines.Add(currentParagraph.ToString().Trim());
            }

            return string.Join("\n", unifiedLines);
        }

        // Detecta si una línea es el fin de un párrafo
        private static bool IsEndOfParagraph(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();

            // Termina con punto
            if (trimmed.EndsWith("."))
                return true;

            // Termina con dos puntos (ejemplo: "RESUELVE:")
            if (trimmed.EndsWith(":"))
                return true;

            // Termina con punto y coma
            if (trimmed.EndsWith(";"))
                return true;

            return false;
        }

        // Detecta si una línea es especial (título, etiqueta, enumeración)
        private static bool IsSpecialLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();

            // Líneas que empiezan con número seguido de punto (1., 2., 3.)
            if (Regex.IsMatch(trimmed, @"^\d+\.\s+[A-ZÁÉÍÓÚ]"))
                return true;

            // Títulos en mayúsculas (RESUELVE, APROBAR, AUTORIZAR, etc.)
            if (trimmed.Length < 50 && trimmed == trimmed.ToUpper() &&
                !trimmed.Contains("USD") && !trimmed.Contains("$"))
                return true;

            // Líneas con formato "Etiqueta: Valor"
            if (Regex.IsMatch(trimmed, @"^(Coordinador|Investigador|Tipo de|Duración|Financiamiento|Referencias|Anexos|Copia)"))
                return true;

            // Fechas en formato especial
            if (Regex.IsMatch(trimmed, @"^Ambato,\s+\d"))
                return true;

            // Resoluciones
            if (trimmed.Contains("Resolución Nro."))
                return true;

            return false;
        }


        private static string? FindLineContaining(string text, string keyword)
        {
            var lines = text.Split('\n');

            return lines
                .FirstOrDefault(l =>
                    l.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static string? ExtractAfterColon(string? line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            var idx = line.IndexOf(':');
            if (idx < 0 || idx == line.Length - 1) return null;

            var afterColon = line[(idx + 1)..].Trim();

            if (afterColon.Length > 100)
            {
                var newLineIdx = afterColon.IndexOf('\n');
                if (newLineIdx > 0)
                    afterColon = afterColon[..newLineIdx].Trim();
            }

            return afterColon;
        }

        private static readonly Regex DateRegex = new(
            @"([0-9]{1,2}\s+de\s+[A-Za-zÁÉÍÓÚáéíóúñÑ]+(?:\s+de\s+[0-9]{4})?)",
            RegexOptions.Compiled);

        private static string? ExtractFirstDate(string text)
        {
            var m = DateRegex.Match(text);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        // ============================================================
        //                   EXTRACTORES POR CAMPO
        // ============================================================

        // 1) Código de resolución
        public static string? ExtractResolutionCode(string text)
        {
            var lines = text.Split('\n');

            var line = lines.FirstOrDefault(l =>
                l.Contains("Resolución", StringComparison.OrdinalIgnoreCase) &&
                (l.Contains("Nro", StringComparison.OrdinalIgnoreCase) ||
                 l.Contains("No.", StringComparison.OrdinalIgnoreCase)));

            if (line is null) return null;

            var m = Regex.Match(line, @"UTA-CONIN-\d{4}-\d{4}-[A-Z]");
            if (m.Success)
                return m.Value.Trim();

            m = Regex.Match(line, @"Nro\.?\s*([A-Z0-9\-]+[A-Z])");
            if (m.Success)
                return m.Groups[1].Value.Trim();

            return null;
        }

        // 2) Fecha de cabecera
        public static string? ExtractHeaderDate(string text)
        {
            var line = FindLineContaining(text, "Ambato,");
            if (line is null) return null;

            return ExtractFirstDate(line);
        }

        // 3) Fecha de la reunión
        public static string? ExtractMeetingDate(string text)
        {
            var lines = text.Split('\n');

            var para = lines.FirstOrDefault(l =>
                l.Contains("sesión", StringComparison.OrdinalIgnoreCase) &&
                l.Contains("efectuad", StringComparison.OrdinalIgnoreCase));

            if (para is null) return null;

            return ExtractFirstDate(para);
        }

        // 8) Duración del proyecto
        public static string? ExtractDuration(string text)
        {
            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains("Duración del proyecto", StringComparison.OrdinalIgnoreCase))
                {
                    var fromSameLine = ExtractAfterColon(line);
                    if (!string.IsNullOrEmpty(fromSameLine))
                        return fromSameLine;

                    if (i + 1 < lines.Length)
                        return lines[i + 1].Trim();
                }
            }

            return null;
        }

        // 9) Presupuesto / Financiamiento solicitado
        public static string? ExtractBudget(string text)
        {
            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains("Financiamiento solicitado", StringComparison.OrdinalIgnoreCase))
                {
                    var fromSameLine = ExtractAfterColon(line);
                    if (!string.IsNullOrEmpty(fromSameLine))
                        return fromSameLine;

                    if (i + 1 < lines.Length)
                        return lines[i + 1].Trim();
                }
            }

            return null;
        }

        // 10) Fecha de inicio de ejecución
        public static string? ExtractExecutionStartDate(string text)
        {
            var lines = text.Split('\n');

            var candidate = lines.FirstOrDefault(l =>
                l.Contains("inicio de ejecución", StringComparison.OrdinalIgnoreCase) &&
                l.Contains("proyecto", StringComparison.OrdinalIgnoreCase));

            if (candidate is null) return null;

            return ExtractFirstDate(candidate);
        }

        // 11) Verbo principal después de RESUELVE
        public static string? ExtractMainDecisionVerb(string text)
        {
            var idx = text.IndexOf("RESUELVE", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            var window = text[idx..];
            var lines = window.Split('\n');

            var firstItemLine = lines
                .SkipWhile(l => !l.Contains("1.", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (firstItemLine is null) return null;

            var m = Regex.Match(firstItemLine, @"1\.\s+([A-ZÁÉÍÓÚ]+)");
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }
    }
}
