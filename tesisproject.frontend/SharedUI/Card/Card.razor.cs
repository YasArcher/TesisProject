using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardBase : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter] public CardTone Tone { get; set; } = CardTone.Default;

        [Parameter] public bool Interactive { get; set; } = false;
        [Parameter] public bool Compact { get; set; } = false;

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        private string ContainerBase =>
            "rounded-xl border-2 bg-background shadow-sm overflow-hidden min-h-0";

        private string ToneBorder() => Tone switch
        {
            CardTone.Info => "border-primary/30",
            CardTone.Warning => "border-accent/40",
            CardTone.Danger => "border-error/40",
            _ => "border-border"
        };

        private string InteractionClasses() =>
            Interactive
                ? "transition-all duration-150 hover:border-primary/40 hover:bg-primary/5"
                : string.Empty;

        protected IDictionary<string, object> MergedAttributes
        {
            get
            {
                var dict = AdditionalAttributes is null
                    ? new Dictionary<string, object>()
                    : new Dictionary<string, object>(AdditionalAttributes);

                var finalClass =
                    $"{ContainerBase} {ToneBorder()} {InteractionClasses()} {Class}".Trim();

                if (dict.TryGetValue("class", out var clsObj) &&
                    clsObj is string cls &&
                    !string.IsNullOrWhiteSpace(cls))
                {
                    dict["class"] = $"{finalClass} {cls}".Trim();
                }
                else
                {
                    dict["class"] = finalClass;
                }

                return dict;
            }
        }
    }
}