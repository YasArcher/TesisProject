namespace tesisproject.backend.Options
{
    public class ExternalApiOptions
    {
        public const string SectionName = "ExternalApis";

        public string BaseUrl { get; set; } = string.Empty;

        // Endpoints relativos (empiezan con '/')
        public string UsersEndpoint { get; set; } = "/api/usuarios";
        public string AcademicsEndpoint { get; set; } = "/api/facultades";

        public int TimeoutSeconds { get; set; } = 30;
        public string UserAgent { get; set; } = "TesisProject/1.0";
    }
}
