using Microsoft.JSInterop;

namespace tesisproject.frontend.Utils
{
    public class ExportJsInterop
    {
        private readonly IJSRuntime _js;
        public ExportJsInterop(IJSRuntime js) => _js = js;

        public ValueTask ExportCsv(string filename, string csv)
            => _js.InvokeVoidAsync("tesisExport.exportCsv", filename, csv);

        public ValueTask PrintSection(string elementId)
            => _js.InvokeVoidAsync("tesisExport.printSection", elementId);
    }
}
