namespace tesisproject.shared.DTOs.Auth
{
    public class AuthMeResponse
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string[] Roles { get; set; } = [];
        public bool HasAcceptedTerms { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }
        public string? TermsVersion { get; set; }
    }

    public class AcceptTermsRequest
    {
        public string TermsVersion { get; set; } = "2026-04";
    }
}
