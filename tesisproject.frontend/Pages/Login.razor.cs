using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using tesisproject.frontend.Services.Auth;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Pages
{
    public partial class Login
    {
        public LoginModel LoginModel { get; set; } = new();

        [Inject] public IAuthClientService AuthClient { get; set; } = null!;
        [Inject] public CustomAuthStateProvider AuthStateProvider { get; set; } = null!;
        [Inject] public NavigationManager Navigation { get; set; } = null!;

        protected async Task HandleLogin()
        {
            var request = new LoginRequest
            {
                Email = LoginModel.Email,
                Password = LoginModel.Password
            };

            var result = await AuthClient.LoginAsync(request);

            // Aquí usas tu HttpResponseWrapper<T> como ya lo haces en el resto del sistema
            if (result.HttpResponse.IsSuccessStatusCode && result.Response is not null)
            {
                // Guardar el access token en tu AuthStateProvider
                await AuthStateProvider.SetTokenAsync(result.Response.AccessToken);

                // Redirigir a la página principal (o donde tú quieras)
                Navigation.NavigateTo("/");
            }
            else
            {
                // Aquí metes tu Toast / manejo de errores
                var errorMessage = result.Error ?? "Invalid credentials.";
                Console.WriteLine(errorMessage);
            }
        }
    }

    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}