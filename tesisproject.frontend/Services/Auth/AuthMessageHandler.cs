namespace tesisproject.frontend.Services.Auth;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;




public class AuthMessageHandler : DelegatingHandler
{
    private readonly ITokenStore _store;

    public AuthMessageHandler(ITokenStore store)
    {
        _store = store;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // No agregues Authorization al endpoint de login/register
        var path = request.RequestUri?.AbsolutePath?.ToLowerInvariant() ?? string.Empty;
        var isAuthEndpoint = path.StartsWith("/api/auth/login") || path.StartsWith("/api/auth/register");

        var token = await _store.GetAsync();
        if (!isAuthEndpoint && !string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}