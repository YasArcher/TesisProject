using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;
using System.Linq.Expressions;

namespace tesisproject.frontend.SharedUI.DateInput
{
    public partial class DateInput : ComponentBase
    {
        private const string DateFormat = "yyyy-MM-dd"; // native <input type="date"> expects this
        protected string _stringValue = string.Empty;
        protected string? _minString;
        protected string? _maxString;

        [CascadingParameter] private EditContext? EditContext { get; set; }

        /// <summary>Optional expression to bind validation messages (for EditForm integration).</summary>
        [Parameter] public Expression<Func<DateOnly?>>? For { get; set; }

        [Parameter] public string? Label { get; set; }
        [Parameter] public DateOnly? Value { get; set; }
        [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }
        [Parameter] public DateOnly? Min { get; set; }
        [Parameter] public DateOnly? Max { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Required { get; set; }

        protected override void OnParametersSet()
        {
            _stringValue = ToDateString(Value);
            _minString = ToDateString(Min);
            _maxString = ToDateString(Max);
        }

        protected async Task OnChanged(ChangeEventArgs e)
        {
            var str = e.Value?.ToString();
            _stringValue = str ?? string.Empty;

            if (string.IsNullOrWhiteSpace(str))
            {
                await ValueChanged.InvokeAsync(null);
                NotifyFieldChanged();
                return;
            }

            if (DateOnlyTryParse(str, out var parsed))
            {
                await ValueChanged.InvokeAsync(parsed);
            }
            else
            {
                // If parsing fails, keep previous Value and surface an error state
                await ValueChanged.InvokeAsync(Value);
            }

            NotifyFieldChanged();
        }

        private void NotifyFieldChanged()
        {
            if (EditContext != null && For != null)
            {
                var fieldIdentifier = FieldIdentifier.Create(For);
                EditContext.NotifyFieldChanged(fieldIdentifier);
            }
        }

        protected bool HasError
        {
            get
            {
                if (Required && Value is null)
                    return true;

                if (Value is not null && Min is not null && Value < Min)
                    return true;
                if (Value is not null && Max is not null && Value > Max)
                    return true;

                return false;
            }
        }

        protected string ErrorText
        {
            get
            {
                if (Required && Value is null)
                    return "This field is required.";
                if (Value is not null && Min is not null && Value < Min)
                    return $"Date must be on or after {ToDateString(Min)}.";
                if (Value is not null && Max is not null && Value > Max)
                    return $"Date must be on or before {ToDateString(Max)}.";
                return string.Empty;
            }
        }

        protected string ValidationMessageFromEditContext
        {
            get
            {
                if (EditContext == null || For == null) return string.Empty;
                var fieldIdentifier = FieldIdentifier.Create(For);
                var messages = EditContext.GetValidationMessages(fieldIdentifier);
                return messages.FirstOrDefault() ?? string.Empty;
            }
        }

        private static string ToDateString(DateOnly? value)
            => value.HasValue ? value.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : string.Empty;

        private static bool DateOnlyTryParse(string value, out DateOnly? result)
        {
            if (DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                result = parsed;
                return true;
            }
            result = null;
            return false;
        }
    }
}