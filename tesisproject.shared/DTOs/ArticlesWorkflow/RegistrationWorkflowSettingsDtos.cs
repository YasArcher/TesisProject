namespace tesisproject.shared.DTOs.Workflow;

public static class RegistrationEntryModes
{
    public const string AuthorAndUodide = "AuthorAndUodide";
    public const string AuthorOnly = "AuthorOnly";
    public const string UodideOnly = "UodideOnly";

    public static readonly string[] All = [AuthorAndUodide, AuthorOnly, UodideOnly];
}

public sealed class RegistrationWorkflowSettingsDto
{
    public string EntryMode { get; set; } = RegistrationEntryModes.AuthorAndUodide;
    public string EntryModeLabel { get; set; } = "Autores y Revisor UODIDE";
    public string Description { get; set; } = "Autores y Revisor UODIDE pueden iniciar registros.";
    public List<RegistrationEntryModeOptionDto> EntryModeOptions { get; set; } = [];
}

public sealed class RegistrationEntryModeOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class UpdateRegistrationWorkflowSettingsRequest
{
    public string EntryMode { get; set; } = RegistrationEntryModes.AuthorAndUodide;
}
