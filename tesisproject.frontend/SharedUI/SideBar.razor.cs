using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI;

public partial class SideBar
{
    private record MenuItem(string Text, string Href, bool Exact = false, string? Section = null, string? Icon = null);

    private readonly MenuItem[] _items =
    [
        //new MenuItem("Home", "/", true, null,
        //    @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""w-5 h-5"">
        //        <path d=""M11.47 3.841a.75.75 0 0 1 1.06 0l8.69 8.69a.75.75 0 1 0 1.06-1.061l-8.689-8.69a2.25 2.25 0 0 0-3.182 0l-8.69 8.69a.75.75 0 1 0 1.061 1.06l8.69-8.689Z"" />
        //        <path d=""m12 5.432 8.159 8.159c.03.03.06.058.091.086v6.198c0 1.035-.84 1.875-1.875 1.875H15a.75.75 0 0 1-.75-.75v-4.5a.75.75 0 0 0-.75-.75h-3a.75.75 0 0 0-.75.75V21a.75.75 0 0 1-.75.75H5.625a1.875 1.875 0 0 1-1.875-1.875v-6.198a2.29 2.29 0 0 0 .091-.086L12 5.432Z"" />
        //      </svg>"
        //),

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

        new MenuItem("TailwindTest", "/tailwind-test", false, "Desarrollo",
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""w-5 h-5"">
                <path fill-rule=""evenodd"" d=""M3 6a3 3 0 0 1 3-3h2.25a3 3 0 0 1 3 3v2.25a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3V6Zm9.75 0a3 3 0 0 1 3-3H18a3 3 0 0 1 3 3v2.25a3 3 0 0 1-3 3h-2.25a3 3 0 0 1-3-3V6ZM3 15.75a3 3 0 0 1 3-3h2.25a3 3 0 0 1 3 3V18a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3v-2.25Zm9.75 0a3 3 0 0 1 3-3H18a3 3 0 0 1 3 3V18a3 3 0 0 1-3 3h-2.25a3 3 0 0 1-3-3v-2.25Z"" clip-rule=""evenodd"" />
              </svg>"
        ),

        new MenuItem("TestUI", "/testui", false, "Desarrollo",
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor"" class=""w-5 h-5"">
                <path fill-rule=""evenodd"" d=""M14.447 3.026a.75.75 0 0 1 .527.921l-4.5 16.5a.75.75 0 0 1-1.448-.394l4.5-16.5a.75.75 0 0 1 .921-.527ZM16.72 6.22a.75.75 0 0 1 1.06 0l5.25 5.25a.75.75 0 0 1 0 1.06l-5.25 5.25a.75.75 0 1 1-1.06-1.06L21.44 12l-4.72-4.72a.75.75 0 0 1 0-1.06Zm-9.44 0a.75.75 0 0 1 0 1.06L2.56 12l4.72 4.72a.75.75 0 0 1-1.06 1.06L.97 12.53a.75.75 0 0 1 0-1.06l5.25-5.25a.75.75 0 0 1 1.06 0Z"" clip-rule=""evenodd"" />
              </svg>"
        ),
    ];

    private IEnumerable<IGrouping<string?, MenuItem>> GroupedItems =>
        _items.GroupBy(i => i.Section);
}