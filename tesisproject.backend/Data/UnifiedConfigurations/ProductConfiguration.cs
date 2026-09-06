using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.Property(p => p.Title).HasMaxLength(1024).IsRequired();
        entity.Property(p => p.Description).HasMaxLength(4000);

        entity.HasOne(p => p.ProductType)
         .WithMany()
         .HasForeignKey(p => p.ProductTypeId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(p => p.Project)
         .WithMany(pr => pr.Products!)
         .HasForeignKey(p => p.ProjectId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(p => p.Visit)
         .WithMany(v => v.Products)
         .HasForeignKey(p => p.VisitId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(p => p.ProjectId);
        entity.HasIndex(p => p.VisitId);
        entity.HasIndex(p => p.ProductTypeId);
        entity.HasIndex(p => p.IsActive);
    }
}
