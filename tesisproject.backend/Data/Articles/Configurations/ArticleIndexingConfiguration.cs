// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Articles.Entities;

namespace tesisproject.backend.Data.Articles.Configurations;

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
            .WithMany(s => s.ArticleIndexings)
            .HasForeignKey(x => x.IndexingSourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

