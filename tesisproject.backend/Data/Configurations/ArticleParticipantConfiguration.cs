using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Configurations;

public sealed class ArticleParticipantConfiguration : IEntityTypeConfiguration<ArticleParticipant>
{
    public void Configure(EntityTypeBuilder<ArticleParticipant> entity)
    {
        entity.Property(x => x.Identificacion).HasMaxLength(100);
        entity.Property(x => x.Nombre).HasMaxLength(300);
        entity.Property(x => x.Participacion).HasMaxLength(150);
        entity.Property(x => x.ParticipantType).HasMaxLength(50);
        entity.Property(x => x.Email).HasMaxLength(200);
        entity.Property(x => x.Orcid).HasMaxLength(50);
        entity.Property(x => x.Affiliation).HasMaxLength(300);
        entity.Property(x => x.ExternalAuthorId).HasMaxLength(150);
        entity.Property(x => x.IsPrimaryAuthor).HasDefaultValue(false);

        entity.HasOne(x => x.Article)
            .WithMany(a => a.Participants)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.ArticleId, x.Index }).IsUnique();
    }
}
