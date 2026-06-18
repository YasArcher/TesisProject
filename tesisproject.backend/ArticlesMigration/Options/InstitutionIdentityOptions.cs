namespace tesisproject.backend.Options;

public class InstitutionIdentityOptions
{
    public bool Enabled { get; set; }
    public string SourceType { get; set; } = "None";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AuthorsEndpoint { get; set; } = string.Empty;
}
