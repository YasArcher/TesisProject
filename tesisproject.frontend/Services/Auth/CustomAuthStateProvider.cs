using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Auth;
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStore _store;
    public CustomAuthStateProvider(ITokenStore store) => _store = store;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _store.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public Task MarkUserAsAuthenticated(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        return Task.CompletedTask;
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _store.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }
}