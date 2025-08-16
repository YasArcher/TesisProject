using System.Net.Http.Headers;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly ITokenStore _tokenStore;

        public AuthMessageHandler(ITokenStore tokenStore) => _tokenStore = tokenStore;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenStore.GetAsync();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}