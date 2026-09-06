using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedConfigurations;

namespace tesisproject.backend.Data;

/// <summary>Complete operational DIDE model. Application DI still uses the original contexts.</summary>
public sealed partial class UnifiedDideDbContext
    : IdentityDbContext<IdentityUser<int>, IdentityRole<int>, int>
{
    public UnifiedDideDbContext(DbContextOptions<UnifiedDideDbContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("dbo");
        builder.ApplyConfigurationsFromAssembly(typeof(UnifiedDideDbContext).Assembly,
            type => type.Namespace == typeof(UnifiedModelConfigurationMarker).Namespace);
        DisableCascadeDeletesGlobally(builder);
    }

    private static void DisableCascadeDeletesGlobally(ModelBuilder builder)
    {
        foreach (var fk in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
