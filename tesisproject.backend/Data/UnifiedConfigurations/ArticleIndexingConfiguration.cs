using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ArticleIndexingConfiguration : IEntityTypeConfiguration<ArticleIndexing>
{
    public void Configure(EntityTypeBuilder<ArticleIndexing> entity)
    {
        entity.HasKey(x => new { x.ArticleId, x.IndexingSourceId });

        entity.HasOne(x => x.Article)
            .WithMany(a => a.Indexings)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.IndexingSource)
            .WithMany()
            .HasForeignKey(x => x.IndexingSourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
