using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI
{
    public class ActionButtonBase : ComponentBase
    {
        [Parameter] public string? Text { get; set; }
        [Parameter] public string? Icon { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Solid;
        [Parameter] public ButtonIntent Intent { get; set; } = ButtonIntent.Primary;
        [Parameter] public string Size { get; set; } = "md";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        // Compatibilidad con ExecuteAsync
        [Parameter] public EventCallback ExecuteAsync { get; set; }

        [Parameter] public EventCallback OnClick { get; set; }

        protected string CssClasses => BuildCss();

        private string BuildCss()
        {
            var classes = "inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-sm";

            if (!string.IsNullOrWhiteSpace(Class))
                classes += " " + Class;

            return classes;
        }

        protected async Task HandleClick()
        {
            if (Disabled) return;

            if (OnClick.HasDelegate)
                await OnClick.InvokeAsync(null);
            else if (ExecuteAsync.HasDelegate)
                await ExecuteAsync.InvokeAsync(null);
        }
    }
}
