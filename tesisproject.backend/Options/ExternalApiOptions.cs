namespace tesisproject.backend.Options
{
    public class ExternalApiOptions
    {
        public const string SectionName = "ExternalApis";

        public string BaseUrl { get; set; } = string.Empty;

        // Endpoints relativos (empiezan con '/')
        public string UsersEndpoint { get; set; } = "/api/usuarios";
        public string AcademicsEndpoint { get; set; } = "/api/facultades";

        // 🔹 Nuevo: parámetro de búsqueda por correo (ejemplo: ?email={correo})
        public string UsersEmailQueryParam { get; set; } = "emails";
        public string UsersDocumentQueryParam { get; set; } = "documents";

        public int TimeoutSeconds { get; set; } = 30;
        public string UserAgent { get; set; } = "TesisProject/1.0";
    }
}
