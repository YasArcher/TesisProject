using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Data.ReadModels;

/// <summary>Canonical view values plus the structural graph needed by the existing Article DTOs.</summary>
public sealed record ArticleReadAggregate(ArticleReadModel Read, Product Product);
