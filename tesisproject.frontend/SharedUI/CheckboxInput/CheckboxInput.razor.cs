using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.CheckboxInput
{
    public partial class CheckboxInput : ComponentBase
    {
        [Parameter] public bool Value { get; set; }
        [Parameter] public EventCallback<bool> ValueChanged { get; set; }

        [Parameter] public string? Label { get; set; }
        [Parameter] public string? HelpText { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Dense { get; set; }
        [Parameter] public bool Center { get; set; }
        [Parameter] public string? Class { get; set; }

        // Variable local que refleja el valor real
        private bool _currentValue;

        protected override void OnParametersSet()
        {
            _currentValue = Value;
        }

        private async Task ToggleAsync()
        {
            if (Disabled) return;

            _currentValue = !_currentValue;
            await ValueChanged.InvokeAsync(_currentValue);
        }

        private string BuildContainerClass()
        {
            var center = Center ? "flex justify-center" : "";
            return $"{center} {Class}".Trim();
        }

        private string BuildWrapperClass()
        {
            var base_ = "inline-flex items-center gap-2 select-none";
            var cursor = Disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer";
            return $"{base_} {cursor}";
        }

        private string BuildTrackClass()
        {
            var size = Dense ? "h-4 w-7" : "h-5 w-9";
            var color = _currentValue ? "bg-primary" : "bg-border";
            return $"{size} {color} rounded-full relative flex items-center transition-colors duration-200 ease-in-out";
        }

        private string BuildKnobClass()
        {
            var size = Dense ? "h-3 w-3" : "h-3.5 w-3.5";
            var position = _currentValue
                ? (Dense ? "translate-x-3.5" : "translate-x-5")
                : "translate-x-0.5";
            return $"{size} bg-white rounded-full shadow-sm transform {position} transition-transform duration-200 ease-in-out absolute";
        }

        private string BuildLabelClass()
        {
            return Dense
                ? "text-xs font-medium text-foreground"
                : "text-sm font-medium text-foreground";
        }
    }
}