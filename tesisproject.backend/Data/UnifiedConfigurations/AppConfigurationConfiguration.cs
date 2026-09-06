using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class AppConfigurationConfiguration : IEntityTypeConfiguration<AppConfiguration>
{
    public void Configure(EntityTypeBuilder<AppConfiguration> entity)
    {
        entity.ToTable("AppConfigurations");

        entity.HasKey(x => x.AppConfigurationId);

        entity.HasIndex(x => new { x.Module, x.SettingKey })
              .IsUnique();

        entity.Property(x => x.Module)
              .HasMaxLength(50)
              .IsRequired();

        entity.Property(x => x.SettingKey)
              .HasMaxLength(100)
              .IsRequired();

        entity.Property(x => x.SettingValue)
              .HasMaxLength(1000)
              .IsRequired();

        entity.Property(x => x.DataType)
              .HasConversion<string>()
              .HasMaxLength(20)
              .IsRequired();

        entity.Property(x => x.MinValue)
              .HasMaxLength(100);

        entity.Property(x => x.MaxValue)
              .HasMaxLength(100);

        entity.Property(x => x.Description)
              .HasMaxLength(250);

        entity.Property(x => x.CreatedAt)
              .HasColumnType("datetime2");

        entity.Property(x => x.UpdatedAt)
              .HasColumnType("datetime2");
    }
}
