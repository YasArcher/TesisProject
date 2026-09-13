namespace tesisproject.backend.Options;

public sealed class AdministrativeOperationsOptions
{
    public const string SectionName = "AdministrativeOperations";
    public bool Enabled { get; set; }
    public string? Secret { get; set; }
}
