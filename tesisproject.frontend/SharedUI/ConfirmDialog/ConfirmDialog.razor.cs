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
            ConfirmIntent.Danger => "inline-flex items-center rounded-xl px-4 py-2 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring",
            ConfirmIntent.Warning => "inline-flex items-center rounded-xl px-4 py-2 text-sm font-semibold text-white bg-amber-500 hover:bg-amber-600 focus:outline-none focus:ring",
            _ /* Neutral */        => "inline-flex items-center rounded-xl px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring",
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