using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> entity)
    {
        entity.HasKey(x => x.AuthorId);
        entity.HasOne(x => x.AppUser).WithOne().HasForeignKey<Author>(x => x.AppUserId);
        entity.HasOne(x => x.ExternalResearcher).WithOne().HasForeignKey<Author>(x => x.ExternalResearcherId);
        entity.HasIndex(x => x.AppUserId).IsUnique().HasFilter("[AppUserId] IS NOT NULL");
        entity.HasIndex(x => x.ExternalResearcherId).IsUnique().HasFilter("[ExternalResearcherId] IS NOT NULL");
        entity.Property(x => x.Orcid).HasMaxLength(50);
        entity.HasIndex(x => x.Orcid).IsUnique().HasFilter("[Orcid] IS NOT NULL AND [Orcid] <> N''");
        entity.ToTable("Authors", t => t.HasCheckConstraint("CK_Authors_ExactlyOneSource",
            "([AppUserId] IS NOT NULL AND [ExternalResearcherId] IS NULL) OR ([AppUserId] IS NULL AND [ExternalResearcherId] IS NOT NULL)"));

        entity.Property(x => x.ExternalAuthorId).HasMaxLength(150);
    }
}
