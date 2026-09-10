using tesisproject.backend.Services.Interfaces;

namespace tesisproject.backend.Services.Unified.Interfaces;

// Opt-in contract: legacy HTTP registration remains bound to its existing service.
public interface IUnifiedArticleRegistrationCommandService : IArticleRegistrationCommandService { }
