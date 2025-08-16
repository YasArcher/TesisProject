using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.ActionButton
{
    public partial class ActionButton : ComponentBase
    {
        // Public API
        [Parameter, EditorRequired] public Func<Task> ExecuteAsync { get; set; } = default!;

        [Parameter] public string? Text { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public RenderFragment? Icon { get; set; }

        [Parameter] public ButtonIntent Intent { get; set; } = ButtonIntent.Neutral;
        [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Md;
        [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Filled;

        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool ConfirmBeforeExecute { get; set; } = false;
        [Parameter] public string ConfirmMessage { get; set; } = "Are you sure?";
        [Parameter] public string? Class { get; set; } // extra Tailwind classes (optional)

        // Internal state
        protected bool _isBusy = false;

        protected async Task OnClickInternal()
        {
            if (_isBusy || Disabled) return;

            if (ConfirmBeforeExecute && IsDestructive(Intent))
            {
                if (!await ConfirmAsync(ConfirmMessage)) return;
            }

            _isBusy = true;
            try
            {
                if (ExecuteAsync is not null)
                    await ExecuteAsync.Invoke();
            }
            finally
            {
                _isBusy = false;
                StateHasChanged();
            }
        }

        protected static bool IsDestructive(ButtonIntent intent)
            => intent == ButtonIntent.Delete || intent == ButtonIntent.Danger;

        // Reemplaza por tu modal o IJSRuntime si lo deseas
        protected Task<bool> ConfirmAsync(string message) => Task.FromResult(true);

        protected string BuildClass()
        {
            var baseCls = "inline-flex select-none items-center justify-center font-medium transition-colors rounded-2xl focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed";

            var sizeCls = Size switch
            {
                ButtonSize.Xs => "text-xs px-2 py-1",
                ButtonSize.Sm => "text-sm px-3 py-1.5",
                ButtonSize.Md => "text-sm px-4 py-2",
                ButtonSize.Lg => "text-base px-5 py-2.5",
                ButtonSize.Xl => "text-base px-6 py-3",
                _ => "text-sm px-4 py-2"
            };

            var (bg, txt, ring, hover) = GetPalette(Intent);

            var variantCls = Variant switch
            {
                ButtonVariant.Filled => $"{bg} {txt} {hover} {ring}",
                ButtonVariant.Outline => $"border {txt} {ring} hover:bg-black/5 dark:hover:bg-white/10 border-current",
                ButtonVariant.Ghost => $"{txt} {ring} hover:bg-black/5 dark:hover:bg-white/10",
                ButtonVariant.Soft => $"bg-black/5 dark:bg-white/10 {txt} {ring} hover:bg-black/10 dark:hover:bg-white/20",
                _ => $"{bg} {txt} {hover} {ring}",
            };

            return $"{baseCls} {sizeCls} {variantCls} {Class}".Trim();
        }

        protected static (string bg, string txt, string ring, string hover) GetPalette(ButtonIntent intent)
            => intent switch
            {
                ButtonIntent.Add or ButtonIntent.Success => ("bg-emerald-600", "text-white", "focus:ring-emerald-500", "hover:bg-emerald-700"),
                ButtonIntent.Accept => ("bg-blue-600", "text-white", "focus:ring-blue-500", "hover:bg-blue-700"),
                ButtonIntent.Delete => ("bg-rose-600", "text-white", "focus:ring-rose-500", "hover:bg-rose-700"),
                ButtonIntent.Warning => ("bg-amber-500", "text-black", "focus:ring-amber-500", "hover:bg-amber-600"),
                ButtonIntent.Info => ("bg-sky-600", "text-white", "focus:ring-sky-500", "hover:bg-sky-700"),
                ButtonIntent.Danger => ("bg-red-600", "text-white", "focus:ring-red-500", "hover:bg-red-700"),
                _ => ("bg-gray-800", "text-white", "focus:ring-gray-600", "hover:bg-gray-900")
            };
    }
}