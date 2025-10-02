using static tesisproject.frontend.SharedUI.FilterBar.FilterBar;

namespace tesisproject.frontend.SharedUI.FilterBar
{
    public class FilterBarConfig
    {
        public string? Title { get; set; }
        public string? SearchPlaceholder { get; set; } = "Search…";
        public List<FilterItemDescriptor> Items { get; set; } = new();
        public bool ShowChips { get; set; } = true;
        public int DebounceMs { get; set; } = 350;
        public SortRule? DefaultSort { get; set; }
    }
}
