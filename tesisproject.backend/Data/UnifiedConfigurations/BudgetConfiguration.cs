using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> entity)
    {
        entity.HasOne(bu => bu.FundingType)
         .WithMany()
         .HasForeignKey(bu => bu.FundingTypeId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => x.ApprovedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ApprovedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
