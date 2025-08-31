using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace tesisproject.frontend.SharedUI.ConfirmDialog
{
    public partial class ConfirmDialog : ConfirmDialogBase { }

    public class ConfirmDialogBase : ComponentBase
    {
        // API
        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

        [Parameter] public string Title { get; set; } = "Are you sure?";
        [Parameter] public string? Message { get; set; }

        [Parameter] public ConfirmIntent Intent { get; set; } = ConfirmIntent.Neutral;

        [Parameter] public string ConfirmText { get; set; } = "Confirm";
        [Parameter] public string CancelText { get; set; } = "Cancel";

        [Parameter] public EventCallback OnConfirm { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        protected string ConfirmButtonClasses => Intent switch
        {
            ConfirmIntent.Danger => "btn-primary-solid bg-danger hover:bg-danger-hover text-on-danger focus:ring-accent",
            ConfirmIntent.Warning => "btn-primary-solid bg-warning hover:bg-warning-hover text-on-warning focus:ring-accent",
            _ /* Neutral */       => "btn-primary-solid bg-primary hover:bg-primary-hover text-white focus:ring-accent",
        };


        protected async Task ConfirmAsync()
        {
            if (OnConfirm.HasDelegate)
                await OnConfirm.InvokeAsync();

            await CloseAsync();
        }

        protected async Task CancelAsync()
        {
            if (OnCancel.HasDelegate)
                await OnCancel.InvokeAsync();

            await CloseAsync();
        }

        protected async Task CloseAsync()
        {
            IsOpen = false;
            if (IsOpenChanged.HasDelegate)
                await IsOpenChanged.InvokeAsync(IsOpen);
            StateHasChanged();
        }

        protected async Task OnBackdropClick()
        {
            // Treat backdrop click as cancel for a better UX
            await CancelAsync();
        }

        protected async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Escape")
                await CancelAsync();
        }
    }
}