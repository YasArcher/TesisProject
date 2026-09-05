using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Articles.Configurations;
using tesisproject.backend.Data.Articles.Entities;

namespace tesisproject.backend.Data.Articles;

public sealed class ArticlesDbContext : DbContext
{
    public ArticlesDbContext(DbContextOptions<ArticlesDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleParticipant> ArticleParticipants => Set<ArticleParticipant>();
    public DbSet<ArticleFile> ArticleFiles => Set<ArticleFile>();
    public DbSet<ArticleIndexing> ArticleIndexings => Set<ArticleIndexing>();
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<PublicationStatus> PublicationStatuses => Set<PublicationStatus>();
    public DbSet<ResearchLine> ResearchLines => Set<ResearchLine>();
    public DbSet<IndexingSource> IndexingSources => Set<IndexingSource>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<BroadField> BroadFields => Set<BroadField>();
    public DbSet<SpecificField> SpecificFields => Set<SpecificField>();
    public DbSet<DetailedField> DetailedFields => Set<DetailedField>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<VenueMetric> VenueMetrics => Set<VenueMetric>();
    public DbSet<FieldCatalogEntry> FieldCatalogEntries => Set<FieldCatalogEntry>();
    public DbSet<DynamicFieldOption> DynamicFieldOptions => Set<DynamicFieldOption>();
    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();
    public DbSet<FormFieldDefinition> FormFieldDefinitions => Set<FormFieldDefinition>();
    public DbSet<DynamicFieldValue> DynamicFieldValues => Set<DynamicFieldValue>();
    public DbSet<ArticleParticipantDynamicFieldValue> ArticleParticipantDynamicFieldValues => Set<ArticleParticipantDynamicFieldValue>();
    public DbSet<RegistrationMatrix> RegistrationMatrices => Set<RegistrationMatrix>();
    public DbSet<RegistrationMatrixColumn> RegistrationMatrixColumns => Set<RegistrationMatrixColumn>();
    public DbSet<RegistrationMatrixRow> RegistrationMatrixRows => Set<RegistrationMatrixRow>();
    public DbSet<RegistrationMatrixCell> RegistrationMatrixCells => Set<RegistrationMatrixCell>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ArticlesModelConfigurationMarker).Assembly,
            type => type.Namespace == typeof(ArticlesModelConfigurationMarker).Namespace);
    }
}
