using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Table
{
    public partial class DataTable<TItem> : ComponentBase
    {
        // Data & columns
        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        [Parameter] public IReadOnlyList<ColumnDef<TItem>> Columns { get; set; } = Array.Empty<ColumnDef<TItem>>();

        // Optional row template (when provided, you render <td> cells inside)
        [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }

        // Sorting state
        private int? _sortIndex = null;
        private SortDirection _sortDirection = SortDirection.None;

        // Optional pager (server- or client-driven)
        [Parameter] public int? PageSize { get; set; }
        [Parameter] public int? Total { get; set; } // total rows across all pages (when paging)
        [Parameter] public int CurrentPage { get; set; } = 1; // 1-based
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }

        // UI
        [Parameter] public string EmptyText { get; set; } = "No records found.";

        // Derived
        protected IEnumerable<TItem> ViewItems { get; private set; } = Enumerable.Empty<TItem>();
        private bool ShowPager => PageSize.HasValue && Total.HasValue && PageSize.Value > 0 && Total.Value > 0;
        private int TotalPages => ShowPager ? Math.Max(1, (int)Math.Ceiling((double)Total!.Value / PageSize!.Value)) : 1;

        private bool _isFirstPage => CurrentPage <= 1;
        private bool _isLastPage => CurrentPage >= TotalPages;

        protected override void OnParametersSet()
        {
            // Recompute the view on parameter or sort changes.
            var source = Items ?? Enumerable.Empty<TItem>();
            ViewItems = ApplySorting(source);
        }

        private IEnumerable<TItem> ApplySorting(IEnumerable<TItem> source)
        {
            if (_sortIndex is null || _sortDirection == SortDirection.None)
                return source;

            var idx = _sortIndex.Value;
            if (idx < 0 || idx >= Columns.Count) return source;

            var col = Columns[idx];
            var selector = col.ValueSelector ?? (_ => (object?)null);

            // Usa comparador seguro para object? (nulls al final)
            return _sortDirection == SortDirection.Asc
                ? source.OrderBy(selector, ObjectComparer.Instance)
                : source.OrderByDescending(selector, ObjectComparer.Instance);
        }

        private sealed class ObjectComparer : IComparer<object?>
        {
            public static readonly ObjectComparer Instance = new();
            private ObjectComparer() { }

            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return 1;   // nulls last
                if (y is null) return -1;

                // Si son comparables y "compatibles", compara directamente
                if (x is IComparable cx)
                {
                    var xt = x.GetType();
                    var yt = y.GetType();
                    if (xt.IsAssignableFrom(yt) || yt.IsAssignableFrom(xt))
                    {
                        try { return cx.CompareTo(y); }
                        catch { /* continúa */ }
                    }
                }

                // Fallback: comparación ordinal de strings
                return string.CompareOrdinal(x.ToString(), y.ToString());
            }
        }

        private void OnHeaderClick(int index)
        {
            if (index < 0 || index >= Columns.Count) return;
            if (!Columns[index].Sortable) return;

            if (_sortIndex != index)
            {
                _sortIndex = index;
                _sortDirection = SortDirection.Asc;
            }
            else
            {
                _sortDirection = _sortDirection switch
                {
                    SortDirection.Asc => SortDirection.Desc,
                    SortDirection.Desc => SortDirection.None,
                    _ => SortDirection.Asc
                };
            }

            // Recompute sorted view
            ViewItems = ApplySorting(Items ?? Enumerable.Empty<TItem>());
            StateHasChanged();
        }

        protected string GetSortGlyph(int index)
        {
            if (_sortIndex != index || _sortDirection == SortDirection.None) return "↕";
            return _sortDirection == SortDirection.Asc ? "▲" : "▼";
        }

        // Pager helpers (when used with server-side paging, Items should be the current page slice)
        private IEnumerable<int> VisiblePages
        {
            get
            {
                if (!ShowPager) return Enumerable.Empty<int>();
                const int window = 5;

                if (TotalPages <= window) return Enumerable.Range(1, TotalPages);

                var start = Math.Max(1, CurrentPage - 2);
                var end = Math.Min(TotalPages, CurrentPage + 2);

                var pages = new List<int>();
                if (start > 1)
                {
                    pages.Add(1);
                    if (start > 2) pages.Add(-1); // ellipsis
                }

                for (var p = start; p <= end; p++) pages.Add(p);

                if (end < TotalPages)
                {
                    if (end < TotalPages - 1) pages.Add(-1); // ellipsis
                    pages.Add(TotalPages);
                }

                return pages;
            }
        }

        private async Task ChangePage(int page)
        {
            if (!ShowPager) return;
            page = Math.Min(Math.Max(1, page), TotalPages);
            if (page == CurrentPage) return;

            await OnPageChanged.InvokeAsync(page);
        }

        private Task PrevPage() => ChangePage(CurrentPage - 1);
        private Task NextPage() => ChangePage(CurrentPage + 1);
    }
}
