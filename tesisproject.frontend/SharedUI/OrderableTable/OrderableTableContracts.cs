using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.OrderableTable
{
    public sealed record OrderableColumnDef<TItem>(
        string Title,
        Func<TItem, object?>? ValueSelector = null,
        string? Width = null,
        string? HeaderClass = null,
        string? CellClass = null,
        RenderFragment<TItem>? CellTemplate = null
    );
}