// SideBar.razor.cs
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.SharedUI;

public partial class SideBar
{
    private record MenuItem(
        string Text,
        string? Href = null,
        bool Exact = false,
        string? Section = null,
        string? Icon = null,
        IReadOnlyList<MenuItem>? Children = null
    )
    {
        public bool HasChildren => Children is { Count: > 0 };
    }

    [Inject] private IDwEtlClientService DwEtlClientService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _etlBusy;
    private string? _etlStatus;
    private string? _etlError;

    private readonly HashSet<string> _expanded = new();

    private void Toggle(string key)
    {
        if (!_expanded.Add(key))
            _expanded.Remove(key);
    }

    private bool IsExpanded(string key) => _expanded.Contains(key);

    private bool IsChildActive(MenuItem parent)
    {
        // BaseRelativePath devuelve algo como "visits/planner"
        var rel = Nav.ToBaseRelativePath(Nav.Uri);
        var path = "/" + rel;

        if (string.IsNullOrWhiteSpace(rel))
            path = "/";

        return parent.Children?.Any(c =>
            !string.IsNullOrWhiteSpace(c.Href) &&
            path.StartsWith(c.Href!, StringComparison.OrdinalIgnoreCase)
        ) == true;
    }

    private readonly MenuItem[] _items =
    [
        new MenuItem("Proyectos", "/projects/home", false, null,
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""w-5 h-5"">
                <path fill-rule=""evenodd"" d=""M2.25 13.5a8.25 8.25 0 0 1 8.25-8.25.75.75 0 0 1 .75.75v6.75H18a.75.75 0 0 1 .75.75 8.25 8.25 0 0 1-16.5 0Z"" clip-rule=""evenodd"" />
                <path fill-rule=""evenodd"" d=""M12.75 3a.75.75 0 0 1 .75-.75 8.25 8.25 0 0 1 8.25 8.25.75.75 0 0 1-.75.75h-7.5a.75.75 0 0 1-.75-.75V3Z"" clip-rule=""evenodd"" />
              </svg>"
        ),

        new MenuItem("Grupos", "/groups", false, null,
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""w-5 h-5"">
                <path d=""M4.5 6.375a4.125 4.125 0 1 1 8.25 0 4.125 4.125 0 0 1-8.25 0ZM14.25 8.625a3.375 3.375 0 1 1 6.75 0 3.375 3.375 0 0 1-6.75 0ZM1.5 19.125a7.125 7.125 0 0 1 14.25 0v.003l-.001.119a.75.75 0 0 1-.363.63 13.067 13.067 0 0 1-6.761 1.873c-2.472 0-4.786-.684-6.76-1.873a.75.75 0 0 1-.364-.63l-.001-.122ZM17.25 19.128l-.001.144a2.25 2.25 0 0 1-.233.96 10.088 10.088 0 0 0 5.06-1.01.75.75 0 0 0 .42-.643 4.875 4.875 0 0 0-6.957-4.611 8.586 8.586 0 0 1 1.71 5.157v.003Z"" />
              </svg>"
        ),

        new MenuItem("Configuraciones", "/settings", false, null,
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""size-6"">
              <path fill-rule=""evenodd"" d=""M11.078 2.25c-.917 0-1.699.663-1.85 1.567L9.05 4.889c-.02.12-.115.26-.297.348a7.493 7.493 0 0 0-.986.57c-.166.115-.334.126-.45.083L6.3 5.508a1.875 1.875 0 0 0-2.282.819l-.922 1.597a1.875 1.875 0 0 0 .432 2.385l.84.692c.095.078.17.229.154.43a7.598 7.598 0 0 0 0 1.139c.015.2-.059.352-.153.43l-.841.692a1.875 1.875 0 0 0-.432 2.385l.922 1.597a1.875 1.875 0 0 0 2.282.818l1.019-.382c.115-.043.283-.031.45.082.312.214.641.405.985.57.182.088.277.228.297.35l.178 1.071c.151.904.933 1.567 1.85 1.567h1.844c.916 0 1.699-.663 1.85-1.567l.178-1.072c.02-.12.114-.26.297-.349.344-.165.673-.356.985-.57.167-.114.335-.125.45-.082l1.02.382a1.875 1.875 0 0 0 2.28-.819l.923-1.597a1.875 1.875 0 0 0-.432-2.385l-.84-.692c-.095-.078-.17-.229-.154-.43a7.614 7.614 0 0 0 0-1.139c-.016-.2.059-.352.153-.43l.84-.692c.708-.582.891-1.59.433-2.385l-.922-1.597a1.875 1.875 0 0 0-2.282-.818l-1.02.382c-.114.043-.282.031-.449-.083a7.49 7.49 0 0 0-.985-.57c-.183-.087-.277-.227-.297-.348l-.179-1.072a1.875 1.875 0 0 0-1.85-1.567h-1.843ZM12 15.75a3.75 3.75 0 1 0 0-7.5 3.75 3.75 0 0 0 0 7.5Z"" clip-rule=""evenodd"" />
            </svg>"
        ),

        new MenuItem("Matrices", "/matrix", false, null,
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""size-6"">
              <path fill-rule=""evenodd"" d=""M5.625 1.5c-1.036 0-1.875.84-1.875 1.875v17.25c0 1.035.84 1.875 1.875 1.875h12.75c1.035 0 1.875-.84 1.875-1.875V12.75A3.75 3.75 0 0 0 16.5 9h-1.875a1.875 1.875 0 0 1-1.875-1.875V5.25A3.75 3.75 0 0 0 9 1.5H5.625ZM7.5 15a.75.75 0 0 1 .75-.75h7.5a.75.75 0 0 1 0 1.5h-7.5A.75.75 0 0 1 7.5 15Zm.75 2.25a.75.75 0 0 0 0 1.5H12a.75.75 0 0 0 0-1.5H8.25Z"" clip-rule=""evenodd"" />
              <path d=""M12.971 1.816A5.23 5.23 0 0 1 14.25 5.25v1.875c0 .207.168.375.375.375H16.5a5.23 5.23 0 0 1 3.434 1.279 9.768 9.768 0 0 0-6.963-6.963Z"" />
            </svg>"
        ),

        // Padre jerárquico
        new MenuItem(
            Text: "Visitas",
            Href: null,
            Icon: @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""size-6"">
                      <path d=""M12.75 12.75a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0ZM7.5 15.75a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5ZM8.25 17.25a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0ZM9.75 15.75a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5ZM10.5 17.25a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0ZM12 15.75a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5ZM12.75 17.25a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0ZM14.25 15.75a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5ZM15 17.25a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0ZM16.5 15.75a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5ZM15 12.75a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0ZM16.5 13.5a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z"" />
                      <path fill-rule=""evenodd"" d=""M6.75 2.25A.75.75 0 0 1 7.5 3v1.5h9V3A.75.75 0 0 1 18 3v1.5h.75a3 3 0 0 1 3 3v11.25a3 3 0 0 1-3 3H5.25a3 3 0 0 1-3-3V7.5a3 3 0 0 1 3-3H6V3a.75.75 0 0 1 .75-.75Zm13.5 9a1.5 1.5 0 0 0-1.5-1.5H5.25a1.5 1.5 0 0 0-1.5 1.5v7.5a1.5 1.5 0 0 0 1.5 1.5h13.5a1.5 1.5 0 0 0 1.5-1.5v-7.5Z"" clip-rule=""evenodd"" />
                    </svg>",
            Children:
            [
                new MenuItem("Planificador", "/visits/planner/plan", false, null,
                    @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""w-5 h-5"">
                        <path d=""M6.75 2.25A.75.75 0 0 1 7.5 3v1.5h9V3A.75.75 0 0 1 18 3v1.5h.75a3 3 0 0 1 3 3v11.25a3 3 0 0 1-3 3H5.25a3 3 0 0 1-3-3V7.5a3 3 0 0 1 3-3H6V3a.75.75 0 0 1 .75-.75Z""/>
                      </svg>"
                ),
                new MenuItem("Editor de planificados", "/visits/planner/pending", false, null,
                    @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""w-5 h-5"">
                        <path d=""M16.862 3.487a2.25 2.25 0 0 1 3.182 3.182l-9.19 9.19a4.5 4.5 0 0 1-1.897 1.13l-3.01 1.003a.75.75 0 0 1-.949-.949l1.003-3.01a4.5 4.5 0 0 1 1.13-1.897l9.73-9.65Z""/>
                        <path d=""M19.5 10.5v8.25A2.25 2.25 0 0 1 17.25 21H5.25A2.25 2.25 0 0 1 3 18.75V6.75A2.25 2.25 0 0 1 5.25 4.5H12""/>
                      </svg>"
                ),
            ]
        ),
    ];

    private IEnumerable<IGrouping<string?, MenuItem>> GroupedItems =>
        _items.GroupBy(i => i.Section);

    private async Task RunDwEtlAsync()
    {
        if (_etlBusy) return;

        _etlBusy = true;
        StateHasChanged();

        Toast.ShowInfo("Actualizando la información para Power BI...");

        try
        {
            var result = await DwEtlClientService.RunFullAsync();

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                Toast.ShowError(result.Error ?? "No se pudo completar la actualización.");
                return;
            }

            Toast.ShowSuccess("Actualización completada. Power BI ya tiene los datos más recientes.");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Ocurrió un problema al actualizar: {ex.Message}");
        }
        finally
        {
            _etlBusy = false;
            StateHasChanged();
        }
    }
}