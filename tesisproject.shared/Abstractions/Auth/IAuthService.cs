using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.shared.Abstractions.Auth
{
    public interface IAuthService
    {
        Task<LoguinResponse> RegisterAsync(RegisterUserRequest request);
        Task<LoguinResponse> LoginAsync(LoguinRequest request);
        Task<AuthMeResponse> MeAsync(ClaimsPrincipal user);
        Task<AuthMeResponse> AcceptTermsAsync(ClaimsPrincipal user, AcceptTermsRequest request, CancellationToken ct = default);
    }
}
