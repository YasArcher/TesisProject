using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Feedback
{
    public partial class Spinner
    {
        [Parameter]
        public int Size { get; set; } = 24; // Pixels

        [Parameter]
        public string? Label { get; set; }

        private string SizePx => $"{Size}px";
    }
}
