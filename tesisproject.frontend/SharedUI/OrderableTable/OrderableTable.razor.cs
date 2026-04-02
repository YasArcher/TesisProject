using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace tesisproject.frontend.SharedUI.OrderableTable
{
    public partial class OrderableTable<TItem> : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [Parameter, EditorRequired] public IList<TItem> Items { get; set; } = new List<TItem>();
        [Parameter, EditorRequired] public IReadOnlyList<OrderableColumnDef<TItem>> Columns { get; set; } = Array.Empty<OrderableColumnDef<TItem>>();
        [Parameter, EditorRequired] public Func<TItem, string> GetKey { get; set; } = default!;
        [Parameter, EditorRequired] public Func<TItem, int> GetOrder { get; set; } = default!;
        [Parameter, EditorRequired] public Action<TItem, int> SetOrder { get; set; } = default!;

        [Parameter] public EventCallback<IReadOnlyList<TItem>> OnOrderChanged { get; set; }

        [Parameter] public string EmptyText { get; set; } = "No hay registros.";
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public Func<TItem, bool>? CanDragItem { get; set; }

        [Parameter] public string TableClass { get; set; } = "min-w-full divide-y divide-border border-separate border-spacing-0";
        [Parameter] public string HeaderClass { get; set; } = "bg-primary-subtle";

        // Igual que TableBase
        [Parameter]
        public string HeaderCellClass { get; set; } = "px-4 py-3 text-left text-xs font-semibold text-primary uppercase tracking-wider select-none bg-primary-subtle sticky top-0 z-20";

        [Parameter] public string BodyClass { get; set; } = "bg-background divide-y divide-border";
        [Parameter] public string RowClass { get; set; } = "odd:bg-primary-subtle hover:bg-primary-subtle-hover transition-colors duration-150";
        [Parameter] public string BodyCellClass { get; set; } = "px-4 py-3 text-sm text-foreground align-middle";
        [Parameter] public string EmptyCellClass { get; set; } = "px-4 py-4 text-sm text-muted text-center align-middle";

        [Parameter] public string HandleTitle { get; set; } = "Arrastrar para reordenar";
        [Parameter] public string HandleHeaderCellClass { get; set; } = "w-12 px-4 py-3 bg-primary-subtle sticky top-0 z-20";
        [Parameter] public string HandleCellClass { get; set; } = "w-12 px-4 py-3 text-center align-middle";

        [Parameter]
        public string HandleWrapperClass { get; set; } =
            "inline-flex cursor-grab active:cursor-grabbing text-muted hover:text-primary transition-colors select-none";

        [Parameter]
        public string HandleWrapperDisabledClass { get; set; } =
            "inline-flex text-muted opacity-30 cursor-not-allowed select-none";

        private List<TItem> _viewItems = new();
        private readonly string _tbodyId = $"sortable-{Guid.NewGuid():N}";
        private DotNetObjectReference<OrderableTable<TItem>>? _dotNetRef;
        private bool _sortableInitialized;

        private int ResolvedColumnCount => Math.Max(1, Columns.Count + 1);

        protected override void OnParametersSet()
        {
            _viewItems = (Items ?? Array.Empty<TItem>())
                .OrderBy(GetOrder)
                .ToList();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
            }

            if (!Disabled && _viewItems.Count > 0)
            {
                await JS.InvokeVoidAsync("OrderableTable.init", _tbodyId, _dotNetRef);
                _sortableInitialized = true;
            }
            else if (_sortableInitialized)
            {
                await JS.InvokeVoidAsync("OrderableTable.destroy", _tbodyId);
                _sortableInitialized = false;
            }
        }

        [JSInvokable]
        public async Task OnSortEnd(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex) return;
            if (oldIndex < 0 || newIndex < 0) return;
            if (oldIndex >= _viewItems.Count || newIndex >= _viewItems.Count) return;

            var movedItem = _viewItems[oldIndex];
            _viewItems.RemoveAt(oldIndex);
            _viewItems.Insert(newIndex, movedItem);

            ResequenceOrderValues();
            SyncOriginalItems();

            await OnOrderChanged.InvokeAsync(_viewItems.AsReadOnly());
            StateHasChanged();
        }

        private bool CanDrag(TItem item)
        {
            if (Disabled) return false;
            return CanDragItem?.Invoke(item) ?? true;
        }

        private void ResequenceOrderValues()
        {
            for (var i = 0; i < _viewItems.Count; i++)
            {
                SetOrder(_viewItems[i], i + 1);
            }
        }

        private void SyncOriginalItems()
        {
            if (Items is null || Items.IsReadOnly) return;

            Items.Clear();

            foreach (var item in _viewItems)
            {
                Items.Add(item);
            }
        }

        private string BuildHeaderCellClass(OrderableColumnDef<TItem> col)
            => string.IsNullOrWhiteSpace(col.HeaderClass)
                ? HeaderCellClass
                : $"{HeaderCellClass} {col.HeaderClass}";

        private string BuildBodyCellClass(OrderableColumnDef<TItem> col)
            => string.IsNullOrWhiteSpace(col.CellClass)
                ? BodyCellClass
                : $"{BodyCellClass} {col.CellClass}";

        private static string? GetColumnStyle(OrderableColumnDef<TItem> col)
        {
            return string.IsNullOrWhiteSpace(col.Width)
                ? null
                : $"width:{col.Width}";
        }

        public async ValueTask DisposeAsync()
        {
            if (_sortableInitialized)
            {
                await JS.InvokeVoidAsync("OrderableTable.destroy", _tbodyId);
            }

            _dotNetRef?.Dispose();
        }
    }
}
