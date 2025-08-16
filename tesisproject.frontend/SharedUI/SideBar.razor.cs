using System.Linq;
using System.Collections.Generic;

namespace tesisproject.frontend.SharedUI;
    public partial class SideBar
    {
        private bool collapseNavMenu = false;
        string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;
        void ToggleNavMenu() => collapseNavMenu = !collapseNavMenu;

        private record MenuItem(string Text, string Href, bool Exact = false, string? Section = null);

        private readonly MenuItem[] _items = new[]
        {
            new MenuItem("Home", "/", true),
            //new MenuItem("TailwindTest", "/tailwind-test", Section: "TailwindTest"), si se pone seaction "TailwindTest" se agrupa en esa sección
            new MenuItem("TailwindTest", "/tailwind-test"),
            new MenuItem("Projects", "/projects"),
            new MenuItem("TestUI", "/testui")
        };

        IEnumerable<IGrouping<string?, MenuItem>> GroupedItems
            => _items.GroupBy(i => i.Section);
    }
