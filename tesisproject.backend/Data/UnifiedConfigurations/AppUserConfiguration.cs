using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> entity)
    {
        entity.HasKey(u => u.IdUser);

        entity.HasIndex(u => u.IdLocal).IsUnique().HasFilter("[IdLocal] IS NOT NULL");
        entity.HasIndex(u => u.IdAsp).IsUnique().HasFilter("[IdAsp] IS NOT NULL");

        entity.HasOne<IdentityUser<int>>().WithMany().HasForeignKey(x => x.IdLocal).OnDelete(DeleteBehavior.NoAction);
    }
}
