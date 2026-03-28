using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardBody : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter] public bool Scroll { get; set; } = false;

        private string _classes =>
            Scroll
                ? $"px-4 py-2 flex flex-col flex-1 min-h-0 overflow-y-auto {Class}"
                : $"px-4 py-2 flex flex-col flex-1 min-h-0 overflow-hidden {Class}";
    }
}