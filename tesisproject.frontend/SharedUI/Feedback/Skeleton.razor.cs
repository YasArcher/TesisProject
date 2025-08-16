using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Feedback
{
    public partial class Skeleton
    {
        [Parameter]
        public SkeletonShape Shape { get; set; } = SkeletonShape.Rect;

        [Parameter]
        public int Width { get; set; } = 120; // Pixels

        [Parameter]
        public int Height { get; set; } = 16; // Pixels

        [Parameter]
        public bool Rounded { get; set; } = true;

        private string WidthPx => $"{Width}px";
        private string HeightPx => $"{Height}px";
        private string RoundedClass => Rounded ? "rounded-md" : string.Empty;
    }
}
