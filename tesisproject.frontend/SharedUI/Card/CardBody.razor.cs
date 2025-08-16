using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Card
{
    public partial class CardBody
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
    }
}
