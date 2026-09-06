using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProductAuthorConfiguration : IEntityTypeConfiguration<ProductAuthor>
{
    public void Configure(EntityTypeBuilder<ProductAuthor> entity)
    {
        entity.HasIndex(x => new { x.ProductId, x.AuthorId }).IsUnique();

        entity.HasOne(x => x.Product)
         .WithMany(p => p.Authors!)
         .HasForeignKey(x => x.ProductId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.Author).WithMany(x => x.Products).HasForeignKey(x => x.AuthorId);
        entity.HasIndex(x => new { x.ProductId, x.AuthorOrder }).IsUnique().HasFilter("[AuthorOrder] IS NOT NULL");
        entity.Property(x => x.Participation).HasMaxLength(150);
        entity.Property(x => x.ParticipantTypeSnapshot).HasMaxLength(50);
        entity.Property(x => x.AffiliationSnapshot).HasMaxLength(300);
        entity.Property(x => x.NameSnapshot).HasMaxLength(300);
        entity.Property(x => x.IdentificationSnapshot).HasMaxLength(100);
        entity.Property(x => x.EmailSnapshot).HasMaxLength(200);
        entity.ToTable("ProductAuthors", t => t.HasCheckConstraint("CK_ProductAuthors_PositiveOrder",
            "[AuthorOrder] IS NULL OR [AuthorOrder] > 0"));
    }
}
