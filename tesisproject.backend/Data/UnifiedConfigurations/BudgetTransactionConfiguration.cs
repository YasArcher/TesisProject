using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class BudgetTransactionConfiguration : IEntityTypeConfiguration<BudgetTransaction>
{
    public void Configure(EntityTypeBuilder<BudgetTransaction> entity)
    {
        entity.HasIndex(x => x.CertifiedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CertifiedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => x.ExecutedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ExecutedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
