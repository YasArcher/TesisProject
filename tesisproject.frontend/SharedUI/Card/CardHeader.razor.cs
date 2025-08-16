using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardHeaderBase : ComponentBase
    {
        [CascadingParameter] public CardBase? Parent { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? Class { get; set; }

        protected string HeaderClasses =>
            $"px-4 py-2 rounded-t-2xl font-semibold {ToneBg()} {Class}";

        private string ToneBg()
        {
            if (Parent is null) return "bg-gray-50";
            return Parent.Tone switch
            {
                CardTone.Info => "bg-gradient-to-r from-blue-100 to-blue-200 text-blue-800",
                CardTone.Warning => "bg-gradient-to-r from-yellow-100 to-yellow-200 text-yellow-800",
                CardTone.Danger => "bg-gradient-to-r from-red-100 to-red-200 text-red-800",
                _ => "bg-gray-50 text-gray-700"
            };
        }
    }
}