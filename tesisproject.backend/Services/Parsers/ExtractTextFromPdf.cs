using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tesisproject.backend.Services.Parsers
{
    public class PdfTextExtractor
    {
        // ============================================
        //   MODO "SUCI0" - RESOLUCIONES (ya existente)
        // ============================================
        // Método mejorado con agrupación por líneas
        public string ExtractTextFromPdfImproved(Stream pdfStream)
        {
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(pdfStream);

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (!words.Any()) continue;

                // Agrupar palabras por línea
                var lines = GroupWordsIntoLines(words, toleranceY: 5);

                // Ordenar líneas de arriba a abajo
                var sortedLines = lines
                    .OrderByDescending(line => line.Max(w => w.BoundingBox.Top))
                    .ToList();

                foreach (var line in sortedLines)
                {
                    // Ordenar palabras de izquierda a derecha
                    var sortedWords = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .Select(w => w.Text)
                        .ToList();

                    // ⚠️ CAMBIO: Usar espacios variables y saltos múltiples (texto "sucio")
                    sb.Append(string.Join("  ", sortedWords)); // doble espacio intencional
                    sb.Append("\n\n"); // doble salto de línea
                }

                sb.Append("\n\n\n"); // Triple salto entre páginas
            }

            return sb.ToString();
        }

        // =========================================================
        //   NUEVO MODO "LIMPIO" - FORMATO DIDE (tablas, secciones)
        // =========================================================
        /// <summary>
        /// Extrae el texto del PDF con un formato más limpio y estable,
        /// pensado para formularios/tablas DIDE:
        /// - Una sola separación por espacio entre palabras.
        /// - Una sola línea por fila visual.
        /// - Doble salto de línea entre páginas.
        /// </summary>
        public string ExtractTextForDideForm(Stream pdfStream)
        {
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(pdfStream);

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (!words.Any()) continue;

                // 1) Agrupar palabras por líneas (ya existente)
                var lines = GroupWordsIntoLines(words, toleranceY: 3);

                // 2) Detectar columnas usando TODAS las palabras de la página
                var columnRanges = DetectColumnRanges(words);

                // 3) Ordenar líneas de arriba hacia abajo
                var sortedLines = lines
                    .OrderByDescending(line => line.Max(w => w.BoundingBox.Top))
                    .ToList();

                foreach (var line in sortedLines)
                {
                    // Crear buckets de columnas
                    var columnTexts = new List<List<Word>>();
                    for (int i = 0; i < columnRanges.Count; i++)
                        columnTexts.Add(new List<Word>());

                    // 4) Asignar cada palabra a su columna correspondiente
                    foreach (var w in line)
                    {
                        int colIndex = FindColumnIndex(w, columnRanges);
                        columnTexts[colIndex].Add(w);
                    }

                    // 5) Ordenar palabras dentro de cada columna
                    var finalColumns = columnTexts
                        .Select(col => col.OrderBy(w => w.BoundingBox.Left)
                                          .Select(w => w.Text.Trim())
                                          .Where(t => !string.IsNullOrWhiteSpace(t))
                                          .ToList())
                        .ToList();

                    // 6) Construir fila final evitando columnas vacías
                    var joined = finalColumns
                        .Where(col => col.Count > 0)
                        .Select(col => string.Join(" ", col))
                        .ToArray();

                    if (joined.Length > 0)
                        sb.AppendLine(string.Join(" | ", joined)); // Separador visible
                }

                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        private List<(double MinX, double MaxX)> DetectColumnRanges(List<Word> words)
        {
            // Tomamos los LEFT de cada palabra
            var xs = words.Select(w => w.BoundingBox.Left).OrderBy(x => x).ToList();

            var ranges = new List<(double MinX, double MaxX)>();
            if (!xs.Any()) return ranges;

            double currentMin = xs[0];
            double previous = xs[0];

            const double gapThreshold = 35; // Separación grande = nueva columna

            foreach (var x in xs)
            {
                if (x - previous > gapThreshold)
                {
                    ranges.Add((currentMin, previous));
                    currentMin = x;
                }
                previous = x;
            }

            ranges.Add((currentMin, previous));
            return ranges;
        }

        private int FindColumnIndex(Word w, List<(double MinX, double MaxX)> ranges)
        {
            double x = w.BoundingBox.Left;

            for (int i = 0; i < ranges.Count; i++)
            {
                if (x >= ranges[i].MinX && x <= ranges[i].MaxX)
                    return i;
            }

            return ranges.Count - 1; // fallback
        }


        // Agrupa palabras en la misma línea horizontal
        private List<List<Word>> GroupWordsIntoLines(List<Word> words, double toleranceY)
        {
            var lines = new List<List<Word>>();
            var remaining = new List<Word>(words);

            while (remaining.Any())
            {
                var currentWord = remaining.First();
                var currentLine = new List<Word> { currentWord };
                remaining.Remove(currentWord);

                // Buscar palabras en la misma altura (Top)
                var sameLineWords = remaining
                    .Where(w => Math.Abs(w.BoundingBox.Top - currentWord.BoundingBox.Top) < toleranceY)
                    .ToList();

                currentLine.AddRange(sameLineWords);
                remaining.RemoveAll(w => sameLineWords.Contains(w));

                lines.Add(currentLine);
            }

            return lines;
        }

        public string ExtractTextFromPdfUltraDirty(Stream pdfStream)
        {
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(pdfStream);

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (!words.Any())
                    continue;

                // =========================
                // 1) Cortar en 2 columnas (splitX global)
                // =========================
                var centers = words
                    .Select(w => w.BoundingBox.Left + w.BoundingBox.Width / 2.0)
                    .OrderBy(x => x)
                    .ToList();

                double? splitX = null;

                if (centers.Count >= 2)
                {
                    double maxGap = 0;
                    int idx = -1;

                    for (int i = 0; i < centers.Count - 1; i++)
                    {
                        var gap = centers[i + 1] - centers[i];
                        if (gap > maxGap)
                        {
                            maxGap = gap;
                            idx = i;
                        }
                    }

                    if (idx >= 0)
                    {
                        splitX = (centers[idx] + centers[idx + 1]) / 2.0;
                    }
                }

                // =========================
                // 2) Agrupar por líneas y distribuir por columnas
                // =========================
                var lines = GroupWordsIntoLines(words, toleranceY: 5);

                var sortedLines = lines
                    .OrderByDescending(line => line.Max(w => w.BoundingBox.Top))
                    .ToList();

                foreach (var line in sortedLines)
                {
                    var sortedWords = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .ToList();

                    if (!sortedWords.Any())
                        continue;

                    // Si por algún motivo no hay splitX, dejamos la línea plana
                    if (splitX is null)
                    {
                        sb.AppendLine(string.Join(" ", sortedWords.Select(w => w.Text)));
                        continue;
                    }

                    var leftBuilder = new StringBuilder();

                    // SOLO procesamos la columna IZQUIERDA
                    foreach (var w in sortedWords)
                    {
                        double centerX = w.BoundingBox.Left + w.BoundingBox.Width / 2.0;

                        if (centerX <= splitX.Value)
                        {
                            if (leftBuilder.Length > 0)
                                leftBuilder.Append(' ');
                            leftBuilder.Append(w.Text);
                        }
                        // Ya no procesamos la columna derecha
                    }

                    var left = leftBuilder.ToString().Trim();

                    // Solo agregamos si hay contenido en la columna izquierda
                    if (!string.IsNullOrWhiteSpace(left))
                    {
                        sb.AppendLine(left);
                    }
                }

                sb.AppendLine();
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}