using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardBody : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter] public bool Scroll { get; set; } = false;
        [Parameter] public bool Compact { get; set; } = false;

        private string Padding => Compact ? "px-4 py-3" : "p-5";

        private string _classes =>
            Scroll
                ? $"{Padding} flex flex-col flex-1 min-h-0 overflow-y-auto {Class}".Trim()
                : $"{Padding} flex flex-col flex-1 min-h-0 overflow-hidden {Class}".Trim();
    }
}