using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardBase : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter] public CardTone Tone { get; set; } = CardTone.Default;

        protected string ContainerClasses =>
            $"rounded-2xl shadow border {ToneBorder()} {Class}";

        private string ToneBorder() => Tone switch
        {
            CardTone.Info => "border-blue-300",
            CardTone.Warning => "border-yellow-400",
            CardTone.Danger => "border-red-400",
            _ => "border-gray-200"
        };
    }
}