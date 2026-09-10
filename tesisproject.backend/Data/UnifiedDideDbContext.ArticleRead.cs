using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.ReadModels;

namespace tesisproject.backend.Data;

public sealed partial class UnifiedDideDbContext
{
    public DbSet<ArticleReadModel> ArticleReads => Set<ArticleReadModel>();
}
