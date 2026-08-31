using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using tesisproject.frontend.Services.Auth; // asegúrate del namespace real de CustomAuthStateProvider

namespace tesisproject.frontend.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] public CustomAuthStateProvider AuthStateProvider { get; set; } = null!;

        private bool IsSidebarOpen = false;
        private DateTime _now = DateTime.Now;
        private CancellationTokenSource? _clockCancellation;

        private string CurrentPath
        {
            get
            {
                var baseUri = Nav.BaseUri.TrimEnd('/');
                var relative = Nav.Uri.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase)
                    ? Nav.Uri[baseUri.Length..]
                    : Nav.Uri;

                return relative.StartsWith("/") ? relative : "/" + relative;
            }
        }

        private bool IsArticleRoute => CurrentPath.StartsWith("/articles", StringComparison.OrdinalIgnoreCase);

        private string CurrentModuleBadge => IsArticleRoute
            ? "Producción científica"
            : "Gestión de proyectos";

        private string CurrentModuleDescription => IsArticleRoute
            ? "Registro, workflow, reportería e inteligencia científica."
            : "Seguimiento operativo de proyectos de investigación.";

        private string CurrentDateLabel => _now.ToString("dd/MM/yyyy");
        private string CurrentTimeLabel => _now.ToString("HH:mm");

        private static readonly string[] SidebarRoutes =
        [
            "/",
            "/projects",
            "/tailwind-test"
        ];

        private bool ShowSidebar
        {
            get
            {
                var baseUri = Nav.BaseUri.TrimEnd('/');
                var relative = Nav.Uri.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase)
                    ? Nav.Uri[baseUri.Length..]
                    : Nav.Uri;

                if (!relative.StartsWith("/"))
                    relative = "/" + relative;

                return SidebarRoutes.Any(r =>
                    relative.Equals(r, StringComparison.OrdinalIgnoreCase) ||
                    relative.StartsWith(r.EndsWith("/") ? r : r + "/", StringComparison.OrdinalIgnoreCase));
            }
        }

        private async Task HandleLogout()
        {
            await AuthStateProvider.LogoutAsync();
            Nav.NavigateTo("/login", replace: true);
        }

        protected override void OnInitialized()
        {
            Nav.LocationChanged += OnLocationChanged;
            _clockCancellation = new CancellationTokenSource();
            _ = RunClockAsync(_clockCancellation.Token);
        }

        private async Task RunClockAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _now = DateTime.Now;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            IsSidebarOpen = false;
            StateHasChanged();
        }

        private void ToggleSidebar()
        {
            IsSidebarOpen = !IsSidebarOpen;
        }

        public void Dispose()
        {
            Nav.LocationChanged -= OnLocationChanged;
            _clockCancellation?.Cancel();
            _clockCancellation?.Dispose();
        }
    }
}
