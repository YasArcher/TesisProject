using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.SearchInput
{

    public partial class SearchInput
    {
        [Parameter] public string Value { get; set; } = string.Empty;
        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        [Parameter] public string Placeholder { get; set; } = "Search…";
        [Parameter] public string AriaLabel { get; set; } = "Search input";

        // Estilos externos opcionales
        [Parameter] public string Class { get; set; } = string.Empty;
        [Parameter] public string InputClass { get; set; } = string.Empty;

        // Debounce opcional (ms). Si 0, emite en cada oninput.
        [Parameter] public int DebounceMs { get; set; } = 0;

        // Slots opcionales
        [Parameter] public RenderFragment? Prefix { get; set; }
        [Parameter] public RenderFragment? Suffix { get; set; }

        private ElementReference _inputRef;
        private CancellationTokenSource? _cts;

        private async Task OnInput(ChangeEventArgs e)
        {
            var next = e.Value?.ToString() ?? string.Empty;

            if (DebounceMs <= 0)
            {
                Value = next;
                await ValueChanged.InvokeAsync(Value);
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Value = next;

            try
            {
                await Task.Delay(DebounceMs, token);
                if (!token.IsCancellationRequested)
                {
                    await ValueChanged.InvokeAsync(Value);
                }
            }
            catch (TaskCanceledException) { /* ignore */ }
        }

        private async Task Clear()
        {
            _cts?.Cancel();
            Value = string.Empty;
            await ValueChanged.InvokeAsync(Value);
        }

    }
}
