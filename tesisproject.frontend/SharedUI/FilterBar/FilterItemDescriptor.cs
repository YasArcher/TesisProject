using static tesisproject.frontend.SharedUI.FilterBar.FilterBar;

namespace tesisproject.frontend.SharedUI.FilterBar
{
    public class FilterItemDescriptor
    {
        public string? GroupKey { get; set; }
        public string? GroupLabel { get; set; }

        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "search"; // search | select | multiselect | daterange | numberrange | boolean
        public string? EntityField { get; set; }
        public List<string>? Operators { get; set; }
        public string? Placeholder { get; set; }
        public object? DefaultValue { get; set; }
        public List<OptionItem>? Options { get; set; } // parent-provided
        public string? HelpText { get; set; }
        // Optional labels for boolean chips
        public string? TrueLabel { get; set; }
        public string? FalseLabel { get; set; }
    }
}
