using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Table
{
    public partial class DataTable<TItem> : ComponentBase
    {
        // Data & columns
        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        [Parameter] public IReadOnlyList<ColumnDef<TItem>> Columns { get; set; } = Array.Empty<ColumnDef<TItem>>();

        // Optional row template
        [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }

        // Sorting state
        private int? _sortIndex = null;
        private SortDirection _sortDirection = SortDirection.None;

        // Pager (client-driven por defecto; compatible con server-side si lo usas)
        [Parameter] public int? PageSize { get; set; }
        [Parameter] public int? Total { get; set; } // total rows
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
            // Clamp de página si cambian Total/PageSize desde el padre
            if (ShowPager && CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            RecomputeView();
        }

        private void RecomputeView()
        {
            var src = Items ?? Enumerable.Empty<TItem>();
            var sorted = ApplySorting(src);
            ViewItems = ApplyPaging(sorted);
        }

        private IEnumerable<TItem> ApplySorting(IEnumerable<TItem> source)
        {
            if (_sortIndex is null || _sortDirection == SortDirection.None)
                return source;

            var col = Columns[_sortIndex.Value];

            // Orden ascendente usando IComparable cuando sea posible
            var asc = source.OrderBy(
                item => col.ValueSelector(item),
                new AscObjectComparer()
            );

            return _sortDirection == SortDirection.Asc ? asc : asc.Reverse();
        }

        private IEnumerable<TItem> ApplyPaging(IEnumerable<TItem> source)
        {
            if (!ShowPager) return source;

            var skip = (CurrentPage - 1) * PageSize!.Value;
            return source.Skip(skip).Take(PageSize!.Value);
        }

        private sealed class AscObjectComparer : IComparer<object?>
        {
            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return 1;        // nulls last
                if (y is null) return -1;

                if (x is IComparable cx && (y is null || x.GetType().IsAssignableFrom(y.GetType()) || y.GetType().IsAssignableFrom(x.GetType())))
                {
                    try { return cx.CompareTo(y); }
                    catch { /* fall through */ }
                }

                // Fallback a string ordinal
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

            // Si se cambia el sort, volvemos a la página 1
            if (ShowPager) CurrentPage = 1;

            RecomputeView();
            StateHasChanged();
        }

        protected string GetSortGlyph(int index)
        {
            if (_sortIndex != index || _sortDirection == SortDirection.None) return "↕";
            return _sortDirection == SortDirection.Asc ? "▲" : "▼";
        }

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
                    if (start > 2) pages.Add(-1); // …
                }

                for (var p = start; p <= end; p++) pages.Add(p);

                if (end < TotalPages)
                {
                    if (end < TotalPages - 1) pages.Add(-1);
                    pages.Add(TotalPages);
                }

                return pages;
            }
        }

        private async void ChangePage(int page)
        {
            if (!ShowPager) return;
            page = Math.Min(Math.Max(1, page), TotalPages);
            if (page == CurrentPage) return;

            CurrentPage = page;
            RecomputeView();
            StateHasChanged();

            // Opcional: notifica al padre si lo necesita
            await OnPageChanged.InvokeAsync(page);
        }

        private void PrevPage() => ChangePage(CurrentPage - 1);
        private void NextPage() => ChangePage(CurrentPage + 1);
    }
}
