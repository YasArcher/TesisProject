using Microsoft.AspNetCore.Identity;

namespace tesisproject.backend.Data.Identity
{
    // Usamos Guid como clave primaria (recomendado para tu modelo)
    public class ApplicationUser : IdentityUser<Guid>
    {
        // Campos extra opcionales (Profile)
        public string? FullName { get; set; }
    }
}