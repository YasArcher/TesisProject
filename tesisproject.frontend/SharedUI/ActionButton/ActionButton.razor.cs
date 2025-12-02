using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.ActionButton
{
    public partial class ActionButton : ComponentBase
    {
        // ---- Public API ----
        [Parameter, EditorRequired] public EventCallback Execute { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        [Parameter] public string? Text { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public RenderFragment? Icon { get; set; }

        [Parameter] public ButtonIntent Intent { get; set; } = ButtonIntent.Neutral;
        [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Md;
        [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Filled;

        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool ConfirmBeforeExecute { get; set; } = false;
        [Parameter] public string ConfirmMessage { get; set; } = "Are you sure?";
        [Parameter] public string? Class { get; set; }

        // ---- Confirmation dialog customization ----
        [Parameter] public string ConfirmTitle { get; set; } = "Confirmation";
        [Parameter] public string ConfirmButtonText { get; set; } = "Yes";
        [Parameter] public string CancelButtonText { get; set; } = "Cancel";

        // ---- Internal state ----
        protected bool _isBusy = false;
        private bool _showConfirmDialog = false;
        private bool _hasBeenUsedForConfirmation = false;
        private TaskCompletionSource<bool>? _confirmTaskSource;

        // ---- Optimization methods ----
        protected bool CanShowConfirmDialog()
        {
            return ConfirmBeforeExecute &&
                   IsDestructive(Intent) &&
                   (_showConfirmDialog || _hasBeenUsedForConfirmation);
        }

        // ---- Click handling ----
        protected async Task OnClickInternal()
        {
            if (_isBusy || Disabled) return;

            if (ConfirmBeforeExecute && IsDestructive(Intent))
            {
                _hasBeenUsedForConfirmation = true;
                var confirmed = await ShowConfirmationAsync();
                if (!confirmed) return;
            }

            _isBusy = true;
            try
            {
                if (Execute.HasDelegate)
                    await Execute.InvokeAsync();
            }
            finally
            {
                _isBusy = false;
                StateHasChanged();
            }
        }

        // ---- Confirmation dialog methods ----
        private async Task<bool> ShowConfirmationAsync()
        {
            _showConfirmDialog = true;
            _confirmTaskSource = new TaskCompletionSource<bool>();
            StateHasChanged();

            return await _confirmTaskSource.Task;
        }

        private void OnConfirmDialogResult(bool confirmed)
        {
            _showConfirmDialog = false;
            _confirmTaskSource?.SetResult(confirmed);
            StateHasChanged();
        }

        // ---- Utility methods ----
        protected static bool IsDestructive(ButtonIntent intent)
            => intent == ButtonIntent.Delete
            || intent == ButtonIntent.Danger
            || intent == ButtonIntent.Warning;

        protected tesisproject.frontend.SharedUI.ConfirmDialog.ConfirmIntent GetConfirmIntent()
        {
            return Intent switch
            {
                ButtonIntent.Delete or ButtonIntent.Danger => tesisproject.frontend.SharedUI.ConfirmDialog.ConfirmIntent.Danger,
                ButtonIntent.Warning => tesisproject.frontend.SharedUI.ConfirmDialog.ConfirmIntent.Warning,
                _ => tesisproject.frontend.SharedUI.ConfirmDialog.ConfirmIntent.Neutral
            };
        }

        // ---- CSS class building ----
        protected string BuildClass()
        {
            var baseCls = "inline-flex select-none items-center justify-center font-medium transition-all duration-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed";

            var sizeCls = Size switch
            {
                ButtonSize.Xs => "text-xs px-2 py-1",
                ButtonSize.Sm => "text-sm px-3 py-1.5",
                ButtonSize.Md => "text-sm px-4 py-2",
                ButtonSize.Lg => "text-base px-5 py-2.5",
                ButtonSize.Xl => "text-base px-6 py-3",
                _ => "text-sm px-4 py-2"
            };

            var variantCls = Variant switch
            {
                ButtonVariant.Filled => GetFilledVariantClass(Intent),
                ButtonVariant.Outline => GetOutlineVariantClass(Intent),
                ButtonVariant.Ghost => GetGhostVariantClass(Intent),
                ButtonVariant.Soft => GetSoftVariantClass(Intent),
                _ => GetFilledVariantClass(Intent),
            };

            return $"{baseCls} {sizeCls} {variantCls} {Class}".Trim();
        }

        protected string GetFilledVariantClass(ButtonIntent intent) => intent switch
        {
            ButtonIntent.Accept or ButtonIntent.Info => "btn-primary-solid",
            ButtonIntent.Add or ButtonIntent.Success => "bg-secondary text-white bg-secondary-hover focus:ring-2",
            ButtonIntent.Warning => "bg-accent text-white hover:bg-accent focus:ring-2",
            ButtonIntent.Danger or ButtonIntent.Delete => "bg-error text-white hover:bg-error focus:ring-2",
            _ => "bg-muted text-foreground hover:bg-muted focus:ring-2"
        };

        protected string GetOutlineVariantClass(ButtonIntent intent) => intent switch
        {
            ButtonIntent.Accept or ButtonIntent.Info => "btn-primary-outline",
            ButtonIntent.Add or ButtonIntent.Success => "border-2 border-secondary text-secondary hover:bg-secondary hover:text-white",
            ButtonIntent.Warning => "border-2 border-accent text-accent hover:bg-accent hover:text-white",
            ButtonIntent.Danger or ButtonIntent.Delete => "border-2 border-error text-error hover:bg-error hover:text-white",
            _ => "border-2 border-muted text-muted hover:bg-muted hover:text-foreground"
        };

        protected string GetGhostVariantClass(ButtonIntent intent) => intent switch
        {
            ButtonIntent.Accept or ButtonIntent.Info => "btn-primary-ghost",
            ButtonIntent.Add or ButtonIntent.Success => "text-secondary hover:bg-secondary-subtle",
            ButtonIntent.Warning => "text-accent hover:bg-accent/10",
            ButtonIntent.Danger or ButtonIntent.Delete => "text-error hover:bg-error/10",
            _ => "text-muted hover:bg-muted/10"
        };

        protected string GetSoftVariantClass(ButtonIntent intent) => intent switch
        {
            ButtonIntent.Accept or ButtonIntent.Info => "bg-primary-subtle text-primary bg-primary-subtle-hover",
            ButtonIntent.Add or ButtonIntent.Success => "bg-secondary-subtle text-secondary hover:bg-secondary-subtle",
            ButtonIntent.Warning => "text-accent hover:bg-accent/20" + " " + "bg-accent/10",
            ButtonIntent.Danger or ButtonIntent.Delete => "text-error hover:bg-error/20" + " " + "bg-error/10",
            _ => "text-muted hover:bg-muted/20" + " " + "bg-muted/10"
        };
    }
}