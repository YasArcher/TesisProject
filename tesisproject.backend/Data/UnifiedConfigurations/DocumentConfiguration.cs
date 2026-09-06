using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> entity)
    {
        entity.HasIndex(d => new { d.DocumentTypeId, d.ResolutionCode })
         .IsUnique()
         .HasDatabaseName("UX_Documents_Type1_ResolutionCode")
         .HasFilter("[ResolutionCode] IS NOT NULL AND [DocumentTypeId] = 1");

        entity.HasIndex(x => x.CreatedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => x.UpdatedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
