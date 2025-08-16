using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.Layout
{
    public partial class MainLayout
    {
        private static readonly string[] SidebarRoutes =
        [
            "/",             // home
            "/projects",     // sección de proyectos
            "/tailwind-test" // demo
        ];

        private bool ShowSidebar
        {
            get
            {
                var baseUri = Nav.BaseUri.TrimEnd('/');
                var relative = Nav.Uri.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase)
                    ? Nav.Uri[baseUri.Length..]
                    : Nav.Uri;

                // Asegura que empiece con "/"
                if (!relative.StartsWith("/"))
                    relative = "/" + relative;

                // Coincidencia por prefijo de segmento
                return SidebarRoutes.Any(r =>
                    relative.Equals(r, StringComparison.OrdinalIgnoreCase) ||
                    relative.StartsWith(r.EndsWith("/") ? r : r + "/", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
