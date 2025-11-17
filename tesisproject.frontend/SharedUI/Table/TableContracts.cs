using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI
{
    public class ColumnDefinition<TItem>
    {
        public string Title { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public bool IsSortable { get; set; }
        public RenderFragment<TItem>? Template { get; set; }
    }
}