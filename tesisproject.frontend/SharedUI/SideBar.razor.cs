// SideBar.razor.cs
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.SharedUI;

public partial class SideBar : IDisposable
{
    private record MenuItem(
        string Text,
        string? Href = null,
        bool Exact = false,
        string? Section = null,
        string? Icon = null,
        IReadOnlyList<MenuItem>? Children = null,
        IReadOnlyCollection<string>? AllowedRoles = null,
        bool RequiresArticlesAccess = false
    )
    {
        public bool HasChildren => Children is { Count: > 0 };
    }

    private const string IconDashboard = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path d=""M3 3h7.5v7.5H3V3Zm10.5 0H21v7.5h-7.5V3ZM3 13.5h7.5V21H3v-7.5Zm10.5 0H21V21h-7.5v-7.5Z"" /></svg>";
    private const string IconProjects = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path d=""M2.25 12.75A6.75 6.75 0 0 1 9 6h6a6.75 6.75 0 0 1 0 13.5H9a6.75 6.75 0 0 1-6.75-6.75Z""/><path d=""M8.25 4.5A2.25 2.25 0 0 1 10.5 2.25h3A2.25 2.25 0 0 1 15.75 4.5V6h-1.5V4.5a.75.75 0 0 0-.75-.75h-3a.75.75 0 0 0-.75.75V6h-1.5V4.5Z""/></svg>";
    private const string IconArticles = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path fill-rule=""evenodd"" d=""M5.625 1.5c-1.036 0-1.875.84-1.875 1.875v17.25c0 1.035.84 1.875 1.875 1.875h12.75c1.035 0 1.875-.84 1.875-1.875V12.75A3.75 3.75 0 0 0 16.5 9h-1.875a1.875 1.875 0 0 1-1.875-1.875V5.25A3.75 3.75 0 0 0 9 1.5H5.625Zm2.625 12.75a.75.75 0 0 0 0 1.5h7.5a.75.75 0 0 0 0-1.5h-7.5Zm0 3a.75.75 0 0 0 0 1.5H12a.75.75 0 0 0 0-1.5H8.25Z"" clip-rule=""evenodd"" /></svg>";
    private const string IconSettings = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path fill-rule=""evenodd"" d=""M11.078 2.25c-.917 0-1.699.663-1.85 1.567l-.178 1.072c-.02.12-.115.26-.297.348-.344.165-.673.356-.986.57-.166.115-.334.126-.45.083L6.3 5.508a1.875 1.875 0 0 0-2.282.819l-.922 1.597a1.875 1.875 0 0 0 .432 2.385l.84.692c.095.078.17.229.154.43a7.598 7.598 0 0 0 0 1.139c.015.2-.059.352-.153.43l-.841.692a1.875 1.875 0 0 0-.432 2.385l.922 1.597a1.875 1.875 0 0 0 2.282.818l1.019-.382c.115-.043.283-.031.45.082.312.214.641.405.985.57.182.088.277.228.297.35l.178 1.071c.151.904.933 1.567 1.85 1.567h1.844c.916 0 1.699-.663 1.85-1.567l.178-1.072c.02-.12.114-.26.297-.349.344-.165.673-.356.985-.57.167-.114.335-.125.45-.082l1.02.382a1.875 1.875 0 0 0 2.28-.819l.923-1.597a1.875 1.875 0 0 0-.432-2.385l-.84-.692c-.095-.078-.17-.229-.154-.43a7.614 7.614 0 0 0 0-1.139c-.016-.2.059-.352.153-.43l.84-.692c.708-.582.891-1.59.433-2.385l-.922-1.597a1.875 1.875 0 0 0-2.282-.818l-1.02.382c-.114.043-.282.031-.449-.083a7.49 7.49 0 0 0-.985-.57c-.183-.087-.277-.227-.297-.348l-.179-1.072a1.875 1.875 0 0 0-1.85-1.567h-1.843ZM12 15.75a3.75 3.75 0 1 0 0-7.5 3.75 3.75 0 0 0 0 7.5Z"" clip-rule=""evenodd"" /></svg>";
    private const string IconCalendar = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path fill-rule=""evenodd"" d=""M6.75 2.25A.75.75 0 0 1 7.5 3v1.5h9V3A.75.75 0 0 1 18 3v1.5h.75a3 3 0 0 1 3 3v11.25a3 3 0 0 1-3 3H5.25a3 3 0 0 1-3-3V7.5a3 3 0 0 1 3-3H6V3a.75.75 0 0 1 .75-.75Zm13.5 9a1.5 1.5 0 0 0-1.5-1.5H5.25a1.5 1.5 0 0 0-1.5 1.5v7.5a1.5 1.5 0 0 0 1.5 1.5h13.5a1.5 1.5 0 0 0 1.5-1.5v-7.5Z"" clip-rule=""evenodd"" /></svg>";
    private const string IconImport = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path d=""M12 3.75a.75.75 0 0 1 .75.75v8.69l2.47-2.47a.75.75 0 1 1 1.06 1.06l-3.75 3.75a.75.75 0 0 1-1.06 0l-3.75-3.75a.75.75 0 1 1 1.06-1.06l2.47 2.47V4.5A.75.75 0 0 1 12 3.75Z""/><path d=""M4.5 15.75a.75.75 0 0 1 .75.75v1.875c0 .621.504 1.125 1.125 1.125h11.25c.621 0 1.125-.504 1.125-1.125V16.5a.75.75 0 0 1 1.5 0v1.875A2.625 2.625 0 0 1 17.625 21H6.375A2.625 2.625 0 0 1 3.75 18.375V16.5a.75.75 0 0 1 .75-.75Z""/></svg>";
    private const string IconChart = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path d=""M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125C16.5 3.504 17.004 3 17.625 3h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z""/></svg>";
    private const string IconHelp = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path fill-rule=""evenodd"" d=""M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12Zm11.378-3.917c-.89-.777-2.366-.777-3.255 0a.75.75 0 0 1-.988-1.129c1.454-1.272 3.776-1.272 5.23 0 1.513 1.324 1.513 3.518 0 4.842-.292.255-.61.47-.928.65-.314.178-.565.318-.734.48-.14.134-.203.256-.203.574a.75.75 0 0 1-1.5 0c0-.695.216-1.19.667-1.622.39-.375.86-.637 1.235-.847.264-.148.488-.272.674-.435.82-.717.82-1.796 0-2.513ZM12 17.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z"" clip-rule=""evenodd"" /></svg>";

    [Inject] private IDwEtlClientService DwEtlClientService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    // [ARTICLES-MIGRATION] La sesion unificada determina la navegacion visible.
    [Inject] private IAuthClientService AuthClient { get; set; } = default!;

    private bool _etlBusy;
    private string? _etlStatus;
    private string? _etlError;
    private HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
    private bool _articlesAccess;
    private bool _projectsAccess;
    private bool _sessionLoaded;

    private readonly HashSet<string> _expanded = new();

    private void Toggle(string key)
    {
        if (!_expanded.Add(key))
            _expanded.Remove(key);
    }

    private bool IsExpanded(string key) => _expanded.Contains(key);

    private bool IsChildActive(MenuItem parent)
    {
        var path = CurrentPath;

        return parent.Children?.Any(c =>
            !string.IsNullOrWhiteSpace(c.Href) &&
            path.StartsWith(c.Href!, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private string CurrentPath
    {
        get
        {
            var rel = Nav.ToBaseRelativePath(Nav.Uri);
            return string.IsNullOrWhiteSpace(rel) ? "/" : "/" + rel;
        }
    }

    private readonly MenuItem[] _projectItems =
    [
        new MenuItem("Módulos", "/", true, "Plataforma", IconDashboard),
        new MenuItem("Artículos científicos", "/articles", false, "Plataforma", IconArticles, RequiresArticlesAccess: true),
        new MenuItem(
            Text: "Gestión de proyectos",
            Section: "Proyectos",
            Icon: IconProjects,
            Children:
            [
                new MenuItem("Panel de proyectos", "/projects/home"),
                new MenuItem("Matrices", "/matrix")
            ],
            AllowedRoles: ["coordinador", "technical", "admin", "superadmin"]),
        new MenuItem(
            Text: "Seguimiento de visitas",
            Section: "Proyectos",
            Icon: IconCalendar,
            Children:
            [
                new MenuItem("Planificador", "/visits/planner/plan"),
                new MenuItem("Editor de planificados", "/visits/planner/pending")
            ],
            AllowedRoles: ["superadmin"]),
        new MenuItem(
            Text: "Administración",
            Section: "Configuración",
            Icon: IconSettings,
            Children:
            [
                new MenuItem("Configuraciones", "/settings"),
                new MenuItem("Grupos", "/groups")
            ],
            AllowedRoles: ["superadmin"])
    ];

    // [ARTICLES-MIGRATION] Mapa ordenado del sistema de produccion cientifica dentro de la fusion.
    private readonly MenuItem[] _articleItems =
    [
        new MenuItem("Portal de artículos", "/articles", true, "Inicio", IconDashboard),
        new MenuItem(
            Text: "Registro y seguimiento",
            Section: "Operación",
            Icon: IconArticles,
            Children:
            [
                new MenuItem("Listado de artículos", "/articles", true),
                new MenuItem("Registrar artículo", "/articles/module/register"),
                new MenuItem("Revisión de envíos", "/articles/module/workflow")
            ]),
        new MenuItem(
            Text: "Captura e importación",
            Section: "Operación",
            Icon: IconImport,
            Children:
            [
                new MenuItem("Matriz de registro", "/articles/module/registration-matrix"),
                new MenuItem("Carga masiva", "/articles/module/bulk-import"),
                new MenuItem("APIs externas", "/articles/module/external-apis"),
                new MenuItem("Ingesta externa", "/articles/module/external-ingestion")
            ]),
        new MenuItem(
            Text: "Análisis institucional",
            Section: "Inteligencia",
            Icon: IconChart,
            Children:
            [
                new MenuItem("Reportería", "/articles/module/reporting"),
                new MenuItem("Inteligencia Artificial", "/articles/module/intelligence")
            ]),
        new MenuItem(
            Text: "Configuración del módulo",
            Section: "Administración",
            Icon: IconSettings,
            Children:
            [
                new MenuItem("Formularios y campos", "/articles/module/configuration"),
                new MenuItem("Usuarios y roles", "/articles/module/security")
            ]),
        new MenuItem(
            Text: "Soporte y cuenta",
            Section: "Cuenta",
            Icon: IconHelp,
            Children:
            [
                new MenuItem("Centro de ayuda", "/articles/module/support"),
                new MenuItem("Manual de usuario", "/articles/module/manual"),
                new MenuItem("Perfil", "/articles/module/profile")
            ])
    ];

    private IEnumerable<IGrouping<string?, MenuItem>> GroupedItems =>
        ActiveItems
            .Where(IsVisible)
            .GroupBy(i => i.Section);

    private IEnumerable<MenuItem> ActiveItems
        => IsArticlePortal ? _articleItems : _projectItems;

    private bool IsArticlePortal
    {
        get
        {
            var path = CurrentPath;
            return path.StartsWith("/articles", StringComparison.OrdinalIgnoreCase) ||
                   (_sessionLoaded && _articlesAccess && !_projectsAccess);
        }
    }

    private bool CanUsePowerBi => !IsArticlePortal && _roles.Contains("superadmin");

    protected override async Task OnInitializedAsync()
    {
        Nav.LocationChanged += HandleLocationChanged;
        var session = await AuthClient.GetCurrentSessionAsync();
        if (!session.Success || session.Data is null)
            return;

        _roles = session.Data.Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _articlesAccess = session.Data.Articles.Enabled &&
            (session.Data.Articles.CanAccess || session.Data.Articles.CanList);
        _projectsAccess = _roles.Overlaps(["coordinador", "technical", "admin", "superadmin"]);
        _sessionLoaded = true;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Nav.LocationChanged -= HandleLocationChanged;
    }

    private bool IsVisible(MenuItem item)
    {
        if (IsArticlePortal)
            return true;

        if (item.RequiresArticlesAccess && !_articlesAccess)
            return false;

        return item.AllowedRoles is null ||
               item.AllowedRoles.Count == 0 ||
               item.AllowedRoles.Any(_roles.Contains);
    }

    private async Task RunDwEtlAsync()
    {
        if (_etlBusy) return;

        _etlBusy = true;
        StateHasChanged();

        Toast.ShowInfo("Actualizando la información para Power BI...");

        try
        {
            var result = await DwEtlClientService.RunFullAsync();

            if (!string.IsNullOrWhiteSpace(result.ErrorCode))
            {
                Toast.ShowError(result.Message?? "No se pudo completar la actualización.");
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
