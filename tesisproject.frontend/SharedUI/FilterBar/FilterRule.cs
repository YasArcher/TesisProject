namespace tesisproject.frontend.SharedUI.FilterBar
{
    public class FilterRule
    {
        public string Field { get; set; } = string.Empty;  // maps to EntityField
        public string Operator { get; set; } = "eq";       // eq | in | between | contains | gte | lte ...
        public object? Value { get; set; }
    }
}
