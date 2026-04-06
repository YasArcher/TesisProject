using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardHeaderBase : ComponentBase
    {
        [CascadingParameter] public CardBase? Parent { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? Class { get; set; }

        protected string HeaderClasses =>
            $"px-5 py-4 border-b border-border shrink-0 {ToneBg()} {Class}".Trim();

        private string ToneBg()
        {
            if (Parent is null) return "bg-background/50 text-foreground";

            return Parent.Tone switch
            {
                CardTone.Info => "bg-primary/5 text-foreground",
                CardTone.Warning => "bg-accent/10 text-foreground",
                CardTone.Danger => "bg-error/5 text-foreground",
                _ => "bg-background/50 text-foreground"
            };
        }
    }
}