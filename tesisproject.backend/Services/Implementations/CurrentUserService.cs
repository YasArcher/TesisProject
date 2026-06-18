using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Utils;

namespace tesisproject.backend.Services.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user.GetUserId();
        }

        public int GetRequiredUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user.GetRequiredUserId();
        }
    }
}