using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.frontend.Models.Configuration
{
    public class DynamicFieldState
    {
        public ResolvedFormFieldDto Definition { get; set; } = new();
        public List<CatalogItemDto> CatalogItems { get; set; } = new();
        public string? TextValue { get; set; }
        public int? IntValue { get; set; }
        public decimal? DecimalValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }
        public int? CatalogValue { get; set; }
        public string? OptionValue { get; set; }
        public string? JsonValue { get; set; }

        public string DisplayValue
        {
            get
            {
                if (CatalogValue.HasValue)
                {
                    var match = CatalogItems.FirstOrDefault(x => x.Id == CatalogValue.Value);
                    return match?.Name ?? CatalogValue.Value.ToString();
                }

                if (!string.IsNullOrWhiteSpace(OptionValue)) return OptionValue!;
                if (!string.IsNullOrWhiteSpace(TextValue)) return TextValue!;
                if (IntValue.HasValue) return IntValue.Value.ToString();
                if (DecimalValue.HasValue) return DecimalValue.Value.ToString("0.##");
                if (DateValue.HasValue) return DateValue.Value.ToString("yyyy-MM-dd");
                if (BoolValue.HasValue) return BoolValue.Value ? "Sí" : "No";
                if (!string.IsNullOrWhiteSpace(JsonValue)) return JsonValue!;
                return string.Empty;
            }
        }
    }

    public class DynamicSectionState
    {
        public string GroupName { get; set; } = "General";
        public int DisplayOrder { get; set; }
        public List<DynamicFieldState> Fields { get; set; } = new();
    }

    public class DynamicParticipantState
    {
        public int Index { get; set; }
        public List<DynamicSectionState> Sections { get; set; } = new();
    }
}
