using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.ReadModels;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ArticleReadModelConfiguration : IEntityTypeConfiguration<ArticleReadModel>
{
    public void Configure(EntityTypeBuilder<ArticleReadModel> entity)
    {
        entity.HasNoKey();
        entity.ToView("ArticleReadView", "dbo");
        entity.Property(x => x.Sjr).HasPrecision(18, 6);
    }
}
