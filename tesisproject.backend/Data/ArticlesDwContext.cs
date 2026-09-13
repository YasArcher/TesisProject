using Microsoft.EntityFrameworkCore;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.backend.Data;

public sealed class ArticlesDwContext(DbContextOptions<ArticlesDwContext> options) : DbContext(options)
{
    public DbSet<DimDate> DimDates => Set<DimDate>();
    public DbSet<DimAuthor> DimAuthors => Set<DimAuthor>();
    public DbSet<DimJournal> DimJournals => Set<DimJournal>();
    public DbSet<DimProductType> DimProductTypes => Set<DimProductType>();
    public DbSet<DimFaculty> DimFaculties => Set<DimFaculty>();
    public DbSet<DimIndexingDatabase> DimIndexingDatabases => Set<DimIndexingDatabase>();
    public DbSet<DimQuartile> DimQuartiles => Set<DimQuartile>();
    public DbSet<DimArticle> DimArticles => Set<DimArticle>();
    public DbSet<DimVenue> DimVenues => Set<DimVenue>();
    public DbSet<DimAcademicTerm> DimAcademicTerms => Set<DimAcademicTerm>();
    public DbSet<DimPublicationStatus> DimPublicationStatuses => Set<DimPublicationStatus>();
    public DbSet<DimResearchLine> DimResearchLines => Set<DimResearchLine>();
    public DbSet<DimField> DimFields => Set<DimField>();
    public DbSet<DimIndexingSource> DimIndexingSources => Set<DimIndexingSource>();

    public DbSet<FactArticlePublication> FactArticlePublications => Set<FactArticlePublication>();
    public DbSet<FactArticleAuthor> FactArticleAuthors => Set<FactArticleAuthor>();
    public DbSet<FactArticleIndexing> FactArticleIndexings => Set<FactArticleIndexing>();
    public DbSet<FactVenueMetricYear> FactVenueMetricYears => Set<FactVenueMetricYear>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("ArticlesDW");

        // Shared CLR shapes are mapped independently in this context. Project-owned
        // navigation targets must never be discovered as part of ArticlesDW.
        modelBuilder.Ignore<FactProject>();
        modelBuilder.Ignore<FactBudget>();
        modelBuilder.Ignore<FactProduct>();
        modelBuilder.Ignore<BridgeProjectResearchCategory>();
        modelBuilder.Ignore<BridgeProductAuthor>();
        modelBuilder.Ignore<DimProjectState>();
        modelBuilder.Ignore<DimFundingType>();
        modelBuilder.Ignore<DimResearchCategory>();

        ConfigureConformedDimensions(modelBuilder);
        ConfigureArticleDimensions(modelBuilder);
        ConfigureFacts(modelBuilder);
        DisableCascadeDeletesGlobally(modelBuilder);
    }

    private static void ConfigureConformedDimensions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimDate>(b =>
        {
            b.ToTable("DimDates", "ArticlesDW");
            b.HasKey(x => x.DateKey);
            b.Property(x => x.DateKey).ValueGeneratedNever();
            b.Property(x => x.Date).IsRequired();
            b.Property(x => x.Year).IsRequired();
            b.Property(x => x.Month).IsRequired();
            b.Property(x => x.Day).IsRequired();
            b.HasIndex(x => x.Date);
            b.HasIndex(x => new { x.Year, x.Month });
        });

        modelBuilder.Entity<DimAuthor>(b =>
        {
            b.ToTable("DimAuthors", "ArticlesDW");
            b.HasKey(x => x.AuthorKey);
            b.HasIndex(x => x.AuthorId).IsUnique();
            b.HasIndex(x => x.IsInstitutional);
            b.HasIndex(x => x.AppUserId);
            b.HasIndex(x => x.IdAsp);
            b.HasIndex(x => x.ExternalResearcherId);
            b.Property(x => x.ExternalFullName).HasMaxLength(150);
            b.Property(x => x.Orcid).HasMaxLength(50);
        });

        modelBuilder.Entity<DimJournal>(b =>
        {
            b.ToTable("DimJournals", "ArticlesDW", table => table.HasCheckConstraint(
                "CK_DimJournals_Name_Trimmed",
                "[Name] <> N'' AND [Name] = LTRIM(RTRIM([Name]))"));
            b.HasKey(x => x.JournalKey);
            b.Property(x => x.Name).HasMaxLength(450).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<DimProductType>(b =>
        {
            b.ToTable("DimProductTypes", "ArticlesDW");
            b.HasKey(x => x.ProductTypeKey);
            b.HasIndex(x => x.ProductTypeId);
            b.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<DimFaculty>(b =>
        {
            b.ToTable("DimFaculties", "ArticlesDW");
            b.HasKey(x => x.FacultyKey);
            b.HasIndex(x => x.FacultyId);
            b.HasIndex(x => x.FacultyCode);
            b.HasIndex(x => x.FacultyName);
        });

        modelBuilder.Entity<DimIndexingDatabase>(b =>
        {
            b.ToTable("DimIndexingDatabases", "ArticlesDW");
            b.HasKey(x => x.IndexingDatabaseKey);
            b.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<DimQuartile>(b =>
        {
            b.ToTable("DimQuartiles", "ArticlesDW");
            b.HasKey(x => x.QuartileKey);
            b.HasIndex(x => x.Code);
        });
    }

    private static void ConfigureArticleDimensions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimArticle>(b =>
        {
            b.ToTable("DimArticles", "ArticlesDW");
            b.HasKey(x => x.ArticleKey);
            b.HasIndex(x => x.ProductId).IsUnique();
            b.HasIndex(x => x.ArticleId);
            b.HasIndex(x => x.ProductTypeId);
            b.Property(x => x.Title).HasMaxLength(1024).IsRequired();
            b.Property(x => x.Doi).HasMaxLength(450);
            b.Property(x => x.YearRaw).HasMaxLength(50);
            b.Property(x => x.IssnIsbn).HasMaxLength(100);
            b.Property(x => x.PublicationUrl).HasMaxLength(2048);
            b.Property(x => x.ExternalSource).HasMaxLength(50);
            b.Property(x => x.ExternalId).HasMaxLength(150);
            b.Property(x => x.ProceedingsName).HasMaxLength(300);
            b.Property(x => x.Proceedings).HasMaxLength(300);
            b.Property(x => x.EventName).HasMaxLength(300);
            b.Property(x => x.GroupName).HasMaxLength(300);
            b.Property(x => x.Filiacion).HasMaxLength(300);
        });

        modelBuilder.Entity<DimVenue>(b =>
        {
            b.ToTable("DimVenues", "ArticlesDW");
            b.HasKey(x => x.VenueKey);
            b.HasIndex(x => x.VenueId).IsUnique();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.IssnCode).HasMaxLength(100);
            b.Property(x => x.Issue).HasMaxLength(100);
            b.Property(x => x.Volume).HasMaxLength(100);
            b.Property(x => x.Url).HasMaxLength(2048);
            b.Property(x => x.Type).HasMaxLength(100);
        });

        modelBuilder.Entity<DimAcademicTerm>(b =>
        {
            b.ToTable("DimAcademicTerms", "ArticlesDW");
            b.HasKey(x => x.AcademicTermKey);
            b.HasIndex(x => x.AcademicTermId).IsUnique();
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<DimPublicationStatus>(b =>
        {
            b.ToTable("DimPublicationStatuses", "ArticlesDW");
            b.HasKey(x => x.PublicationStatusKey);
            b.HasIndex(x => x.PublicationStatusId).IsUnique();
            b.Property(x => x.Name).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<DimResearchLine>(b =>
        {
            b.ToTable("DimResearchLines", "ArticlesDW");
            b.HasKey(x => x.ResearchLineKey);
            b.HasIndex(x => x.ResearchLineId).IsUnique();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<DimField>(b =>
        {
            b.ToTable("DimFields", "ArticlesDW");
            b.HasKey(x => x.FieldKey);
            b.HasIndex(x => new { x.BroadFieldId, x.SpecificFieldId, x.DetailedFieldId }).IsUnique();
            b.Property(x => x.BroadFieldName).HasMaxLength(200).IsRequired();
            b.Property(x => x.SpecificFieldName).HasMaxLength(200);
            b.Property(x => x.DetailedFieldName).HasMaxLength(200);
        });

        modelBuilder.Entity<DimIndexingSource>(b =>
        {
            b.ToTable("DimIndexingSources", "ArticlesDW");
            b.HasKey(x => x.IndexingSourceKey);
            b.HasIndex(x => x.IndexingSourceId).IsUnique();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });
    }

    private static void ConfigureFacts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FactArticlePublication>(b =>
        {
            b.ToTable("FactArticlePublications", "ArticlesDW");
            b.HasKey(x => x.FactArticlePublicationId);
            b.HasIndex(x => x.ArticleKey).IsUnique();
            b.HasIndex(x => x.ProductId).IsUnique();
            b.HasIndex(x => x.ProductTypeKey);
            b.HasIndex(x => x.JournalKey);
            b.HasIndex(x => x.IndexingDatabaseKey);
            b.HasIndex(x => x.QuartileKey);
            b.HasIndex(x => x.VenueKey);
            b.HasIndex(x => x.AcademicTermKey);
            b.HasIndex(x => x.PublicationStatusKey);
            b.HasIndex(x => x.ResearchLineKey);
            b.HasIndex(x => x.FieldKey);
            b.HasIndex(x => x.ArticleFacultyKey);
            b.HasIndex(x => x.ProjectFacultyKey);
            b.HasIndex(x => x.ProjectId);
            b.HasIndex(x => x.CreatedDateKey);
            b.HasIndex(x => x.PublishedDateKey);
            b.Property(x => x.ArticleCount).HasDefaultValue(1);
            b.Property(x => x.AuthorCount).HasDefaultValue(0);
            b.Property(x => x.IndexingCount).HasDefaultValue(0);
            b.Property(x => x.Sjr).HasPrecision(18, 6);

            b.HasOne(x => x.Article).WithOne()
                .HasForeignKey<FactArticlePublication>(x => x.ArticleKey);
            b.HasOne(x => x.ProductType).WithMany().HasForeignKey(x => x.ProductTypeKey);
            b.HasOne(x => x.Journal).WithMany().HasForeignKey(x => x.JournalKey);
            b.HasOne(x => x.IndexingDatabase).WithMany().HasForeignKey(x => x.IndexingDatabaseKey);
            b.HasOne(x => x.Quartile).WithMany().HasForeignKey(x => x.QuartileKey);
            b.HasOne(x => x.Venue).WithMany().HasForeignKey(x => x.VenueKey);
            b.HasOne(x => x.AcademicTerm).WithMany().HasForeignKey(x => x.AcademicTermKey);
            b.HasOne(x => x.PublicationStatus).WithMany().HasForeignKey(x => x.PublicationStatusKey);
            b.HasOne(x => x.ResearchLine).WithMany().HasForeignKey(x => x.ResearchLineKey);
            b.HasOne(x => x.Field).WithMany().HasForeignKey(x => x.FieldKey);
            b.HasOne(x => x.ArticleFaculty).WithMany().HasForeignKey(x => x.ArticleFacultyKey);
            b.HasOne(x => x.ProjectFaculty).WithMany().HasForeignKey(x => x.ProjectFacultyKey);
            b.HasOne(x => x.CreatedDate).WithMany().HasForeignKey(x => x.CreatedDateKey);
            b.HasOne(x => x.PublishedDate).WithMany().HasForeignKey(x => x.PublishedDateKey);
        });

        modelBuilder.Entity<FactArticleAuthor>(b =>
        {
            b.ToTable("FactArticleAuthors", "ArticlesDW");
            b.HasKey(x => x.FactArticleAuthorId);
            b.HasIndex(x => x.ProductAuthorId).IsUnique();
            b.HasIndex(x => new { x.ArticleKey, x.AuthorKey }).IsUnique();
            b.HasIndex(x => x.AuthorKey);
            b.Property(x => x.Participation).HasMaxLength(150);
            b.Property(x => x.NameSnapshot).HasMaxLength(300);
            b.Property(x => x.AffiliationSnapshot).HasMaxLength(300);
            b.HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleKey);
            b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorKey);
        });

        modelBuilder.Entity<FactArticleIndexing>(b =>
        {
            b.ToTable("FactArticleIndexings", "ArticlesDW");
            b.HasKey(x => x.FactArticleIndexingId);
            b.HasIndex(x => new { x.ArticleKey, x.IndexingSourceKey }).IsUnique();
            b.HasIndex(x => x.IndexingSourceKey);
            b.HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleKey);
            b.HasOne(x => x.IndexingSource).WithMany().HasForeignKey(x => x.IndexingSourceKey);
        });

        modelBuilder.Entity<FactVenueMetricYear>(b =>
        {
            b.ToTable("FactVenueMetricYears", "ArticlesDW");
            b.HasKey(x => x.FactVenueMetricYearId);
            b.HasIndex(x => new { x.VenueKey, x.Year }).IsUnique();
            b.Property(x => x.Sjr).HasPrecision(18, 6);
            b.Property(x => x.Quartile).HasMaxLength(50);
            b.HasOne(x => x.Venue).WithMany().HasForeignKey(x => x.VenueKey);
        });
    }

    private static void DisableCascadeDeletesGlobally(ModelBuilder modelBuilder)
    {
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
