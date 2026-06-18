namespace tesisproject.frontend.SharedUI.TextInput
{
    public interface IHasValidationState
    {
        string? ErrorMessage { get; }
        bool HasError { get; }
    }
}