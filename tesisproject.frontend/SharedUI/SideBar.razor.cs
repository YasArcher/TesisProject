using System.Linq;
using System.Collections.Generic;

namespace tesisproject.frontend.SharedUI;

public partial class SideBar
{
    // Estado del menú (mobile)
    private bool _open = false;
    private string MenuCss => _open ? "block" : "hidden";
    private void ToggleNavMenu() => _open = !_open;

    private record MenuItem(string Text, string Href, bool Exact = false, string? Section = null);

    private readonly MenuItem[] _items = new[]
    {
        new MenuItem("Home", "/", true),
        new MenuItem("TailwindTest", "/tailwind-test"),
        new MenuItem("Projects", "/projects"),
        new MenuItem("TestUI", "/testui"),
        new MenuItem("Grupos", "/grupos"),
    };

    IEnumerable<IGrouping<string?, MenuItem>> GroupedItems => _items.GroupBy(i => i.Section);
}
