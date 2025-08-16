namespace tesisproject.frontend.SharedUI.Table
{
    public enum SortDirection { None, Asc, Desc }

    public sealed record ColumnDef<TItem>(
        string Title,
        Func<TItem, object?> ValueSelector,
        string? Width = null,
        bool Sortable = true
    );
}