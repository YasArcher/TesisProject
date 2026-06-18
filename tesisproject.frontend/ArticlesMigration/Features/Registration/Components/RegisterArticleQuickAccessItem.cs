namespace tesisproject.frontend.Features.Registration.Components;

public sealed class RegisterArticleQuickAccessItem
{
    public string Label { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public bool IsButton { get; set; }
    public bool IsWarmAccent { get; set; }
}
