using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.StatCard
{
    public class StatCardBase : ComponentBase
    {
        // Content
        [Parameter] public string? Title { get; set; }
        [Parameter] public string? Value { get; set; }              // raw text value (e.g., "10,000.00")
        [Parameter] public string? FormattedValue { get; set; }     // optional preformatted string (takes precedence)
        [Parameter] public RenderFragment? Icon { get; set; }       // optional icon (svg)
        [Parameter] public RenderFragment? ChildContent { get; set; } // custom value/template
        [Parameter] public RenderFragment? Footer { get; set; }     // small caption or delta tag

        // State
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public string? Error { get; set; }

        // Appearance
        [Parameter] public StatCardVariant Variant { get; set; } = StatCardVariant.Outlined;
        [Parameter] public StatCardEmphasis Emphasis { get; set; } = StatCardEmphasis.Normal;
        [Parameter] public StatCardSize Size { get; set; } = StatCardSize.Md;
        [Parameter] public bool Hoverable { get; set; } = true;
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Style { get; set; }

        // Behavior
        [Parameter] public EventCallback OnClick { get; set; }
        [Parameter] public bool Disabled { get; set; } = false;

        protected string BuildWrapperClass()
        {
            var classes = new List<string>
            {
                "stat-card",
                $"stat-{Variant.ToString().ToLower()}",
                $"stat-{Emphasis.ToString().ToLower()}",
                $"stat-size-{Size.ToString().ToLower()}"
            };

            if (Hoverable && !Disabled) classes.Add("stat-hover");
            if (Disabled) classes.Add("stat-disabled");
            if (!string.IsNullOrWhiteSpace(Class)) classes.Add(Class);

            return string.Join(" ", classes);
        }

        protected async Task HandleClick()
        {
            if (Disabled || !OnClick.HasDelegate) return;
            await OnClick.InvokeAsync();
        }
    }
}
