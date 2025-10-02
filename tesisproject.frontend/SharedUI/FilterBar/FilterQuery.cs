using static tesisproject.frontend.SharedUI.FilterBar.FilterBar;

namespace tesisproject.frontend.SharedUI.FilterBar
{
    public class FilterQuery
    {
        public string? Search { get; set; }
        public List<SortRule>? Sort { get; set; }
        public List<FilterRule> Filters { get; set; } = new();
    }
}
