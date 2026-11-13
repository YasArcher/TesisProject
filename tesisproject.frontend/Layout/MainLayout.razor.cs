using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using tesisproject.frontend.Services.Auth;

namespace tesisproject.frontend.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] public CustomAuthStateProvider AuthStateProvider { get; set; } = null!;

        private bool IsSidebarOpen = false;
        private bool _isSidebarCollapsed;
        private bool _portalSidebarExpanded;
        private DateTime _now = DateTime.Now;
        private CancellationTokenSource? _clockCancellation;

        private bool IsPortalRoute => Nav.ToBaseRelativePath(Nav.Uri).Split('?', '#')[0].Trim('/') == string.Empty;
        private bool SidebarCollapsed => IsPortalRoute ? !_portalSidebarExpanded : _isSidebarCollapsed;

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
            if (IsPortalRoute)
                _portalSidebarExpanded = false;
            StateHasChanged();
        }

        private void ToggleSidebar()
        {
            IsSidebarOpen = !IsSidebarOpen;
        }

        private void ToggleSidebarCollapsed()
        {
            if (IsPortalRoute)
                _portalSidebarExpanded = !_portalSidebarExpanded;
            else
                _isSidebarCollapsed = !_isSidebarCollapsed;
        }

        public void Dispose()
        {
            Nav.LocationChanged -= OnLocationChanged;
            _clockCancellation?.Cancel();
            _clockCancellation?.Dispose();
        }
    }
}
