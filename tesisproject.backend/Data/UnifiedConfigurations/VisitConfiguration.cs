using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> entity)
    {
        entity.HasOne(v => v.Document)
         .WithMany()
         .HasForeignKey(v => v.DocumentId)
         .OnDelete(DeleteBehavior.NoAction)
         .HasConstraintName("FK_Visits_Documents_DocumentId");

        entity.HasOne(v => v.FundingDocument)
         .WithMany()
         .HasForeignKey(v => v.FundingDocumentId)
         .OnDelete(DeleteBehavior.NoAction)
         .HasConstraintName("FK_Visits_Documents_FundingDocumentId");

        entity.HasOne(v => v.ProgressDocument)
         .WithMany()
         .HasForeignKey(v => v.ProgressDocumentId)
         .OnDelete(DeleteBehavior.NoAction)
         .HasConstraintName("FK_Visits_Documents_ProgressDocumentId");

        entity.HasOne(v => v.Project)
         .WithMany(p => p.Visits)
         .HasForeignKey(v => v.ProjectId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(v => v.VisitState)
         .WithMany()
         .HasForeignKey(v => v.VisitStateId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(v => v.DocumentId);
        entity.HasIndex(v => v.FundingDocumentId);
        entity.HasIndex(v => v.ProgressDocumentId);
        entity.HasIndex(v => v.ProjectId);
        entity.HasIndex(v => v.VisitStateId);

        entity.HasOne<AcademicTerm>().WithMany().HasForeignKey(x => x.AcademicTermId);

        entity.HasIndex(x => x.PerformedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.PerformedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
