using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;

namespace tesisproject.backend.Repositories.Unified.Implementations;

/// <summary>DI constructor adapter for the shared GenericRepository; no additional CRUD behavior.</summary>
public sealed class UnifiedGenericRepository<T>(UnifiedDideDbContext context) : GenericRepository<T>(context)
    where T : class;
