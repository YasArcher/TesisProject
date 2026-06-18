using Microsoft.AspNetCore.Components;
namespace tesisproject.frontend.SharedUI.SelectInput
{
    public partial class SelectInput<TItem> : ComponentBase
    {
        private sealed record Option(string Key, string Text, TItem Item);
        private List<Option>? _options;
        private string _selectedKey = string.Empty;

        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        /// <summary>Current selected value (nullable supported).</summary>
        [Parameter] public TItem? Value { get; set; }
        [Parameter] public EventCallback<TItem?> ValueChanged { get; set; }
        /// <summary>Returns the display text for an item (required).</summary>
        [Parameter, EditorRequired] public Func<TItem, string> GetText { get; set; } = default!;
        /// <summary>Returns a unique key for an item (optional). If not provided, GetText(item) is used.</summary>
        [Parameter] public Func<TItem, string>? GetKey { get; set; }
        [Parameter] public string? Placeholder { get; set; } = "Select an option";
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool AllowClear { get; set; } = false;
        [Parameter] public SelectSize Size { get; set; } = SelectSize.Md;

        protected override void OnParametersSet()
        {
            // Build option list
            if (Items is not null)
            {
                _options = Items
                    .Select(i => new Option(KeyOf(i), GetText(i), i))
                    .ToList();
            }
            else
            {
                _options = new List<Option>();
            }

            // Figure out selected key from current Value
            if (Value is null)
            {
                _selectedKey = string.Empty;
            }
            else
            {
                var keyFromValue = KeyOf(Value);
                // If Value might not exist in Items, still keep the key (select will fall back to placeholder if not found)
                _selectedKey = _options.Any(o => o.Key == keyFromValue) ? keyFromValue : string.Empty;
            }
        }

        private string KeyOf(TItem item)
        {
            var k = GetKey?.Invoke(item) ?? GetText(item);
            return k ?? string.Empty;
        }

        private async Task OnChanged(ChangeEventArgs e)
        {
            var newKey = e.Value?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(newKey))
            {
                _selectedKey = string.Empty;
                await ValueChanged.InvokeAsync(default);
                return;
            }

            var match = _options?.FirstOrDefault(o => o.Key == newKey);
            _selectedKey = newKey;
            if (match is not null)
                await ValueChanged.InvokeAsync(match.Item);
            else
                await ValueChanged.InvokeAsync(default);
        }

        private async Task ClearSelection()
        {
            if (Disabled) return;
            _selectedKey = string.Empty;
            await ValueChanged.InvokeAsync(default);
        }

        private string BuildSelectClasses() => Size switch
        {
            SelectSize.Sm => "px-2 py-1 text-sm",
            SelectSize.Md => "px-3 py-2 text-base",
            SelectSize.Lg => "px-4 py-3 text-lg",
            _ => "px-3 py-2 text-base"
        };
    }
}