using System.Globalization;
using System.Text;

namespace tesisproject.frontend.Utils
{
    public class Normalizers
    {
        public static string NormalizeForSearch(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Normaliza a FormD (letras + marcas de acento separadas)
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                // Quitamos las marcas de acento (NonSpacingMark)
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }

            // Volvemos a FormC para tener texto “normal”
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

    }
}
