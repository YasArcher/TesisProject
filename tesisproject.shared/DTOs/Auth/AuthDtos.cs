using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
        [Required, StringLength(10)] public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        [Required] public int? AspUserId { get; set; }
    }

    public class LoginRequest
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    // Respuesta estándar tipo Supabase
    public class AuthResponse
    {
        public string TokenType { get; set; } = "Bearer";
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; set; }

        // Refresh solo en cookie HttpOnly (recomendado). 
        // Si quieres exponerlo por cuerpo (menos seguro) añade:
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    }
}
