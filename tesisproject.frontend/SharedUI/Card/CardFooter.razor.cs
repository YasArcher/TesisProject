using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardFooter
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
    }
}
