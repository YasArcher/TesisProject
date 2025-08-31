using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace tesisproject.frontend.SharedUI.TextInput
{
    public partial class TextInput : ComponentBase, IHasValidationState, IDisposable
    {
        // ---- Public API (requested) ----
        [Parameter] public string? Label { get; set; }
        [Parameter] public string? Value { get; set; }
        [Parameter] public EventCallback<string?> ValueChanged { get; set; }
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Required { get; set; }
        [Parameter] public string? HelpText { get; set; }
        [Parameter] public int? MaxLength { get; set; }
        [Parameter] public int DebounceMs { get; set; } = 300;
        [Parameter] public string InputType { get; set; } = "text";


        // ---- Optional: hook into EditForm validation (keeps your surface API intact) ----
        [CascadingParameter] private EditContext? EditContext { get; set; }

        /// <summary>
        /// Optional expression to link this input to a model field in EditForm.
        /// If provided, the component will display DataAnnotations validation messages.
        /// </summary>
        [Parameter] public Expression<Func<string?>>? For { get; set; }

        // ---- IHasValidationState ----
        public string? ErrorMessage { get; private set; }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // ---- Internal ----
        protected string? _value;
        private CancellationTokenSource? _debounceCts;
        private FieldIdentifier? _fieldId;

        protected override void OnInitialized()
        {
            _value = Value;

            if (EditContext != null && For != null)
            {
                _fieldId = FieldIdentifier.Create(For);
                EditContext.OnValidationStateChanged += OnValidationStateChanged;
            }

            // Initial local validation (for Required/MaxLength)
            RecomputeErrors();
        }

        protected override void OnParametersSet()
        {
            // Keep internal in sync if external changes
            if (_value != Value)
            {
                _value = Value;
                RecomputeErrors();
            }
        }

        private async Task OnInputAsync(ChangeEventArgs e)
        {
            _value = e.Value?.ToString();

            // Cancel previous pending debounce
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();

            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                var delay = Math.Max(0, DebounceMs);
                if (delay > 0)
                    await Task.Delay(delay, token);

                if (!token.IsCancellationRequested)
                {
                    await ValueChanged.InvokeAsync(_value);
                    RecomputeErrors(); // validate after change
                }
            }
            catch (TaskCanceledException)
            {
                // Swallow: expected when typing quickly
            }
        }

        private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        {
            if (_fieldId.HasValue)
            {
                ErrorMessage = EditContext?
                    .GetValidationMessages(_fieldId.Value)
                    .FirstOrDefault();
                StateHasChanged();
            }
        }

        private void RecomputeErrors()
        {
            // Prefer EditForm messages if For is provided
            if (_fieldId.HasValue && EditContext != null)
            {
                ErrorMessage = EditContext.GetValidationMessages(_fieldId.Value).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ErrorMessage)) return;
            }

            // Fallback: lightweight local checks
            ErrorMessage = null;

            if (Required && string.IsNullOrWhiteSpace(_value))
            {
                ErrorMessage = "This field is required.";
                return;
            }

            if (MaxLength.HasValue && _value?.Length > MaxLength.Value)
            {
                ErrorMessage = $"Maximum length is {MaxLength.Value} characters.";
                return;
            }
        }

        public void Dispose()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();

            if (EditContext != null)
                EditContext.OnValidationStateChanged -= OnValidationStateChanged;
        }
    }
}