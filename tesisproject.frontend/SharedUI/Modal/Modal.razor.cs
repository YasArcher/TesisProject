using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace tesisproject.frontend.SharedUI.Modal
{
    public class ModalBase : ComponentBase, IDisposable
    {
        [Inject] protected IModalService? ModalService { get; set; }

        [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public ModalSize Size { get; set; } = ModalSize.Md;
        [Parameter] public bool ShowCloseButton { get; set; } = true;
        [Parameter] public bool CloseOnBackdrop { get; set; } = true;
        [Parameter] public bool CloseOnEscape { get; set; } = true;

        [Parameter] public RenderFragment? Footer { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

        protected string DialogMaxWidth => Size switch
        {
            ModalSize.Sm => "max-w-sm",
            ModalSize.Md => "max-w-md",
            ModalSize.Lg => "max-w-2xl",
            ModalSize.Xl => "max-w-4xl",
            _ => "max-w-md"
        };

        protected ElementReference _keyTarget;
        private bool _shouldFocus;

        protected override void OnInitialized()
        {
            if (ModalService is not null)
            {
                ModalService.OpenRequested += HandleOpenRequested;
                ModalService.CloseRequested += HandleCloseRequested;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_shouldFocus)
            {
                _shouldFocus = false;
                try { await _keyTarget.FocusAsync(); } catch { /* ignore if not yet in DOM */ }
            }
        }

        private void HandleOpenRequested(string id)
        {
            if (id == Id)
            {
                IsOpen = true;
                _shouldFocus = true;   // enfocar para capturar ESC
                StateHasChanged();
            }
        }

        private void HandleCloseRequested(string id)
        {
            if (id == Id)
            {
                _ = Close();
            }
        }

        protected async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (!IsOpen) return;
            if (CloseOnEscape && (e.Key == "Escape" || e.Code == "Escape"))
            {
                await Close();
            }
        }

        protected async Task OnBackdropClick()
        {
            if (CloseOnBackdrop)
            {
                await Close();
            }
        }

        protected async Task Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (IsOpenChanged.HasDelegate)
                await IsOpenChanged.InvokeAsync(false);
            if (OnClose.HasDelegate)
                await OnClose.InvokeAsync();
            StateHasChanged();
        }

        public void Dispose()
        {
            if (ModalService is not null)
            {
                ModalService.OpenRequested -= HandleOpenRequested;
                ModalService.CloseRequested -= HandleCloseRequested;
            }
        }
    }
}
