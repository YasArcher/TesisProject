using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class RegistrationMatrixConfiguration : IEntityTypeConfiguration<RegistrationMatrix>
{
    public void Configure(EntityTypeBuilder<RegistrationMatrix> entity)
    {
        entity.ToTable("RegistrationMatrix");
        entity.HasKey(x => x.RegistrationMatrixId);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.Property(x => x.Notes).HasMaxLength(1000);
        entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
        entity.HasIndex(x => x.CreatedByUserId);
        entity.HasMany(x => x.Columns).WithOne(x => x.Matrix).HasForeignKey(x => x.RegistrationMatrixId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(x => x.Rows).WithOne(x => x.Matrix).HasForeignKey(x => x.RegistrationMatrixId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RegistrationMatrixColumnConfiguration : IEntityTypeConfiguration<RegistrationMatrixColumn>
{
    public void Configure(EntityTypeBuilder<RegistrationMatrixColumn> entity)
    {
        entity.ToTable("RegistrationMatrixColumn");
        entity.HasKey(x => x.RegistrationMatrixColumnId);
        entity.Property(x => x.WidthUnits).HasDefaultValue(1);
        entity.HasIndex(x => new { x.RegistrationMatrixId, x.FieldId }).IsUnique();
        entity.HasOne(x => x.Field).WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RegistrationMatrixRowConfiguration : IEntityTypeConfiguration<RegistrationMatrixRow>
{
    public void Configure(EntityTypeBuilder<RegistrationMatrixRow> entity)
    {
        entity.ToTable("RegistrationMatrixRow");
        entity.HasKey(x => x.RegistrationMatrixRowId);
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.HasIndex(x => new { x.RegistrationMatrixId, x.RowNumber }).IsUnique();
        entity.HasMany(x => x.Cells).WithOne(x => x.Row).HasForeignKey(x => x.RegistrationMatrixRowId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RegistrationMatrixCellConfiguration : IEntityTypeConfiguration<RegistrationMatrixCell>
{
    public void Configure(EntityTypeBuilder<RegistrationMatrixCell> entity)
    {
        entity.ToTable("RegistrationMatrixCell");
        entity.HasKey(x => x.RegistrationMatrixCellId);
        entity.Property(x => x.RawValue).HasMaxLength(4000);
        entity.HasIndex(x => new { x.RegistrationMatrixRowId, x.FieldId }).IsUnique();
        entity.HasOne(x => x.Field).WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
    }
}
