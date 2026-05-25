using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Configurations;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Identity;

namespace tesisproject.backend.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

        // ===== DbSets dominio =====
        public DbSet<Article> Articles => Set<Article>();
        public DbSet<ArticleParticipant> ArticleParticipants => Set<ArticleParticipant>();
        public DbSet<ArticleFile> ArticleFiles => Set<ArticleFile>();
        public DbSet<ArticleIndexing> ArticleIndexings => Set<ArticleIndexing>();

        // Catálogos / referencia
        public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
        public DbSet<PublicationStatus> PublicationStatuses => Set<PublicationStatus>();
        public DbSet<ResearchLine> ResearchLines => Set<ResearchLine>();
        public DbSet<IndexingSource> IndexingSources => Set<IndexingSource>();
        public DbSet<Faculty> Faculties => Set<Faculty>();

        // Campos OCDE
        public DbSet<BroadField> BroadFields => Set<BroadField>();
        public DbSet<SpecificField> SpecificFields => Set<SpecificField>();
        public DbSet<DetailedField> DetailedFields => Set<DetailedField>();

        // Publicaciones (revistas/congresos)
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<VenueMetric> VenueMetrics => Set<VenueMetric>();

        // Otros
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<FieldCatalogEntry> FieldCatalogEntries => Set<FieldCatalogEntry>();
        public DbSet<DynamicFieldOption> DynamicFieldOptions => Set<DynamicFieldOption>();
        public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();
        public DbSet<FormFieldDefinition> FormFieldDefinitions => Set<FormFieldDefinition>();
        public DbSet<DynamicFieldValue> DynamicFieldValues => Set<DynamicFieldValue>();
        public DbSet<ArticleParticipantDynamicFieldValue> ArticleParticipantDynamicFieldValues => Set<ArticleParticipantDynamicFieldValue>();
        public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
        public DbSet<ImportBatchRow> ImportBatchRows => Set<ImportBatchRow>();
        public DbSet<ImportBatchRowValue> ImportBatchRowValues => Set<ImportBatchRowValue>();
        public DbSet<ImportBatchError> ImportBatchErrors => Set<ImportBatchError>();
        public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
        public DbSet<WorkflowStageDefinition> WorkflowStageDefinitions => Set<WorkflowStageDefinition>();
        public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
        public DbSet<WorkflowStageInstance> WorkflowStageInstances => Set<WorkflowStageInstance>();
        public DbSet<WorkflowActionLog> WorkflowActionLogs => Set<WorkflowActionLog>();
        public DbSet<RegistrationMatrix> RegistrationMatrices => Set<RegistrationMatrix>();
        public DbSet<RegistrationMatrixColumn> RegistrationMatrixColumns => Set<RegistrationMatrixColumn>();
        public DbSet<RegistrationMatrixRow> RegistrationMatrixRows => Set<RegistrationMatrixRow>();
        public DbSet<RegistrationMatrixCell> RegistrationMatrixCells => Set<RegistrationMatrixCell>();
        public DbSet<IntelligenceTrainingRun> IntelligenceTrainingRuns => Set<IntelligenceTrainingRun>();
        public DbSet<IntelligenceTrainingAlgorithmMetric> IntelligenceTrainingAlgorithmMetrics => Set<IntelligenceTrainingAlgorithmMetric>();
        public DbSet<ReportingPerformanceMetric> ReportingPerformanceMetrics => Set<ReportingPerformanceMetric>();

        protected override void OnModelCreating(ModelBuilder m)
        {
            base.OnModelCreating(m);

            m.ApplyConfigurationsFromAssembly(
                typeof(ScientificProductionModelConfigurationMarker).Assembly,
                type => type.Namespace == typeof(ScientificProductionModelConfigurationMarker).Namespace);
        }
    }
}
