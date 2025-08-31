using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace tesisproject.frontend.SharedUI.UserMenu
{
    public partial class UserMenu : ComponentBase
    {
        // Public API
        [Parameter] public string? UserName { get; set; }
        [Parameter] public string? UserEmail { get; set; }
        [Parameter] public string? UserAvatarUrl { get; set; }
        [Parameter] public bool Compact { get; set; } = false;
        [Parameter] public string? Class { get; set; }

        // Events
        [Parameter] public EventCallback OnProfile { get; set; }
        [Parameter] public EventCallback OnSettings { get; set; }
        [Parameter] public EventCallback OnLogout { get; set; }

        // Optional custom items
        [Parameter] public IEnumerable<UserMenuItem>? Items { get; set; }

        // Local state
        private bool _open;
        private bool _suppressCloseNextDocClick;

        private void Toggle()
        {
            _open = !_open;
            if (_open)
            {
                // Avoid immediately closing due to the same click bubbling to document
                _suppressCloseNextDocClick = true;
            }
        }

        private void Close() => _open = false;

        // Close on any document click unless we just opened
        private void OnDocumentMouseDown()
        {
            if (_suppressCloseNextDocClick)
            {
                _suppressCloseNextDocClick = false;
                return;
            }
            _open = false;
        }

        // Close on Escape
        private void OnWindowKeyDown(KeyboardEventArgs e)
        {
            if (e.Key is "Escape" or "Esc")
            {
                _open = false;
            }
        }

        // Default items if none provided
        private IEnumerable<UserMenuItem> ResolveItems()
        {
            if (Items is not null) return Items;

            return new[]
            {
                new UserMenuItem("Profile", "profile"),
                new UserMenuItem("Settings", "settings"),
                new UserMenuItem("Logout", "logout", true),
            };
        }

        private async Task InvokeItem(UserMenuItem item)
        {
            switch (item.Key)
            {
                case "profile":
                    if (OnProfile.HasDelegate) await OnProfile.InvokeAsync();
                    break;
                case "settings":
                    if (OnSettings.HasDelegate) await OnSettings.InvokeAsync();
                    break;
                case "logout":
                    if (OnLogout.HasDelegate) await OnLogout.InvokeAsync();
                    break;
            }
            _open = false;
        }

        private string BuildRootClasses()
            => $"relative inline-block text-left {Class}".Trim();
    }
}