using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> entity)
    {
        entity.HasOne(x => x.Product).WithOne(x => x.Article)
                    .HasForeignKey<Article>(x => x.ProductId).IsRequired();

        entity.HasIndex(x => x.ProductId).IsUnique();

        entity.Property(x => x.Doi).HasMaxLength(200);
        entity.Property(x => x.PublicationUrl).HasMaxLength(500);
        entity.Property(x => x.ProceedingsName).HasMaxLength(300);
        entity.Property(x => x.Proceedings).HasMaxLength(300);
        entity.Property(x => x.EventName).HasMaxLength(300);
        entity.Property(x => x.GroupName).HasMaxLength(300);
        entity.Property(x => x.Filiacion).HasMaxLength(300);
        entity.Property(x => x.ExternalSource).HasMaxLength(50);
        entity.Property(x => x.ExternalId).HasMaxLength(150);
        entity.Property(x => x.IsOpenAccess).HasDefaultValue(false);

        entity.HasOne(x => x.AcademicTerm)
            .WithMany(t => t.Articles)
            .HasForeignKey(x => x.AcademicTermId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(x => x.PublicationStatus)
            .WithMany(s => s.Articles)
            .HasForeignKey(x => x.PublicationStatusId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(x => x.ResearchLine)
            .WithMany(r => r.Articles)
            .HasForeignKey(x => x.ResearchLineId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(x => x.BroadField)
            .WithMany()
            .HasForeignKey(x => x.BroadFieldId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.SpecificField)
            .WithMany()
            .HasForeignKey(x => x.SpecificFieldId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.DetailedField)
            .WithMany()
            .HasForeignKey(x => x.DetailedFieldId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.Faculty)
            .WithMany(f => f.Articles)
            .HasForeignKey(x => x.FacultyId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(x => x.Venue)
            .WithMany(v => v.Articles)
            .HasForeignKey(x => x.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(a => a.Doi)
            .HasDatabaseName("UX_Articles_Doi_NotBlank")
            .IsUnique()
            .HasFilter("[Doi] IS NOT NULL AND [Doi] <> N''");

        entity.HasIndex(a => new { a.ExternalSource, a.ExternalId })
            .HasDatabaseName("IX_Articles_ExternalSource_ExternalId")
            .HasFilter("[ExternalSource] IS NOT NULL AND [ExternalId] IS NOT NULL");
    }
}
