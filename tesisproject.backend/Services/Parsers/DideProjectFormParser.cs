using System.Text.RegularExpressions;
using tesisproject.shared.DTOs.Algorithms.Response;

namespace tesisproject.backend.Services.Parsers
{
    /// <summary>
    /// Helper methods for working with DIDE project forms (DIDE-PRY-002-2020).
    /// This class MUST NOT build DTOs or apply heavy normalization.
    /// It only provides small, reusable utilities.
    /// </summary>
    public static class DideProjectFormParser
    {
        /// <summary>
        /// Cuts a "linear" text block (raw text) between two markers.
        /// - Works on lines as they come from the PDF extractor.
        /// - Does NOT apply strong normalization (that is done externally).
        /// - Returns the block including the line where <paramref name="startMarker"/> is found,
        ///   and stops when a line containing <paramref name="nextSectionMarker"/> is found (excluded).
        /// </summary>
        /// <param name="rawText">Full text extracted in "dirty" mode.</param>
        /// <param name="startMarker">
        /// Fragment that identifies the line where the section begins (included).
        /// E.g.: "I. TÍTULO DEL PROYECTO", "Objetivo específico 2".
        /// </param>
        /// <param name="nextSectionMarker">
        /// Fragment that identifies the beginning of the next section (excluded).
        /// E.g.: "II. INFORMACIÓN GENERAL", "Nivel Actividad Resultado de la".
        /// Can be null or empty to take until the end of the document.
        /// </param>
        public static string ExtractSectionFromRawText(
            string rawText,
            string startMarker,
            string? nextSectionMarker)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            var lines = rawText
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n')
                .ToList();

            // 1) Find the line where the start marker appears
            int startIndex = lines.FindIndex(l =>
                l.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase) >= 0);

            if (startIndex < 0)
                return string.Empty;

            var collected = new List<string>();

            for (int i = startIndex + 1; i < lines.Count; i++) // <-- +1
            {
                var line = lines[i];

                if (!string.IsNullOrWhiteSpace(nextSectionMarker) &&
                    line.IndexOf(nextSectionMarker, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    i > startIndex + 1)
                {
                    break;
                }

                collected.Add(line);
            }

            if (collected.Count == 0)
                return string.Empty;

            // Return the block as-is (with line breaks).
            // Any further normalization is done outside (e.g. ResolutionParser.NormalizeText).
            return string.Join("\n", collected);
        }

        public static string ExtractSectionByMarkers(
    string rawText,
    string startMarker,
    string? nextSectionMarker)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            // Normalizamos saltos pero SIN dividir en líneas todavía
            var text = rawText
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            // Buscar inicio del marcador
            var startPos = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
            if (startPos < 0)
                return string.Empty;

            // Avanzar hasta el final del marcador (el contenido empieza después del título)
            startPos += startMarker.Length;

            int endPos;

            if (string.IsNullOrWhiteSpace(nextSectionMarker))
            {
                endPos = text.Length;
            }
            else
            {
                endPos = text.IndexOf(nextSectionMarker, startPos, StringComparison.OrdinalIgnoreCase);
                if (endPos < 0)
                    endPos = text.Length;
            }

            var section = text.Substring(startPos, endPos - startPos);

            // Limpieza básica
            return section.Trim();
        }


        /// <summary>
        /// Removes parenthetical content from a string.
        /// Useful for cases like:
        /// "Objetivo específico 2 (Indicar actividades y subactividades.)"
        /// → "Objetivo específico 2".
        /// </summary>
        public static string StripParentheses(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return Regex.Replace(input, @"\s*\(.*?\)\s*", " ").Trim();
        }

        public static IEnumerable<string> StripParentheses(IEnumerable<string> inputs)
        {
            if (inputs == null)
                return Enumerable.Empty<string>();

            return inputs.Select(StripParentheses);
        }

        public static List<ResearcherInfo> ExtractResearchersFromMembersSection(string membersSectionNormalized)
        {
            var result = new List<ResearcherInfo>();

            if (string.IsNullOrWhiteSpace(membersSectionNormalized))
                return result;

            // Patrones de rol que SÍ hemos visto en tu texto
            const string rolePattern =
                @"Coordinador Principal del Proyecto" +
                @"|Coordinador Subrogante del Proyecto" +
                @"|Investigador\s+\d+";

            // El correo SIEMPRE está después de "Correo electrónico institucional – UTA"
            var pattern = $@"({rolePattern})[\s\S]*?Correo electrónico institucional\s*[–-]?\s*UTA\s+(\S+)";

            var matches = Regex.Matches(
                membersSectionNormalized,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                var roleRaw = match.Groups[1].Value.Trim();
                var emailRaw = match.Groups[2].Value.Trim();

                // Opcional: normalizar rol si quieres quitar dobles espacios, etc.
                // Si ya tienes ResolutionParser.NormalizeText, puedes usarla:
                // var role = ResolutionParser.NormalizeText(roleRaw);
                var role = roleRaw;

                result.Add(new ResearcherInfo
                {
                    RoleName = role,
                    email = emailRaw
                });
            }

            return result;
        }

        public static int OnlyDigits(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            var digits = new string(input.Where(char.IsDigit).ToArray());

            return int.TryParse(digits, out int number) ? number : 0;
        }

        public static int ParseLatinNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            // 1) Remover símbolos no numéricos
            var cleaned = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());

            // 2) Caso formato LATAM: miles="." decimales=","
            if (cleaned.Contains(","))
            {
                var lastComma = cleaned.LastIndexOf(',');

                // Parte antes de la coma
                var intPart = cleaned.Substring(0, lastComma)
                                     .Replace(".", "")  // quitar puntos de miles
                                     .Replace(",", ""); // quitar comas internas si las hubiera

                // Convertir
                return int.TryParse(intPart, out int number) ? number : 0;
            }

            // 3) Caso formato USA: miles="," decimales="."
            if (cleaned.Contains("."))
            {
                var lastDot = cleaned.LastIndexOf('.');

                var intPart = cleaned.Substring(0, lastDot)
                                     .Replace(",", ""); // quitar comas de miles

                return int.TryParse(intPart, out int number) ? number : 0;
            }

            // 4) Si no hay separadores, se convierte directo
            return int.TryParse(cleaned, out int n) ? n : 0;
        }

        public static string DetectResearchType(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            // 1) Trabajamos primero por líneas, por si vienen separadas
            var normalizedLines = rawText
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var candidates = new[] { "Aplicada", "Experimental" };

            // Caso A: cada opción en su propia línea, o línea que contiene X y el texto del tipo
            foreach (var line in normalizedLines)
            {
                if (!line.Contains('X', StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var type in candidates)
                {
                    if (line.IndexOf(type, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return type;
                    }
                }
            }

            // Caso B: todo quedó aplastado en una sola línea (p.ej. "Aplicada X Experimental")
            // Normalizamos para simplificar búsquedas
            var flat = ResolutionParser.NormalizeText(rawText);

            int idxX = flat.IndexOf(" X ", StringComparison.OrdinalIgnoreCase);
            if (idxX < 0)
                idxX = flat.IndexOf("X", StringComparison.OrdinalIgnoreCase);

            if (idxX < 0)
                return string.Empty;

            int idxAplicada = flat.IndexOf("Aplicada", StringComparison.OrdinalIgnoreCase);
            int idxExperimental = flat.IndexOf("Experimental", StringComparison.OrdinalIgnoreCase);

            // Patrón típico que viste: "Aplicada X Experimental"
            if (idxAplicada >= 0 && idxAplicada < idxX &&
                (idxExperimental < 0 || idxX < idxExperimental))
            {
                return "Aplicada";
            }

            // Patrón inverso posible: "Aplicada Experimental X" (X marcando Experimental)
            if (idxExperimental >= 0 && idxExperimental < idxX)
            {
                return "Experimental";
            }

            return string.Empty;
        }
        public static List<string> ExtractResearchLines(string rawText)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(rawText))
                return result;

            // Aplastamos el texto: sin saltos, acentos, espacios dobles, etc.
            var flat = ResolutionParser.NormalizeText(rawText);

            const string lineMarker = "LINEA DE INVESTIGACION";

            // Buscamos el marcador dentro del texto normalizado
            var idxLine = flat.IndexOf(lineMarker, StringComparison.OrdinalIgnoreCase);
            if (idxLine < 0)
                return result;

            // Tomamos TODO lo que viene después del marcador
            var start = idxLine + lineMarker.Length;
            if (start >= flat.Length)
                return result;

            var block = flat.Substring(start)
                .Trim(' ', ':', '-', '.'); // limpiamos basura típica al inicio/fin

            if (string.IsNullOrWhiteSpace(block))
                return result;

            // Separamos SOLO por punto, cada parte es una línea de investigación
            var parts = block.Split('.', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var line = part.Trim();
                if (!string.IsNullOrWhiteSpace(line))
                    result.Add(line);
            }

            return result;
        }

        public static string StripLeadingNonLetters(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input ?? string.Empty;

            // Busca el índice del primer carácter alfabético (A-Z o a-z)
            int firstLetterIndex = -1;
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsLetter(input[i]))
                {
                    firstLetterIndex = i;
                    break;
                }
            }

            // Si no hay letras, devuelves vacío
            if (firstLetterIndex == -1)
                return string.Empty;

            // Recorta desde la primera letra real
            return input.Substring(firstLetterIndex).Trim();
        }

        public static List<DideObjectiveInfo> ExtractAllSpecificObjectives(string rawText)
        {
            var result = new List<DideObjectiveInfo>();
            if (string.IsNullOrWhiteSpace(rawText))
                return result;

            // Normalizar saltos de línea
            var text = rawText
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            // Dividir en bloques, cada uno comienza en "Objetivo específico N"
            var chunks = Regex.Split(text, @"(?=Objetivo\s+específico\s+\d+)", RegexOptions.IgnoreCase);

            foreach (var rawChunk in chunks)
            {
                var chunk = rawChunk.Trim();
                if (string.IsNullOrWhiteSpace(chunk))
                    continue;

                // 1) Número del objetivo
                var numMatch = Regex.Match(
                    chunk,
                    @"Objetivo\s+específico\s+(\d+)",
                    RegexOptions.IgnoreCase
                );
                if (!numMatch.Success || !int.TryParse(numMatch.Groups[1].Value, out int number))
                    continue;

                // 2) Separar el objetivo de las actividades
                int cutIndex = FindActivitiesHeaderIndex(chunk);

                Console.WriteLine($"=== DEBUG Objetivo {number} ===");
                Console.WriteLine($"cutIndex: {cutIndex}");

                string objectivePart = cutIndex >= 0
                    ? chunk.Substring(0, cutIndex).Trim()
                    : chunk;

                Console.WriteLine($"objectivePart (primeros 300 chars): {objectivePart.Substring(0, Math.Min(300, objectivePart.Length))}");

                // 3) Extraer descripción del objetivo sin el prefijo "Objetivo específico N"
                string objectiveText = ExtractObjectiveDescription(objectivePart);
                objectiveText = RemoveDidePageHeaders(objectiveText);
                objectiveText = RemoveActivityInstructionNotes(objectiveText);
                objectiveText = RemoveDideInstructionParentheses(objectiveText);
                objectiveText = StripLeadingNonLetters(objectiveText);

                var dto = new DideObjectiveInfo
                {
                    ObjectiveType = 2,
                    objetiveNumber = number,
                    ObjectiveText = objectiveText,
                    Activities = new List<DideObjectiveActivityInfo>()
                };

                result.Add(dto);
            }

            return result;
        }
        private static int FindActivitiesHeaderIndex(string chunk)
        {
            if (string.IsNullOrWhiteSpace(chunk))
                return -1;

            // ESTRATEGIA: Buscar primero con salto de línea (más confiable)
            // Si no encuentra, buscar sin salto de línea pero con validación más estricta

            // ===== PRIORIDAD 1: Buscar con salto de línea =====

            // 1.1 Buscar "Nivel" después de salto de línea
            var nivelMatchNewLine = Regex.Match(chunk, @"(?<=\n)\s*Nivel\b", RegexOptions.IgnoreCase);
            if (nivelMatchNewLine.Success)
            {
                int nivelPos = nivelMatchNewLine.Index;
                string afterNivel = chunk.Substring(nivelPos + nivelMatchNewLine.Length);

                bool tieneActividad = Regex.IsMatch(afterNivel, @"Actividad\b", RegexOptions.IgnoreCase);
                bool tieneResultado = Regex.IsMatch(afterNivel, @"Resultado\s+de\s+la\b", RegexOptions.IgnoreCase);
                bool tieneDescripcion = Regex.IsMatch(afterNivel, @"Descripci[oó]n\s+de\s+la\b", RegexOptions.IgnoreCase);

                if (tieneActividad || tieneResultado || tieneDescripcion)
                    return nivelPos;
            }

            // 1.2 Buscar "Actividad" después de salto de línea
            var actividadMatchNewLine = Regex.Match(chunk, @"(?<=\n)\s*Actividad\b", RegexOptions.IgnoreCase);
            if (actividadMatchNewLine.Success)
            {
                int actividadPos = actividadMatchNewLine.Index;
                string afterActividad = chunk.Substring(actividadPos + actividadMatchNewLine.Length);

                bool tieneResultado = Regex.IsMatch(afterActividad, @"Resultado\s+de\s+la\b", RegexOptions.IgnoreCase);
                bool tieneDescripcion = Regex.IsMatch(afterActividad, @"Descripci[oó]n\s+de\s+la\b", RegexOptions.IgnoreCase);

                if (tieneResultado || tieneDescripcion)
                    return actividadPos;
            }

            // 1.3 Buscar "Resultado de la" después de salto de línea
            var resultadoMatchNewLine = Regex.Match(chunk, @"(?<=\n)\s*Resultado\s+de\s+la\b", RegexOptions.IgnoreCase);
            if (resultadoMatchNewLine.Success)
            {
                int resultadoPos = resultadoMatchNewLine.Index;
                string afterResultado = chunk.Substring(resultadoPos + resultadoMatchNewLine.Length);

                bool tieneDescripcion = Regex.IsMatch(afterResultado, @"Descripci[oó]n\s+de\s+la\b", RegexOptions.IgnoreCase);

                if (tieneDescripcion)
                    return resultadoPos;
            }

            // 1.4 Buscar "Descripción de la" después de salto de línea
            var descripcionMatchNewLine = Regex.Match(chunk, @"(?<=\n)\s*Descripci[oó]n\s+de\s+la\b", RegexOptions.IgnoreCase);
            if (descripcionMatchNewLine.Success)
            {
                return descripcionMatchNewLine.Index;
            }

            // ===== PRIORIDAD 2: Si no encontró con salto de línea, buscar sin él =====
            // Pero con validación más estricta: debe tener al menos 2 columnas siguientes

            // 2.1 Buscar "Nivel" en cualquier lugar
            var nivelMatch = Regex.Match(chunk, @"Nivel\b", RegexOptions.IgnoreCase);
            if (nivelMatch.Success)
            {
                int nivelPos = nivelMatch.Index;
                string afterNivel = chunk.Substring(nivelPos + nivelMatch.Length);

                bool tieneActividad = Regex.IsMatch(afterNivel, @"Actividad\b", RegexOptions.IgnoreCase);
                bool tieneResultado = Regex.IsMatch(afterNivel, @"Resultado\s+de\s+la\b", RegexOptions.IgnoreCase);
                bool tieneDescripcion = Regex.IsMatch(afterNivel, @"Descripci[oó]n\s+de\s+la\b", RegexOptions.IgnoreCase);

                // Debe tener al menos 2 de las 3 columnas siguientes
                int count = (tieneActividad ? 1 : 0) + (tieneResultado ? 1 : 0) + (tieneDescripcion ? 1 : 0);
                if (count >= 2)
                    return nivelPos;
            }

            // 2.2 Buscar "Actividad" en cualquier lugar
            var actividadMatch = Regex.Match(chunk, @"Actividad\b", RegexOptions.IgnoreCase);
            if (actividadMatch.Success)
            {
                int actividadPos = actividadMatch.Index;
                string afterActividad = chunk.Substring(actividadPos + actividadMatch.Length);

                bool tieneResultado = Regex.IsMatch(afterActividad, @"Resultado\s+de\s+la\b", RegexOptions.IgnoreCase);
                bool tieneDescripcion = Regex.IsMatch(afterActividad, @"Descripci[oó]n\s+de\s+la\b", RegexOptions.IgnoreCase);

                // Debe tener ambas columnas siguientes
                if (tieneResultado && tieneDescripcion)
                    return actividadPos;
            }

            // 2.3 Buscar "Resultado de la" seguido de "Descripción de la" (muy específico del header)
            var resultadoMatch = Regex.Match(chunk, @"Resultado\s+de\s+la\b", RegexOptions.IgnoreCase);
            if (resultadoMatch.Success)
            {
                int resultadoPos = resultadoMatch.Index;
                string afterResultado = chunk.Substring(resultadoPos + resultadoMatch.Length);

                bool tieneDescripcion = Regex.IsMatch(afterResultado, @"Descripci[oó]n\s+de\s+la\b", RegexOptions.IgnoreCase);

                if (tieneDescripcion)
                    return resultadoPos;
            }

            return -1;
        }

        public static string RemoveDidePageHeaders(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? string.Empty;

            var normalized = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            var cleanedLines = normalized
                .Split('\n')
                .Where(line =>
                {
                    var trimmed = line.Trim();
                    // Elimina líneas tipo: "FORMATO DIDE-PRY-002-2020", "FORMATO DIDE-LO-QUE-SEA"
                    return !trimmed.StartsWith("FORMATO DIDE-", StringComparison.OrdinalIgnoreCase);
                });

            return string.Join("\n", cleanedLines);
        }


        public static string RemoveActivityInstructionNotes(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? string.Empty;

            // Normalización básica
            var normalized = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            // Patrón robusto:
            // - Elimina paréntesis que contengan las palabras "indicar", "actividad", "subactividad"
            // - Soporta variaciones, mayúsc/minúsc, acentos, puntos, espacios.
            var pattern = @"\(\s*(indicar)[^\)]*(actividad|subactividad)[^\)]*\)";

            var cleaned = Regex.Replace(
                normalized,
                pattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            return cleaned;
        }


        // Devuelve el índice de la primera aparición de cualquiera de los patrones en el texto
        private static int IndexOfFirst(string text, string[] patterns)
        {
            int bestIndex = -1;

            foreach (var p in patterns)
            {
                int idx = text.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && (bestIndex == -1 || idx < bestIndex))
                    bestIndex = idx;
            }

            return bestIndex;
        }

        public static string RemoveDideInstructionParentheses(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? string.Empty;

            // Remueve específicamente "(Indicar actividades y subactividades.)"
            text = Regex.Replace(
                text,
                @"\(\s*I[a-zA-Z\s]+y[a-zA-Z\s]+?\.\s*\)",
                "",
                RegexOptions.IgnoreCase
            ).Trim();

            return text;
        }


        // Quita "Objetivo específico N" y devuelve sólo la descripción
        private static string ExtractObjectiveDescription(string block)
        {
            var lines = block
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            if (lines.Count == 0)
                return string.Empty;

            string firstLine = lines[0];

            var match = Regex.Match(
                firstLine,
                @"Objetivo\s+específico\s+\d+\s*(.*)",
                RegexOptions.IgnoreCase
            );

            if (match.Success)
                return match.Groups[1].Value.Trim();

            return firstLine;
        }
    }
}