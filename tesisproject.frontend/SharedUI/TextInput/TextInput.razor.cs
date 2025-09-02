using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

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
        [Parameter] public int DebounceMs { get; set; } = 300;
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

        protected override void OnInitialized()
        {
            _value = Value;

            // Auto-enable password toggle for password inputs
            if (InputType == "password" && !ShowPasswordToggle)
            {
                ShowPasswordToggle = true;
            }

            if (EditContext != null && For != null)
            {
                _fieldId = FieldIdentifier.Create(For);
                EditContext.OnValidationStateChanged += OnValidationStateChanged;
            }

            RecomputeErrors();
        }

        protected override void OnParametersSet()
        {
            if (_value != Value)
            {
                _value = Value;
                RecomputeErrors();
            }
        }

        // NEW: Method to get the actual input type
        private string GetActualInputType()
        {
            if (InputType == "password" && ShowPasswordToggle && _showPassword)
            {
                return "text";
            }
            return InputType;
        }

        // NEW: Toggle password visibility
        private void TogglePasswordVisibility()
        {
            _showPassword = !_showPassword;
        }

        // NEW: Check if should show toggle button
        private bool ShouldShowPasswordToggle()
        {
            return ShowPasswordToggle && InputType == "password";
        }

        private async Task OnInputAsync(ChangeEventArgs e)
        {
            _value = e.Value?.ToString();

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
                    RecomputeErrors();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when typing quickly
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