using System.Globalization;

namespace tesisproject.frontend.Utils
{
    // DateExtensions.cs
    public static class DateExtensions
    {
        private static readonly CultureInfo SpanishCulture = new CultureInfo("es-ES");

        public static string ToSpanishDate(this DateTime? date)
        {
            if (!date.HasValue) return "Sin fecha";
            return date.Value.ToString("dd 'de' MMMM 'de' yyyy", SpanishCulture);
        }

        public static string ToSpanishShortDate(this DateTime? date)
        {
            if (!date.HasValue) return "-";
            return date.Value.ToString("dd/MM/yyyy", SpanishCulture);
        }

        public static string ToRelativeTime(this DateTime? date)
        {
            if (!date.HasValue) return "Sin fecha";

            var diff = DateTime.Now - date.Value;

            if (diff.TotalDays < 1) return "Hoy";
            if (diff.TotalDays < 2) return "Ayer";
            if (diff.TotalDays < 7) return $"Hace {(int)diff.TotalDays} días";
            if (diff.TotalDays < 30) return $"Hace {(int)(diff.TotalDays / 7)} semanas";
            if (diff.TotalDays < 365) return $"Hace {(int)(diff.TotalDays / 30)} meses";

            return date.Value.ToString("dd/MM/yyyy", SpanishCulture);
        }
    }
}
