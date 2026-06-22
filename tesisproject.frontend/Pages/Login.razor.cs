using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using tesisproject.frontend.Services.Auth;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Utils;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Pages
{
    public partial class Login
    {
        public LoginModel LoginModel { get; set; } = new();

        [Inject] public IAuthClientService AuthClient { get; set; } = null!;
        [Inject] public CustomAuthStateProvider AuthStateProvider { get; set; } = null!;
        [Inject] public NavigationManager Navigation { get; set; } = null!;
        [Inject] public IToastService Toast { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var session = await AuthClient.GetCurrentSessionAsync();
                Navigation.NavigateTo(GetLandingRoute(session.Data), replace: true);
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

            if (result.Success && result.Data is not null)
            {
                await AuthStateProvider.SetTokenAsync(result.Data.AccessToken);

                Toast.ShowSuccess(result.ToSuccessMessage("Acceso correcto"));
                // [ARTICLES-MIGRATION] El mismo login dirige al portal permitido por la sesion.
                var session = await AuthClient.GetCurrentSessionAsync();
                Navigation.NavigateTo(GetLandingRoute(session.Data), replace: true);
            }
            else
            {
                var errorMessage = result.ToErrorMessage("Credenciales no válidas");
                Toast.ShowError(errorMessage);
                Console.WriteLine(errorMessage);
            }
        }

        private static string GetLandingRoute(CurrentSessionResponse? session)
        {
            if (session is null)
                return "/";

            var projectAccess = session.Roles.Any(role =>
                role.Equals("coordinador", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("technical", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("superadmin", StringComparison.OrdinalIgnoreCase));

            var articleAccess = session.Articles.Enabled &&
                (session.Articles.CanAccess || session.Articles.CanList);

            if (articleAccess && !projectAccess)
                return "/articles";

            if (projectAccess && !articleAccess)
                return "/projects/home";

            return "/";
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
