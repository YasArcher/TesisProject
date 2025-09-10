using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardBase : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter] public CardTone Tone { get; set; } = CardTone.Default;

        // Captura de atributos adicionales (onclick, tabindex, aria-label, etc.)
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        // Clases principales del card
        private string ContainerBase =>
            "rounded-2xl shadow border transition-shadow";

        // Clases finales que combinan base + tono + clases externas
        protected IDictionary<string, object> MergedAttributes
        {
            get
            {
                var dict = AdditionalAttributes is null
                    ? new Dictionary<string, object>()
                    : new Dictionary<string, object>(AdditionalAttributes);

                var toneBorder = ToneBorder();

                if (dict.TryGetValue("class", out var clsObj) && clsObj is string cls && !string.IsNullOrWhiteSpace(cls))
                    dict["class"] = $"{ContainerBase} {toneBorder} {cls}";
                else
                    dict["class"] = $"{ContainerBase} {toneBorder} {Class}";

                return dict;
            }
        }

        // Define colores de borde por tono
        private string ToneBorder() => Tone switch
        {
            CardTone.Info => "border-blue-300",
            CardTone.Warning => "border-yellow-400",
            CardTone.Danger => "border-red-400",
            _ => "border-border" // usa tu token global institucional
        };
    }
}
