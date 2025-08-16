using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace tesisproject.frontend.SharedUI.Tabs
{
    public partial class Tabs : ComponentBase
    {
        [Parameter] public string? ActiveKey { get; set; }
        [Parameter] public EventCallback<string> OnChange { get; set; }
        [Parameter] public TabVariant Variant { get; set; } = TabVariant.Underline;
        [Parameter] public RenderFragment? ChildContent { get; set; }
        internal class TabItem
        {
            public required string Key { get; set; }
            public required string Title { get; set; }
            public required RenderFragment Content { get; set; }
        }

        private readonly List<TabItem> _items = new();
        private readonly Dictionary<string, ElementReference> _tabRefs = new();

        private TabItem? _activeItem => _items.FirstOrDefault(i => i.Key == ActiveKey)
            ?? _items.FirstOrDefault();

        protected override void OnAfterRender(bool firstRender)
        {
            // Keep refs array length in sync with items
            while (_tabRefs.Count < _items.Count)
            {
                var item = _items[_tabRefs.Count];
                _tabRefs.TryAdd(item.Key, default);
            }
            while (_tabRefs.Count > _items.Count)
            {
                // Remove the last tab reference by key
                var keyToRemove = _items.Count < _tabRefs.Count
                    ? _tabRefs.Keys.ElementAt(_tabRefs.Count - 1)
                    : null;
                if (keyToRemove != null)
                    _tabRefs.Remove(keyToRemove);
            }
        }

        internal void Register(TabItem item)
        {
            // Prevent duplicates by key
            if (_items.Any(i => i.Key == item.Key))
                throw new InvalidOperationException($"Duplicate Tab Key detected: '{item.Key}'");

            _items.Add(item);
            StateHasChanged();

            if (string.IsNullOrWhiteSpace(ActiveKey))
            {
                ActiveKey = item.Key;
            }
        }

        private async Task Activate(string key)
        {
            if (ActiveKey == key) return;
            ActiveKey = key;
            if (OnChange.HasDelegate)
                await OnChange.InvokeAsync(key);
            StateHasChanged();
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (_items.Count == 0) return;
            var currentIndex = Math.Max(0, _items.FindIndex(i => i.Key == ActiveKey));

            if (e.Key == "ArrowRight")
            {
                var next = (currentIndex + 1) % _items.Count;
                await ActivateAndFocus(next);
            }
            else if (e.Key == "ArrowLeft")
            {
                var prev = (currentIndex - 1 + _items.Count) % _items.Count;
                await ActivateAndFocus(prev);
            }
        }

        private async Task ActivateAndFocus(int index)
        {
            var key = _items[index].Key;
            await Activate(key);
            // Focus the corresponding button if available
            if (index >= 0 && index < _items.Count)
            {
                var tabKey = _items[index].Key;
                if (_tabRefs.TryGetValue(tabKey, out var tabRef))
                {
                    try { await tabRef.FocusAsync(); } catch { /* ignore */ }
                }
            }
        }
    }
}
