using Microsoft.AspNetCore.Components;
using tesisproject.frontend.SharedUI.Forms;

namespace tesisproject.frontend.SharedUI.TextInput
{
    public partial class TextInput : ComponentBase, IHasValidationState
    {
        [Parameter] public string? Label { get; set; }
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public string? HelpText { get; set; }
        [Parameter] public bool Required { get; set; }
        [Parameter] public bool Disabled { get; set; }

        [Parameter] public string? Value { get; set; }
        [Parameter] public EventCallback<string?> ValueChanged { get; set; }

        // Validación
        [Parameter] public bool HasError { get; set; }
        [Parameter] public string? ErrorText { get; set; }

        protected async Task OnInputChanged(ChangeEventArgs e)
        {
            var newValue = e.Value?.ToString();
            Value = newValue;
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(newValue);
            }
        }
    }
}
