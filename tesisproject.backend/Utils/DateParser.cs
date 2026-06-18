using System.Globalization;

namespace tesisproject.backend.Utils
{
    public class DateParser
    {
        public static DateTime? FromSpanishLongDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var culture = new CultureInfo("es-ES");

            // Formato exacto: 29 de marzo de 2021
            const string format = "d 'de' MMMM 'de' yyyy";

            if (DateTime.TryParseExact(input.Trim(), format, culture,
                                       DateTimeStyles.None, out var date))
            {
                return date;
            }

            return null;
        }
    }
}
