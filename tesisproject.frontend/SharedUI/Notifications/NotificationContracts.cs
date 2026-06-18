namespace tesisproject.frontend.SharedUI.Notifications
{
    public enum NotificationType { Info, Success, Warning, Error }

    public record NotificationItem(
        string Id,
        string Title,
        string? Description,
        DateTimeOffset CreatedAt,
        NotificationType Type,
        bool Unread
    );
}