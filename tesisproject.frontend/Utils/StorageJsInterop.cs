using Microsoft.JSInterop;

namespace tesisproject.frontend.Utils;
public sealed class StorageJsInterop
{
    private readonly IJSRuntime _js;
    public StorageJsInterop(IJSRuntime js) => _js = js;

    public ValueTask SetLocal(string key, string value) =>
        _js.InvokeVoidAsync("localStorage.setItem", key, value);

    public ValueTask<string?> GetLocal(string key) =>
        _js.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask RemoveLocal(string key) =>
        _js.InvokeVoidAsync("localStorage.removeItem", key);
}