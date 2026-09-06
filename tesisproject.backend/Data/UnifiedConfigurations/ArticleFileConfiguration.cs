using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ArticleFileConfiguration : IEntityTypeConfiguration<ArticleFile>
{
    public void Configure(EntityTypeBuilder<ArticleFile> entity)
    {
        entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        entity.Property(x => x.FileUrl).HasMaxLength(500);
        entity.Property(x => x.Sha256).HasMaxLength(64);

        entity.HasOne(x => x.Article)
            .WithMany(a => a.Files)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
