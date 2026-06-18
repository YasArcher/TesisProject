namespace tesisproject.backend.Options
{
    public class ExternalApiExplorerOptions
    {
        public ExternalApiProviderAuthOptions Scopus { get; set; } = new();
        public ExternalApiProviderAuthOptions Crossref { get; set; } = new();
        public ExternalApiProviderAuthOptions OpenAlex { get; set; } = new();
        public ExternalApiProviderAuthOptions SemanticScholar { get; set; } = new();
    }

    public class ExternalApiProviderAuthOptions
    {
        public string? ApiKey { get; set; }
        public string? InstToken { get; set; }
    }
}
