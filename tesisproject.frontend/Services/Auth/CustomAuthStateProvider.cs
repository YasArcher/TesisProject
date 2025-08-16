using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ITokenStore _tokenStore;

        public CustomAuthStateProvider(ITokenStore tokenStore) => _tokenStore = tokenStore;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _tokenStore.GetAsync();
            if (string.IsNullOrWhiteSpace(token)) return Anonymous();

            try
            {
                var claims = ParseClaimsFromJwt(token).ToList();

                // exp opcional
                var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                if (long.TryParse(exp, out var expUnix))
                {
                    if (DateTimeOffset.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(expUnix))
                        return Anonymous();
                }

                var identity = new ClaimsIdentity(claims, "jwt");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                return Anonymous();
            }
        }

        public void NotifyUserAuthentication(string token)
        {
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        }

        public void NotifyUserLogout() =>
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));

        private static AuthenticationState Anonymous() =>
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) yield break;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var kv = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (kv is null) yield break;
            foreach (var (k, v) in kv)
            {
                if (v is JsonElement je && je.ValueKind == JsonValueKind.Array)
                    foreach (var item in je.EnumerateArray()) yield return new Claim(k, item.ToString());
                else
                    yield return new Claim(k, v?.ToString() ?? "");
            }
        }
    }
}