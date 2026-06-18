using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace tesisproject.frontend.SharedUI.Header
{
    public partial class Header : ComponentBase
    {
        // Brand/Logo Parameters
        [Parameter] public string? BrandName { get; set; }
        // Header Style Parameters
        [Parameter] public string BackgroundClass { get; set; } = "bg-white dark:bg-gray-900";
        [Parameter] public string BorderClass { get; set; } = "border-b border-gray-200 dark:border-gray-700";
        [Parameter] public bool Sticky { get; set; } = true;
        [Parameter] public bool Shadow { get; set; } = true;

        // Search Parameters
        [Parameter] public bool ShowSearch { get; set; } = false;
        [Parameter] public string SearchPlaceholder { get; set; } = "Search...";
        [Parameter] public string SearchTerm { get; set; } = "";
        [Parameter] public EventCallback<string> OnSearchChanged { get; set; }

        // Breadcrumbs Parameters
        [Parameter] public bool ShowBreadcrumbs { get; set; } = false;
        [Parameter] public List<BreadcrumbItem>? Breadcrumbs { get; set; }

        // Actions Parameters
        [Parameter] public List<HeaderAction>? Actions { get; set; }
        [Parameter] public EventCallback<string> OnActionClick { get; set; }

        // Theme Toggle Parameters
        [Parameter] public bool ShowThemeToggle { get; set; } = false;
        [Parameter] public EventCallback OnThemeToggle { get; set; }

        // Notifications Parameters
        [Parameter] public bool ShowNotifications { get; set; } = true;
        [Parameter] public int NotificationsCount { get; set; } = 0;
        [Parameter] public List<NotificationItem>? Notifications { get; set; }
        [Parameter] public string NotificationsEmptyText { get; set; } = "No notifications";
        [Parameter] public EventCallback OnMarkAllNotificationsRead { get; set; }
        [Parameter] public EventCallback<string> OnNotificationClick { get; set; }

        // User Menu Parameters
        [Parameter] public bool ShowUserMenu { get; set; } = true;
        [Parameter] public string? UserName { get; set; }
        [Parameter] public string? UserEmail { get; set; }
        [Parameter] public string? UserAvatarUrl { get; set; }
        [Parameter] public bool UserMenuCompact { get; set; } = false;
        [Parameter] public List<UserMenuItem>? UserMenuItems { get; set; }
        [Parameter] public EventCallback<string> OnUserMenuClick { get; set; }

        // Mobile Menu Parameters
        [Parameter] public bool ShowMobileMenuButton { get; set; } = false;
        [Parameter] public RenderFragment? MobileMenuContent { get; set; }

        // Content Parameters
        [Parameter] public RenderFragment? CenterContent { get; set; }

        // State
        private bool _notificationsOpen = false;
        private bool _userMenuOpen = false;
        private bool _mobileMenuOpen = false;

        protected override void OnInitialized()
        {
            UserMenuItems ??= GetDefaultUserMenuItems();
        }

        // Header CSS Classes
        private string GetHeaderClasses()
        {
            var classes = new List<string> { BackgroundClass };

            if (!string.IsNullOrWhiteSpace(BorderClass))
                classes.Add(BorderClass);

            if (Sticky)
                classes.Add("sticky top-0 z-50");

            if (Shadow)
                classes.Add("shadow-sm");

            return string.Join(" ", classes);
        }

        private string GetActionButtonClasses(HeaderAction action)
        {
            var baseClasses = "inline-flex items-center rounded-xl px-3 py-2 text-sm font-medium focus:outline-none focus-visible:ring-2 focus-visible:ring-sky-500";

            return action.Style switch
            {
                "primary" => $"{baseClasses} bg-sky-600 text-white hover:bg-sky-700",
                "secondary" => $"{baseClasses} bg-gray-100 text-gray-900 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-100 dark:hover:bg-gray-700",
                "danger" => $"{baseClasses} bg-red-600 text-white hover:bg-red-700",
                _ => $"{baseClasses} text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800"
            };
        }

        // Search Methods
        private async Task HandleSearchInput(ChangeEventArgs e)
        {
            var newValue = e.Value?.ToString() ?? string.Empty;
            SearchTerm = newValue;
            if (OnSearchChanged.HasDelegate)
            {
                await OnSearchChanged.InvokeAsync(newValue);
            }
        }

        // Action Methods
        private async Task HandleActionClick(string actionKey)
        {
            if (OnActionClick.HasDelegate)
            {
                await OnActionClick.InvokeAsync(actionKey);
            }
        }

        // Theme Methods
        private async Task HandleThemeToggle()
        {
            if (OnThemeToggle.HasDelegate)
            {
                await OnThemeToggle.InvokeAsync();
            }
        }

        // Notifications Methods
        private string BuildNotificationsRootClasses()
        {
            return "relative";
        }

        private async Task ToggleNotificationsAsync()
        {
            _notificationsOpen = !_notificationsOpen;
            if (_notificationsOpen)
            {
                _userMenuOpen = false;
                _mobileMenuOpen = false;
            }
            await Task.CompletedTask;
        }

        private async Task HandleMarkAllNotificationsRead()
        {
            if (OnMarkAllNotificationsRead.HasDelegate)
            {
                await OnMarkAllNotificationsRead.InvokeAsync();
            }
        }

        private async Task HandleNotificationClick(string notificationId)
        {
            if (OnNotificationClick.HasDelegate)
            {
                await OnNotificationClick.InvokeAsync(notificationId);
            }
            _notificationsOpen = false;
        }

        private void OnNotificationsDocumentMouseDown(MouseEventArgs e)
        {
            _notificationsOpen = false;
            StateHasChanged();
        }

        private void OnNotificationsWindowKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Escape")
            {
                _notificationsOpen = false;
                StateHasChanged();
            }
        }

        private string GetNotificationTypeAccent(string type)
        {
            return type?.ToLower() switch
            {
                "success" => "bg-green-500",
                "warning" => "bg-yellow-500",
                "error" => "bg-red-500",
                "info" => "bg-blue-500",
                _ => "bg-gray-400"
            };
        }

        private string GetUnreadDot(bool unread)
        {
            return unread ? "opacity-100" : "opacity-0";
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var diff = DateTime.Now - dateTime;

            if (diff.TotalMinutes < 1)
                return "now";
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes}m";
            if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours}h";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d";

            return dateTime.ToString("MMM d");
        }

        // User Menu Methods
        private string BuildUserMenuRootClasses()
        {
            return "relative";
        }

        private void ToggleUserMenu()
        {
            _userMenuOpen = !_userMenuOpen;
            if (_userMenuOpen)
            {
                _notificationsOpen = false;
                _mobileMenuOpen = false;
            }
            StateHasChanged();
        }

        private void OnUserMenuDocumentMouseDown(MouseEventArgs e)
        {
            _userMenuOpen = false;
            StateHasChanged();
        }

        private void OnUserMenuWindowKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Escape")
            {
                _userMenuOpen = false;
                StateHasChanged();
            }
        }

        private List<UserMenuItem> ResolveUserMenuItems()
        {
            return UserMenuItems ?? GetDefaultUserMenuItems();
        }

        private async Task HandleUserMenuClick(UserMenuItem item)
        {
            _userMenuOpen = false;
            if (OnUserMenuClick.HasDelegate)
            {
                await OnUserMenuClick.InvokeAsync(item.Key);
            }
        }

        private List<UserMenuItem> GetDefaultUserMenuItems()
        {
            return new List<UserMenuItem>
            {
                new("logout", "Salir", true)
            };
        }

        // Mobile Menu Methods
        private void ToggleMobileMenu()
        {
            _mobileMenuOpen = !_mobileMenuOpen;
            if (_mobileMenuOpen)
            {
                _notificationsOpen = false;
                _userMenuOpen = false;
            }
            StateHasChanged();
        }
    }

    // Supporting Classes
    public record BreadcrumbItem(string Text, string? Url = null);

    public record HeaderAction(string Key, string Text, string? Icon = null, string Style = "ghost", string? Tooltip = null);

    public record NotificationItem(string Id, string Title, string? Description = null, string Type = "info", bool Unread = false, DateTime CreatedAt = default);

    public record UserMenuItem(string Key, string Text, bool Dangerous = false);
    public enum HeaderVariant
    {
        Default,
        Transparent,
        Solid,
        Blur,
        Elevated,
        Minimal
    }
}