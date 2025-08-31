namespace tesisproject.frontend.SharedUI.UserMenu
{
    // Allow overriding default items if desired
    public record UserMenuItem(string Text, string Key, bool Dangerous = false);
}