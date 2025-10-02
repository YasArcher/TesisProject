using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI.Header
{
    public partial class Header : ComponentBase
    {
        // Public API
        [Parameter] public string? Title { get; set; }
        [Parameter] public string? LogoUrl { get; set; }
        [Parameter] public RenderFragment? Logo { get; set; }
        [Parameter] public bool ShowBorder { get; set; } = true;
        [Parameter] public HeaderVariant Variant { get; set; } = HeaderVariant.Default;
        [Parameter] public string? Class { get; set; }

        // Slots
        [Parameter] public RenderFragment? HeaderNav { get; set; }
        [Parameter] public RenderFragment? HeaderActions { get; set; }
        [Parameter] public RenderFragment? HeaderUser { get; set; }

        // Local state
        private bool _navOpen;

        // Toggle the mobile nav panel
        private void ToggleNav() => _navOpen = !_navOpen;

        // Close when a nav link is clicked on mobile
        private void CloseNav() => _navOpen = false;

        private string BuildWrapperClasses()
        {
            var border = ShowBorder ? "border-b border-gray-200/60 dark:border-gray-800/60" : "";
            var elevated = Variant == HeaderVariant.Elevated ? "shadow-sm" : "";
            return $"sticky top-0 z-40 bg-white/80 dark:bg-gray-900/70 backdrop-blur {border} {elevated} {Class}".Trim();
        }
        private void CloseNotifications()
        {
            _notificationsOpen = false;
        }

        private void CloseUserMenu()
        {
            _userMenuOpen = false;
        }
    }
}