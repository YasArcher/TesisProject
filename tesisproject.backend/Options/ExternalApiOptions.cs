namespace tesisproject.backend.Options
{
    public class ExternalApiOptions
    {
        public const string SectionName = "ExternalApis";

        public string BaseUrl { get; set; } = string.Empty;

        // Endpoints relativos
        public string UsersEndpoint { get; set; } = "/api/usuarios";
        public string AcademicsEndpoint { get; set; } = "/api/facultades";
        public string PeriodsEndpoint { get; set; } = "/api/periodos";

        // Query params Users
        public string UsersEmailQueryParam { get; set; } = "emails";
        public string UsersDocumentQueryParam { get; set; } = "documents";

        // Query params Periods
        public string PeriodsNamesQueryParam { get; set; } = "nombres";

        // =========================
        // NUEVO: Distributivos
        // =========================
        public string DistributivosEndpoint { get; set; } = "/api/distributivos";

        public string DistributivosCedulasQueryParam { get; set; } = "cedulas";
        public string DistributivosCorreosQueryParam { get; set; } = "correos";
        public string DistributivosPeriodosQueryParam { get; set; } = "periodos";

        // (Opcional si lo implementas en Node)
        public string DistributivosFacultadesQueryParam { get; set; } = "facultades";

        public int TimeoutSeconds { get; set; } = 30;
        public string UserAgent { get; set; } = "TesisProject/1.0";
    }
}