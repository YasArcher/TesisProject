using Microsoft.AspNetCore.Components.Authorization;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Implementations;
public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStore _store;
    private readonly IAuthClient _auth;

    public JwtAuthStateProvider(ITokenStore store, IAuthClient auth)
    {
        _store = store;
        _auth = auth;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _store.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // anónimo

        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken? jwt = null;
        try { jwt = handler.ReadJwtToken(token); } catch { }

        if (jwt is null || jwt.ValidTo <= DateTime.UtcNow)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private async Task EnrichFromMeAsync()
    {
        try
        {
            var me = await _auth.GetCurrentAsync();
            if (me is null) return;

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, me.Email ?? string.Empty),
                    new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(me.FullName) ? (me.Email ?? string.Empty) : me.FullName)
                };
            if (me.Roles is not null)
                claims.AddRange(me.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "jwt");
            var enriched = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(enriched)));
        }
        catch { /* si /me falla, mantenemos el estado por JWT */ }
    }

    public void NotifyUserChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static AuthenticationState Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));
}