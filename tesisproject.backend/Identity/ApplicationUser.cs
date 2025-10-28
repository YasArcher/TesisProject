using Microsoft.AspNetCore.Identity;

namespace tesisproject.backend.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}
