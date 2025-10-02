using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace tesisproject.frontend.Layout
{
    public partial class MainLayout : IDisposable
    {
        private bool IsSidebarOpen = false;

        private static readonly string[] SidebarRoutes =
        [
            "/",
            "/projects",
            "/tailwind-test"
        ];

        // Sidebar visible solo en rutas incluidas
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

        protected override void OnInitialized()
        {
            Nav.LocationChanged += OnLocationChanged;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            // Cuando cambia de ruta, cierra el sidebar móvil
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
