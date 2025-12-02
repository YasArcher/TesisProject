using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Threading;

namespace tesisproject.frontend.SharedUI.TextInput
{
    public partial class TextInput<TValue> : ComponentBase, IHasValidationState, IDisposable
    {
        // ---- Public API ----
        [Parameter] public string? Label { get; set; }
        [Parameter] public TValue? Value { get; set; }
        [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Required { get; set; }
        [Parameter] public string? HelpText { get; set; }
        [Parameter] public int? MaxLength { get; set; }
        [Parameter] public int DebounceMs { get; set; } = 0;
        [Parameter] public string InputType { get; set; } = "text";

        // Password toggle
        [Parameter] public bool ShowPasswordToggle { get; set; } = false;

        // EditForm / validación
        [CascadingParameter] private EditContext? EditContext { get; set; }
        [Parameter] public Expression<Func<TValue?>>? For { get; set; }

        // IHasValidationState
        public string? ErrorMessage { get; private set; }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // Internos
        protected string? _text;                  // lo que ve el input
        private TValue? _currentValue;            // valor tipado
        private CancellationTokenSource? _debounceCts;
        private FieldIdentifier? _fieldId;
        private bool _showPassword = false;
        private long _inputSeq = 0;

        protected override void OnInitialized()
        {
            _currentValue = Value;
            _text = FormatValue(_currentValue);

            // auto habilitar toggle para password
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
            if (!EqualityComparer<TValue?>.Default.Equals(_currentValue, Value))
            {
                _currentValue = Value;
                _text = FormatValue(_currentValue);
                RecomputeErrors();
            }
        }

        // Password
        private string GetActualInputType()
        {

            return (InputType == "password" && ShowPasswordToggle && _showPassword)
                ? "text"
                : InputType;
        }

        private void TogglePasswordVisibility() => _showPassword = !_showPassword;

        private bool ShouldShowPasswordToggle() => ShowPasswordToggle && InputType == "password";

        // Formatear valor tipado a string
        private string? FormatValue(TValue? value)
        {
            if (value is null)
                return string.Empty;

            if (value is IFormattable formattable && InputType == "number")
                return formattable.ToString(null, CultureInfo.CurrentCulture);

            return value?.ToString();
        }

        // Parsear string a TValue
        private bool TryParseValueFromString(string? value, out TValue? result)
        {
            // string → string
            if (typeof(TValue) == typeof(string))
            {
                result = (TValue?)(object?)value;
                return true;
            }

            // vacío → default (útil para tipos anulables)
            if (string.IsNullOrWhiteSpace(value))
            {
                result = default;
                return true;
            }

            // conversión genérica usando BindConverter
            if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var parsed))
            {
                result = parsed;
                return true;
            }

            result = default;
            return false;
        }

        private async Task OnInputAsync(ChangeEventArgs e)
        {
            _text = e.Value?.ToString();

            var mySeq = Interlocked.Increment(ref _inputSeq);

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

                if (mySeq == Volatile.Read(ref _inputSeq))
                {
                    if (TryParseValueFromString(_text, out var parsed))
                    {
                        _currentValue = parsed;

                        if (ValueChanged.HasDelegate)
                            await InvokeAsync(() => ValueChanged.InvokeAsync(_currentValue));

                        RecomputeErrors();
                    }
                    // si falla el parse, simplemente no se actualiza el valor tipado
                }
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
            // primero, errores del EditForm/DataAnnotations
            if (_fieldId.HasValue && EditContext != null)
            {
                var fromContext = EditContext.GetValidationMessages(_fieldId.Value).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fromContext))
                {
                    ErrorMessage = fromContext;
                    return;
                }
            }

            ErrorMessage = null;

            // Validaciones locales básicas
            if (Required && string.IsNullOrWhiteSpace(_text))
            {
                ErrorMessage = "Este campo es obligatorio";
                return;
            }

            if (MaxLength.HasValue && _text?.Length > MaxLength.Value)
            {
                ErrorMessage = $"Cantidad maxima de caracteres {MaxLength.Value}.";
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
