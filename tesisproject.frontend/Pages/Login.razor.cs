using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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

        // ✅ BLOQUE CORRECTO: dentro de la clase
        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                Navigation.NavigateTo("/", replace: true);
            }
        }

        protected async Task HandleLogin()
        {
            var request = new LoginRequest
            {
                Email = LoginModel.Email,
                Password = LoginModel.Password
            };

            var result = await AuthClient.LoginAsync(request);

            if (result.HttpResponse.IsSuccessStatusCode && result.Response is not null)
            {
                await AuthStateProvider.SetTokenAsync(result.Response.AccessToken);

                // ✅ replace:true evita volver al login con "atrás"
                Navigation.NavigateTo("/", replace: true);
            }
            else
            {
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
