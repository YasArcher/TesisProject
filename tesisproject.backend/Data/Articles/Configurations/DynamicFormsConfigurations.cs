using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Articles.Entities;

namespace tesisproject.backend.Data.Articles.Configurations;

public sealed class FieldCatalogEntryConfiguration : IEntityTypeConfiguration<FieldCatalogEntry>
{
    public void Configure(EntityTypeBuilder<FieldCatalogEntry> entity)
    {
        entity.ToTable("FieldCatalog");
        entity.HasKey(x => x.FieldId);
        entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
        entity.Property(x => x.FieldLabel).HasMaxLength(150).IsRequired();
        entity.Property(x => x.DataType).HasMaxLength(50).IsRequired();
        entity.Property(x => x.SourceType).HasMaxLength(30).IsRequired();
        entity.Property(x => x.PhysicalTableName).HasMaxLength(100);
        entity.Property(x => x.PhysicalColumnName).HasMaxLength(100);
        entity.Property(x => x.ReferenceTableName).HasMaxLength(100);
        entity.Property(x => x.Placeholder).HasMaxLength(200);
        entity.Property(x => x.HelpText).HasMaxLength(500);
        entity.Property(x => x.DefaultValue).HasMaxLength(200);
        entity.Property(x => x.ValidationRule).HasMaxLength(500);
    }
}

public sealed class DynamicFieldOptionConfiguration : IEntityTypeConfiguration<DynamicFieldOption>
{
    public void Configure(EntityTypeBuilder<DynamicFieldOption> entity)
    {
        entity.ToTable("DynamicFieldOptions");
        entity.HasKey(x => x.DynamicFieldOptionId);
        entity.Property(x => x.OptionValue).HasMaxLength(200).IsRequired();
        entity.Property(x => x.OptionLabel).HasMaxLength(200).IsRequired();

        entity.HasOne(x => x.Field)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public void Configure(EntityTypeBuilder<FormDefinition> entity)
    {
        entity.ToTable("FormDefinitions");
        entity.HasKey(x => x.FormId);
        entity.Property(x => x.FormKey).HasMaxLength(100).IsRequired();
        entity.Property(x => x.FormName).HasMaxLength(150).IsRequired();
        entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(500);
    }
}

public sealed class FormFieldDefinitionConfiguration : IEntityTypeConfiguration<FormFieldDefinition>
{
    public void Configure(EntityTypeBuilder<FormFieldDefinition> entity)
    {
        entity.ToTable("FormFields");
        entity.HasKey(x => x.FormFieldId);
        entity.Property(x => x.GroupName).HasMaxLength(100);

        entity.HasOne(x => x.Form)
            .WithMany(x => x.Fields)
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Field)
            .WithMany(x => x.FormFields)
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DynamicFieldValueConfiguration : IEntityTypeConfiguration<DynamicFieldValue>
{
    public void Configure(EntityTypeBuilder<DynamicFieldValue> entity)
    {
        entity.ToTable("DynamicFieldValues");
        entity.HasKey(x => x.DynamicFieldValueId);
        entity.Property(x => x.ValueDecimal).HasPrecision(18, 4);

        entity.HasOne(x => x.Article)
            .WithMany(x => x.DynamicFieldValues)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Field)
            .WithMany()
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ArticleParticipantDynamicFieldValueConfiguration : IEntityTypeConfiguration<ArticleParticipantDynamicFieldValue>
{
    public void Configure(EntityTypeBuilder<ArticleParticipantDynamicFieldValue> entity)
    {
        entity.ToTable("ArticleParticipantDynamicFieldValues");
        entity.HasKey(x => x.ArticleParticipantDynamicFieldValueId);
        entity.Property(x => x.ValueDecimal).HasPrecision(18, 4);

        entity.HasOne(x => x.ArticleParticipant)
            .WithMany(x => x.DynamicFieldValues)
            .HasForeignKey(x => x.ArticleParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Field)
            .WithMany()
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
