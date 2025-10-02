using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Threading;

namespace tesisproject.frontend.SharedUI.TextInput
{
    public partial class TextInput : ComponentBase, IHasValidationState, IDisposable
    {
        // ---- Public API (existing) ----
        [Parameter] public string? Label { get; set; }
        [Parameter] public string? Value { get; set; }
        [Parameter] public EventCallback<string?> ValueChanged { get; set; }
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Required { get; set; }
        [Parameter] public string? HelpText { get; set; }
        [Parameter] public int? MaxLength { get; set; }
        [Parameter] public int DebounceMs { get; set; } = 0;
        [Parameter] public string InputType { get; set; } = "text";

        // ---- NEW: Password toggle functionality ----
        [Parameter] public bool ShowPasswordToggle { get; set; } = false;

        // ---- Optional: hook into EditForm validation ----
        [CascadingParameter] private EditContext? EditContext { get; set; }
        [Parameter] public Expression<Func<string?>>? For { get; set; }

        // ---- IHasValidationState ----
        public string? ErrorMessage { get; private set; }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // ---- Internal ----
        protected string? _value;
        private CancellationTokenSource? _debounceCts;
        private FieldIdentifier? _fieldId;

        // NEW: Password visibility state
        private bool _showPassword = false;

        // NEW: secuencia de cambios para descartar emisiones atrasadas
        private long _inputSeq = 0;

        protected override void OnInitialized()
        {
            _value = Value;

            // Auto-enable password toggle for password inputs
            if (InputType == "password" && !ShowPasswordToggle)
                ShowPasswordToggle = true;

            if (EditContext != null && For != null)
            {
                _fieldId = FieldIdentifier.Create(For);
                EditContext.OnValidationStateChanged += OnValidationStateChanged;
            }

            RecomputeErrors();
        }

        protected override void OnParametersSet()
        {
            // Solo sincroniza si el padre realmente cambió el Value
            if (!Equals(_value, Value))
            {
                _value = Value;
                RecomputeErrors();
            }
        }

        // Mostrar/ocultar contraseña
        private string GetActualInputType() =>
            (InputType == "password" && ShowPasswordToggle && _showPassword) ? "text" : InputType;

        private void TogglePasswordVisibility() => _showPassword = !_showPassword;

        private bool ShouldShowPasswordToggle() => ShowPasswordToggle && InputType == "password";

        private async Task OnInputAsync(ChangeEventArgs e)
        {
            _value = e.Value?.ToString();

            // Cada input incrementa la versión
            var mySeq = Interlocked.Increment(ref _inputSeq);

            // Reinicia CTS del debounce
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                var delay = Math.Max(0, DebounceMs);
                if (delay > 0)
                    await Task.Delay(delay, token);

                if (token.IsCancellationRequested) return;

                // Solo emite si sigue siendo la última versión registrada
                if (mySeq == Volatile.Read(ref _inputSeq))
                {
                    // Evita re-renders innecesarios si el padre ya tiene ese mismo valor
                    if (ValueChanged.HasDelegate)
                        await InvokeAsync(() => ValueChanged.InvokeAsync(_value));

                    // Recalcula errores en el local
                    RecomputeErrors();
                }
                // Si no coincide, era una emisión vieja: ignorar.
            }
            catch (TaskCanceledException)
            {
                // typing rápido: esperado
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
            if (_fieldId.HasValue && EditContext != null)
            {
                ErrorMessage = EditContext.GetValidationMessages(_fieldId.Value).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ErrorMessage)) return;
            }

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
