using Microsoft.JSInterop;

namespace tesisproject.frontend.Utils
{
    public class ExportJsInterop
    {
        private readonly IJSRuntime _js;
        public ExportJsInterop(IJSRuntime js) => _js = js;

        public ValueTask ExportCsv(string filename, string csv)
            => _js.InvokeVoidAsync("tesisExport.exportCsv", filename, csv);

        public ValueTask DownloadFile(string filename, string contentType, byte[] content)
            => _js.InvokeVoidAsync("tesisExport.downloadFileFromBase64", filename, contentType, Convert.ToBase64String(content));

        public ValueTask<string> CreateObjectUrl(string contentType, byte[] content)
            => _js.InvokeAsync<string>("tesisExport.createObjectUrlFromBase64", contentType, Convert.ToBase64String(content));

        public ValueTask RevokeObjectUrl(string? url)
            => _js.InvokeVoidAsync("tesisExport.revokeObjectUrl", url);

        public ValueTask PrintSection(string elementId)
            => _js.InvokeVoidAsync("tesisExport.printSection", elementId);
    }
}
