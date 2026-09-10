using tesisproject.backend.Services.Interfaces;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedArticleUserContext : IArticleUserContext
{
    string? OwnerReference { get; }
    bool CanManageAll { get; }
}
