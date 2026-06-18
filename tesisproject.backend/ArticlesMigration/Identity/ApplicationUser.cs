using Microsoft.AspNetCore.Identity;

namespace tesisproject.backend.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }
        public string? TermsVersion { get; set; }
    }
}
