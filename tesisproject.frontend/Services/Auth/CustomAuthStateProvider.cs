using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ITokenStore _tokenStore;
        private readonly IAuthClientService _authClient;

        public CustomAuthStateProvider(ITokenStore tokenStore, IAuthClientService authClient)
        {
            _tokenStore = tokenStore;
            _authClient = authClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _tokenStore.GetAsync();
            if (string.IsNullOrWhiteSpace(token)) return Anonymous();

            try
            {
                var claims = ParseClaimsFromJwt(token);

                var expStr = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                if (long.TryParse(expStr, out var expUnix))
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (now >= expUnix)
                    {
                        var refreshResult = await _authClient.RefreshAsync();

                        if (refreshResult.Success &&
                            refreshResult.Data is not null)
                        {
                            var newToken = refreshResult.Data.AccessToken;

                            await _tokenStore.SetAsync(newToken);

                            var newClaims = ParseClaimsFromJwt(newToken);
                            var newIdentity = new ClaimsIdentity(
                                newClaims,
                                authenticationType: "jwt",
                                nameType: "name",
                                roleType: "role"
                            );

                            return new AuthenticationState(
                                new ClaimsPrincipal(newIdentity));
                        }

                        await _tokenStore.ClearAsync();
                        NotifyUserLogout();
                        return Anonymous();
                    }
                }

                var identity = new ClaimsIdentity(
                    claims,
                    authenticationType: "jwt",
                    nameType: "name",
                    roleType: "role"
                );

                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                await _tokenStore.ClearAsync();
                NotifyUserLogout();
                return Anonymous();
            }
        }

        public async Task SetTokenAsync(string token)
        {
            await _tokenStore.SetAsync(token);
            NotifyUserAuthentication(token);
        }

        public async Task LogoutAsync()
        {
            await _tokenStore.ClearAsync();
            NotifyUserLogout();
        }

        public void NotifyUserAuthentication(string token)
        {
            var identity = new ClaimsIdentity(
                ParseClaimsFromJwt(token),
                authenticationType: "jwt",
                nameType: "name",
                roleType: "role"
            );

            var authState = new AuthenticationState(new ClaimsPrincipal(identity));
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public void NotifyUserLogout() =>
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));

        private static AuthenticationState Anonymous() =>
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        private static List<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3) return claims;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var kv = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (kv is null) return claims;

                foreach (var (k, v) in kv)
                {
                    if (v is JsonElement je && je.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in je.EnumerateArray())
                            claims.Add(new Claim(k, item.ToString()));
                    }
                    else
                    {
                        claims.Add(new Claim(k, v?.ToString() ?? string.Empty));
                    }
                }

                return claims;
            }
            catch
            {
                return new List<Claim>();
            }
        }
    }
}