using System.Linq;
using System.Collections.Generic;
namespace tesisproject.frontend.Layout
{
    public partial class NavMenu
    {
        private bool collapseNavMenu = true;
        string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;
        void ToggleNavMenu() => collapseNavMenu = !collapseNavMenu;

        private record MenuItem(string Text, string Href, bool Exact = false, string? Section = null);

        private readonly MenuItem[] _items = new[]
        {
            new MenuItem("Home", "/", true),
            new MenuItem("Orders", "/orders", Section: "Management"),
            new MenuItem("Customers", "/customers", Section: "Management"),
            new MenuItem("Reports", "/reports", Section: "Analytics"),
            new MenuItem("Settings", "/settings", Section: "Admin"),
            new MenuItem("Users", "/users", Section: "Admin"),
        };

        IEnumerable<IGrouping<string?, MenuItem>> GroupedItems
            => _items.GroupBy(i => i.Section);
    }
}
