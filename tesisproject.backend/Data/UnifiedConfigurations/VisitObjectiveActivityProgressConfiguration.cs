using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class VisitObjectiveActivityProgressConfiguration : IEntityTypeConfiguration<VisitObjectiveActivityProgress>
{
    public void Configure(EntityTypeBuilder<VisitObjectiveActivityProgress> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.VisitId);
        entity.HasIndex(x => x.ObjectiveActivityId);

        entity.HasIndex(x => new { x.VisitId, x.ObjectiveActivityId }).IsUnique();

        entity.HasOne(x => x.ObjectiveActivity)
         .WithMany(a => a.VisitProgresses)
         .HasForeignKey(x => x.ObjectiveActivityId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.Visit)
         .WithMany(v => v.ActivityProgresses)
         .HasForeignKey(x => x.VisitId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
