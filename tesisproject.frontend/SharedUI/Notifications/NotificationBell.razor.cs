using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace tesisproject.frontend.SharedUI.Notifications
{
    public partial class NotificationBell : ComponentBase
    {
        // Public API
        [Parameter] public int Count { get; set; } = 0;
        [Parameter] public IEnumerable<NotificationItem>? Items { get; set; }
        [Parameter] public string EmptyText { get; set; } = "No notifications";
        [Parameter] public string? Class { get; set; }

        // Events
        [Parameter] public EventCallback OnOpen { get; set; }
        [Parameter] public EventCallback<string> OnItemClick { get; set; }
        [Parameter] public EventCallback OnMarkAllRead { get; set; }

        // Local state
        private bool _open;
        private bool _suppressCloseNextDocClick;

        private async Task ToggleAsync()
        {
            _open = !_open;
            if (_open && OnOpen.HasDelegate)
                await OnOpen.InvokeAsync();

            if (_open)
                _suppressCloseNextDocClick = true;
        }

        private void Close() => _open = false;

        private void OnDocumentMouseDown()
        {
            if (_suppressCloseNextDocClick)
            {
                _suppressCloseNextDocClick = false;
                return;
            }
            _open = false;
        }

        private void OnWindowKeyDown(KeyboardEventArgs e)
        {
            if (e.Key is "Escape" or "Esc")
                _open = false;
        }

        protected static string RelativeTime(DateTimeOffset ts)
        {
            var delta = DateTimeOffset.UtcNow - ts.ToUniversalTime();
            if (delta.TotalSeconds < 60) return $"{Math.Max(0, (int)delta.TotalSeconds)}s ago";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
            return $"{(int)delta.TotalDays}d ago";
        }

        protected static string TypeAccent(NotificationType t) => t switch
        {
            NotificationType.Success => "bg-emerald-500",
            NotificationType.Warning => "bg-amber-500",
            NotificationType.Error=> "bg-rose-500",
            _ => "bg-sky-500",
        };

        protected static string UnreadDot(bool unread) => unread ? "opacity-100" : "opacity-0";

        private string BuildRootClasses()
            => $"relative inline-block {Class}".Trim();
    }
}