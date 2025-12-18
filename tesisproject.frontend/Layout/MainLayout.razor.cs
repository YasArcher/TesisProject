using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using tesisproject.frontend.Services.Auth; // asegúrate del namespace real de CustomAuthStateProvider

namespace tesisproject.frontend.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] public CustomAuthStateProvider AuthStateProvider { get; set; } = null!;

        private bool IsSidebarOpen = false;

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
        }
    }
}
