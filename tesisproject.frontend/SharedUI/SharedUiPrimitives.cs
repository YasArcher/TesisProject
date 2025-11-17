using System;

namespace tesisproject.frontend.SharedUI
{
    public enum ButtonIntent
    {
        Primary,
        Secondary,
        Danger,
        Neutral
    }

    public enum ButtonVariant
    {
        Solid,
        Outline,
        Ghost
    }

    public enum BadgeVariant
    {
        Neutral,
        Success,
        Danger,
        Warning,
        NeutralSoft
    }
}

namespace tesisproject.frontend.SharedUI.Card
{
    public enum CardTone
    {
        Default,
        Info,
        Success,
        Warning,
        Danger
    }
}

namespace tesisproject.frontend.SharedUI.Table
{
    public enum SortDirection
    {
        None,
        Asc,
        Desc
    }
    public class ColumnDef<TItem>
    {
        public string Header { get; set; } = string.Empty;
        public Func<TItem, object?>? ValueSelector { get; set; }
        public string? CssClass { get; set; }
        public bool Sortable { get; set; } = false;
    }
}

namespace tesisproject.frontend.SharedUI.Feedback
{
    public enum SkeletonShape
    {
        Rect,
        Rectangle,
        Circle,
        Line
    }
}

namespace tesisproject.frontend.SharedUI.Tabs
{
    public enum TabVariant
    {
        Default,
        Pill,
        Pills,
        Underline
    }
}

namespace tesisproject.frontend.SharedUI.SelectInput
{
    public enum SelectSize
    {
        Default = 0,
        Small = 1,
        Medium = 2,
        Large = 3,
        Sm = Small,
        Md = Medium,
        Lg = Large
    }

    public class SelectItem
    {
        public string? Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Text
        {
            get => Label;
            set => Label = value;
        }
        public bool Disabled { get; set; }
    }
}

namespace tesisproject.frontend.SharedUI.Forms
{
    public interface IHasValidationState
    {
        bool HasError { get; }
        string? ErrorText { get; }
    }
}
