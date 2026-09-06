using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class VisitIssueConfiguration : IEntityTypeConfiguration<VisitIssue>
{
    public void Configure(EntityTypeBuilder<VisitIssue> entity)
    {
        entity.HasOne(vi => vi.Visit)
         .WithMany(v => v.Issues)
         .HasForeignKey(vi => vi.VisitId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => x.ReportedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReportedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
