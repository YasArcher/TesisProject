using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Linq.Expressions;
using tesisproject.frontend.SharedUI.TextInput;

namespace tesisproject.frontend.SharedUI.SearchSelectInput
{
    // OJO: genéricos + herencia + interfaces
    public partial class SearchSelectInput<TValue, TItem>
        : ComponentBase, IHasValidationState, IDisposable
    {
        // ---- Public API ----
        [Parameter] public string? Label { get; set; }
        [Parameter] public TValue? Value { get; set; }
        [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
        [Parameter] public EventCallback<TValue?> ValueSelected { get; set; } // opcional extra, por si quieres un evento al seleccionar
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Required { get; set; }
        [Parameter] public string? HelpText { get; set; }
        [Parameter] public int DebounceMs { get; set; } = 300;

        // Sugerencias / datos
        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        [Parameter] public Func<TItem, string>? ItemLabelSelector { get; set; }
        [Parameter] public Func<TItem, TValue>? ItemValueSelector { get; set; }

        /// <summary>
        /// Se dispara cuando cambia el texto de búsqueda (después del debounce).
        /// El padre puede usarlo para llamar a un API y actualizar Items.
        /// </summary>
        [Parameter] public EventCallback<string> OnSearchRequested { get; set; }

        // EditForm / validación
        [CascadingParameter] private EditContext? EditContext { get; set; }
        [Parameter] public Expression<Func<TValue?>>? For { get; set; }

        // IHasValidationState
        public string? ErrorMessage { get; private set; }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // Internos
        private string? _searchText;
        private TValue? _currentValue;
        private bool _isOpen;
        private CancellationTokenSource? _debounceCts;
        private long _inputSeq = 0;
        private FieldIdentifier? _fieldId;
        private IEnumerable<TItem>? Suggestions => Items;

        protected override void OnInitialized()
        {
            _currentValue = Value;
            SyncTextWithValue();

            if (EditContext != null && For != null)
            {
                _fieldId = FieldIdentifier.Create(For);
                EditContext.OnValidationStateChanged += OnValidationStateChanged;
            }

            RecomputeErrors();
        }

        protected override void OnParametersSet()
        {
            // Si el padre cambió el Value, sincronizamos
            if (!EqualityComparer<TValue?>.Default.Equals(_currentValue, Value))
            {
                _currentValue = Value;
                SyncTextWithValue();
                RecomputeErrors();
            }
        }

        private void SyncTextWithValue()
        {
            if (_currentValue is null)
            {
                _searchText = string.Empty;
                return;
            }

            if (Items != null && ItemValueSelector != null)
            {
                var match = Items.FirstOrDefault(i =>
                    EqualityComparer<TValue>.Default.Equals(ItemValueSelector(i), _currentValue));

                if (match is not null && ItemLabelSelector != null)
                {
                    _searchText = ItemLabelSelector(match);
                    return;
                }
            }

            // fallback: ToString del valor
            _searchText = _currentValue?.ToString();
        }

        private void OnFocus(FocusEventArgs _)
        {
            if (!Disabled && Suggestions?.Any() == true)
            {
                _isOpen = true;
            }
        }

        private async void OnBlur(FocusEventArgs _)
        {
            // Pequeño delay para permitir clic en item antes de cerrar
            await Task.Delay(150);
            _isOpen = false;
            StateHasChanged();
        }

        private async Task OnInputAsync(ChangeEventArgs e)
        {
            _searchText = e.Value?.ToString();
            _isOpen = true;

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
                if (mySeq != Volatile.Read(ref _inputSeq)) return;

                if (OnSearchRequested.HasDelegate)
                {
                    await OnSearchRequested.InvokeAsync(_searchText ?? string.Empty);
                }

                RecomputeErrors();
            }
            catch (TaskCanceledException)
            {
                // typing rápido: ignorar
            }
        }

        private async Task OnItemSelected(TItem item)
        {
            if (ItemValueSelector == null)
                return;

            var newValue = ItemValueSelector(item);
            _currentValue = newValue;

            if (ItemLabelSelector != null)
                _searchText = ItemLabelSelector(item);
            else
                _searchText = item?.ToString();

            _isOpen = false;

            if (ValueChanged.HasDelegate)
                await ValueChanged.InvokeAsync(_currentValue);

            if (ValueSelected.HasDelegate)
                await ValueSelected.InvokeAsync(_currentValue);

            RecomputeErrors();
            StateHasChanged();
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

            // Validación local: requerido por valor
            if (Required && EqualityComparer<TValue?>.Default.Equals(_currentValue, default))
            {
                ErrorMessage = "Este campo es obligatorio";
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