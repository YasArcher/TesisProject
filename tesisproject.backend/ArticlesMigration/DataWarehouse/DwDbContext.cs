using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.DataWarehouse.Entities;

namespace tesisproject.backend.DataWarehouse
{
    public class DwDbContext : DbContext
    {
        public DwDbContext(DbContextOptions<DwDbContext> options) : base(options)
        {
        }

        public DbSet<DimDate> DimDates => Set<DimDate>();
        public DbSet<DimAcademicTerm> DimAcademicTerms => Set<DimAcademicTerm>();
        public DbSet<DimVenue> DimVenues => Set<DimVenue>();
        public DbSet<DimField> DimFields => Set<DimField>();
        public DbSet<DimResearchLine> DimResearchLines => Set<DimResearchLine>();
        public DbSet<DimPublicationStatus> DimPublicationStatuses => Set<DimPublicationStatus>();
        public DbSet<DimIndexingSource> DimIndexingSources => Set<DimIndexingSource>();
        public DbSet<DimProject> DimProjects => Set<DimProject>();
        public DbSet<DimArticle> DimArticles => Set<DimArticle>();
        public DbSet<DimAuthor> DimAuthors => Set<DimAuthor>();

        public DbSet<FactArticlePublication> FactArticlePublications => Set<FactArticlePublication>();
        public DbSet<FactArticleIndexing> FactArticleIndexings => Set<FactArticleIndexing>();
        public DbSet<FactVenueMetricYear> FactVenueMetricYears => Set<FactVenueMetricYear>();
        public DbSet<FactArticleAuthor> FactArticleAuthors => Set<FactArticleAuthor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureDimDate(modelBuilder.Entity<DimDate>());
            ConfigureDimAcademicTerm(modelBuilder.Entity<DimAcademicTerm>());
            ConfigureDimVenue(modelBuilder.Entity<DimVenue>());
            ConfigureDimField(modelBuilder.Entity<DimField>());
            ConfigureDimResearchLine(modelBuilder.Entity<DimResearchLine>());
            ConfigureDimPublicationStatus(modelBuilder.Entity<DimPublicationStatus>());
            ConfigureDimIndexingSource(modelBuilder.Entity<DimIndexingSource>());
            ConfigureDimProject(modelBuilder.Entity<DimProject>());
            ConfigureDimArticle(modelBuilder.Entity<DimArticle>());
            ConfigureDimAuthor(modelBuilder.Entity<DimAuthor>());

            ConfigureFactArticlePublication(modelBuilder.Entity<FactArticlePublication>());
            ConfigureFactArticleIndexing(modelBuilder.Entity<FactArticleIndexing>());
            ConfigureFactVenueMetricYear(modelBuilder.Entity<FactVenueMetricYear>());
            ConfigureFactArticleAuthor(modelBuilder.Entity<FactArticleAuthor>());
        }

        #region Dim Config

        private static void ConfigureDimDate(EntityTypeBuilder<DimDate> e)
        {
            e.HasKey(d => d.DateKey);
            e.Property(d => d.DateKey).ValueGeneratedNever();
        }

        private static void ConfigureDimAcademicTerm(EntityTypeBuilder<DimAcademicTerm> e)
        {
            e.HasKey(x => x.AcademicTermKey);
            e.HasIndex(x => x.AcademicTermId);
        }

        private static void ConfigureDimVenue(EntityTypeBuilder<DimVenue> e)
        {
            e.HasKey(x => x.VenueKey);
            e.HasIndex(x => x.VenueId);
        }

        private static void ConfigureDimField(EntityTypeBuilder<DimField> e)
        {
            e.HasKey(x => x.FieldKey);
            e.HasIndex(x => x.DetailedFieldId);
        }

        private static void ConfigureDimResearchLine(EntityTypeBuilder<DimResearchLine> e)
        {
            e.HasKey(x => x.ResearchLineKey);
            e.HasIndex(x => x.ResearchLineId);
        }

        private static void ConfigureDimPublicationStatus(EntityTypeBuilder<DimPublicationStatus> e)
        {
            e.HasKey(x => x.PublicationStatusKey);
            e.HasIndex(x => x.PublicationStatusId);
        }

        private static void ConfigureDimIndexingSource(EntityTypeBuilder<DimIndexingSource> e)
        {
            e.HasKey(x => x.IndexingSourceKey);
            e.HasIndex(x => x.IndexingSourceId);
        }

        private static void ConfigureDimProject(EntityTypeBuilder<DimProject> e)
        {
            e.HasKey(x => x.ProjectKey);
            e.HasIndex(x => x.ProjectId);
        }

        private static void ConfigureDimArticle(EntityTypeBuilder<DimArticle> e)
        {
            e.HasKey(x => x.ArticleKey);
            e.HasIndex(x => x.ArticleId);
        }

        private static void ConfigureDimAuthor(EntityTypeBuilder<DimAuthor> e)
        {
            e.HasKey(x => x.AuthorKey);
        }

        #endregion

        #region Fact Config

        private static void ConfigureFactArticlePublication(EntityTypeBuilder<FactArticlePublication> e)
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.ArticleKey);
            e.HasIndex(x => x.CreatedDateKey);
            e.HasIndex(x => x.PublicationDateKey);

            // 👉 Precisión de SJR
            e.Property(x => x.SJR)
             .HasPrecision(10, 4);

            e.HasOne(x => x.Article)
             .WithMany()
             .HasForeignKey(x => x.ArticleKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Venue)
             .WithMany()
             .HasForeignKey(x => x.VenueKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Field)
             .WithMany()
             .HasForeignKey(x => x.FieldKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ResearchLine)
             .WithMany()
             .HasForeignKey(x => x.ResearchLineKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PublicationStatus)
             .WithMany()
             .HasForeignKey(x => x.PublicationStatusKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Project)
             .WithMany()
             .HasForeignKey(x => x.ProjectKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AcademicTerm)
             .WithMany()
             .HasForeignKey(x => x.AcademicTermKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CreatedDate)
             .WithMany()
             .HasForeignKey(x => x.CreatedDateKey)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PublicationDate)
             .WithMany()
             .HasForeignKey(x => x.PublicationDateKey)
             .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigureFactArticleIndexing(EntityTypeBuilder<FactArticleIndexing> e)
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.ArticleKey, x.IndexingSourceKey }).IsUnique();

            e.HasOne(x => x.Article)
             .WithMany()
             .HasForeignKey(x => x.ArticleKey)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.IndexingSource)
             .WithMany()
             .HasForeignKey(x => x.IndexingSourceKey)
             .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureFactVenueMetricYear(EntityTypeBuilder<FactVenueMetricYear> e)
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.VenueKey, x.Year }).IsUnique();

            // 👉 Precisión de SJR
            e.Property(x => x.SJR)
             .HasPrecision(10, 4);

            e.HasOne(x => x.Venue)
             .WithMany()
             .HasForeignKey(x => x.VenueKey)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.YearDate)
             .WithMany()
             .HasForeignKey(x => x.YearDateKey)
             .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigureFactArticleAuthor(EntityTypeBuilder<FactArticleAuthor> e)
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.ArticleKey, x.AuthorKey }).IsUnique();

            e.HasOne(x => x.Article)
             .WithMany()
             .HasForeignKey(x => x.ArticleKey)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Author)
             .WithMany()
             .HasForeignKey(x => x.AuthorKey)
             .OnDelete(DeleteBehavior.Cascade);
        }

        #endregion
    }
}
