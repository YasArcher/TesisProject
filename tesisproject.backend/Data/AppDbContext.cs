using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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

        protected override void OnModelCreating(ModelBuilder m)
        {
            base.OnModelCreating(m);

            // =============== Article ===================
            m.Entity<Article>(e =>
            {
                e.Property(x => x.Title).HasMaxLength(500);
                e.Property(x => x.Doi).HasMaxLength(200);
                e.Property(x => x.PublicationUrl).HasMaxLength(500);
                e.Property(x => x.ProceedingsName).HasMaxLength(300);
                e.Property(x => x.Proceedings).HasMaxLength(300);
                e.Property(x => x.EventName).HasMaxLength(300);
                e.Property(x => x.GroupName).HasMaxLength(300);
                e.Property(x => x.Filiacion).HasMaxLength(300);
                e.Property(x => x.ExternalSource).HasMaxLength(50);
                e.Property(x => x.ExternalId).HasMaxLength(150);
                e.Property(x => x.IsOpenAccess).HasDefaultValue(false);

                e.HasOne(x => x.AcademicTerm)
                    .WithMany(t => t.Articles)
                    .HasForeignKey(x => x.AcademicTermId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.PublicationStatus)
                    .WithMany(s => s.Articles)
                    .HasForeignKey(x => x.PublicationStatusId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.ResearchLine)
                    .WithMany(r => r.Articles)
                    .HasForeignKey(x => x.ResearchLineId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Evitar multiple cascade paths
                e.HasOne(x => x.BroadField)
                    .WithMany()
                    .HasForeignKey(x => x.BroadFieldId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.SpecificField)
                    .WithMany()
                    .HasForeignKey(x => x.SpecificFieldId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.DetailedField)
                    .WithMany()
                    .HasForeignKey(x => x.DetailedFieldId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Faculty)
                    .WithMany(f => f.Articles)
                    .HasForeignKey(x => x.FacultyId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.Venue)
                    .WithMany(v => v.Articles)
                    .HasForeignKey(x => x.VenueId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índices
                e.HasIndex(a => a.Doi)
                 .HasDatabaseName("UX_Articles_Doi_NotBlank")
                 .IsUnique()
                 .HasFilter("[Doi] IS NOT NULL AND [Doi] <> N''");

                e.HasIndex(a => new { a.Title, a.Year, a.VenueId })
                 .HasDatabaseName("UX_Articles_TitleYearVenue_NoDoi")
                 .IsUnique()
                 .HasFilter("[Doi] IS NULL");
            });

            // =============== ArticleParticipant =========
            m.Entity<ArticleParticipant>(e =>
            {
                e.Property(x => x.Identificacion).HasMaxLength(100);
                e.Property(x => x.Nombre).HasMaxLength(300);
                e.Property(x => x.Participacion).HasMaxLength(150);
                e.Property(x => x.ParticipantType).HasMaxLength(50);
                e.Property(x => x.Email).HasMaxLength(200);
                e.Property(x => x.Orcid).HasMaxLength(50);
                e.Property(x => x.Affiliation).HasMaxLength(300);
                e.Property(x => x.ExternalAuthorId).HasMaxLength(150);
                e.Property(x => x.IsPrimaryAuthor).HasDefaultValue(false);

                e.HasOne(x => x.Article)
                 .WithMany(a => a.Participants)
                 .HasForeignKey(x => x.ArticleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ArticleId, x.Index }).IsUnique();
            });

            // =============== ArticleFile ================
            m.Entity<ArticleFile>(e =>
            {
                e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
                e.Property(x => x.FileUrl).HasMaxLength(500);
                e.Property(x => x.Sha256).HasMaxLength(64);

                e.HasOne(x => x.Article)
                 .WithMany(a => a.Files)
                 .HasForeignKey(x => x.ArticleId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // =============== ArticleIndexing ============
            m.Entity<ArticleIndexing>(e =>
            {
                e.HasKey(x => new { x.ArticleId, x.IndexingSourceId });

                e.HasOne(x => x.Article)
                 .WithMany(a => a.Indexings)
                 .HasForeignKey(x => x.ArticleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.IndexingSource)
                 .WithMany(s => s.ArticleIndexings)
                 .HasForeignKey(x => x.IndexingSourceId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // =============== AcademicTerm ===============
            m.Entity<AcademicTerm>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
            });

            // =============== PublicationStatus ==========
            m.Entity<PublicationStatus>(e =>
            {
                e.HasKey(x => x.PublicationStatusId);
                e.Property(x => x.Name).HasMaxLength(20).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
            });

            // =============== ResearchLine ===============
            m.Entity<ResearchLine>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
            });

            // =============== IndexingSource =============
            m.Entity<IndexingSource>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(120).IsRequired();
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => x.Name).IsUnique();
            });

            // =============== Faculty ====================
            m.Entity<Faculty>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Code).HasMaxLength(40);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasIndex(x => x.Name).IsUnique();
                e.HasIndex(x => x.Code)
                    .IsUnique()
                    .HasFilter("[Code] IS NOT NULL AND [Code] <> N''");
            });

            // =============== OCDE Fields ================
            m.Entity<BroadField>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
            });

            m.Entity<SpecificField>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Code).HasMaxLength(20);

                e.HasOne(x => x.BroadField)
                 .WithMany(b => b.SpecificFields)
                 .HasForeignKey(x => x.BroadFieldId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.BroadFieldId, x.Code })
                 .HasDatabaseName("UQ_SpecificField_Broad_Code")
                 .IsUnique()
                 .HasFilter("[Code] IS NOT NULL AND [Code] <> N''");

                e.HasIndex(x => new { x.BroadFieldId, x.Name })
                 .HasDatabaseName("UQ_SpecificField_Broad_Name")
                 .IsUnique();
            });

            m.Entity<DetailedField>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Code).HasMaxLength(20);

                e.HasOne(x => x.SpecificField)
                 .WithMany(s => s.DetailedFields)
                 .HasForeignKey(x => x.SpecificFieldId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.SpecificFieldId, x.Code })
                 .HasDatabaseName("UQ_DetailedField_Specific_Code")
                 .IsUnique()
                 .HasFilter("[Code] IS NOT NULL AND [Code] <> N''");

                e.HasIndex(x => new { x.SpecificFieldId, x.Name })
                 .HasDatabaseName("UQ_DetailedField_Specific_Name")
                 .IsUnique();
            });

            // =============== Venue / VenueMetric =========
            m.Entity<Venue>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Type).HasMaxLength(20).HasDefaultValue("Journal").IsRequired();
                e.Property(x => x.IssnCode).HasMaxLength(20);
                e.Property(x => x.IssueNumber).HasMaxLength(20);
                e.Property(x => x.VolumeNumber).HasMaxLength(20);
                e.Property(x => x.JournalUrl).HasMaxLength(400);

                e.HasIndex(x => new { x.Name, x.IssnCode })
                 .HasDatabaseName("UQ_Venue_Name_Issn")
                 .IsUnique()
                 .HasFilter("[IssnCode] IS NOT NULL");
            });

            m.Entity<VenueMetric>(e =>
            {
                e.HasKey(x => new { x.VenueId, x.Year });
                e.Property(x => x.Quartile).HasMaxLength(10);
                e.Property(x => x.SJR).HasPrecision(6, 3);

                e.HasOne(x => x.Venue)
                 .WithMany(v => v.VenueMetrics)   // <- CLAVE: SIEMPRE VenueMetrics
                 .HasForeignKey(x => x.VenueId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // =============== AuditLog ====================
            m.Entity<AuditLog>(e =>
            {
                e.Property(x => x.Action).HasMaxLength(120).IsRequired();
                e.Property(x => x.EntityName).HasMaxLength(120);
                e.Property(x => x.EntityId).HasMaxLength(120);
                e.Property(x => x.Detail).HasMaxLength(4000);
            });

            // =============== Project =====================
            m.Entity<Project>(e =>
            {
                e.Property(x => x.Code).IsRequired();
                e.Property(x => x.Name).IsRequired();
            });

            // =============== FieldCatalog ================
            m.Entity<FieldCatalogEntry>(e =>
            {
                e.ToTable("FieldCatalog");
                e.HasKey(x => x.FieldId);
                e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
                e.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
                e.Property(x => x.FieldLabel).HasMaxLength(150).IsRequired();
                e.Property(x => x.DataType).HasMaxLength(50).IsRequired();
                e.Property(x => x.SourceType).HasMaxLength(30).IsRequired();
                e.Property(x => x.PhysicalTableName).HasMaxLength(100);
                e.Property(x => x.PhysicalColumnName).HasMaxLength(100);
                e.Property(x => x.ReferenceTableName).HasMaxLength(100);
                e.Property(x => x.Placeholder).HasMaxLength(200);
                e.Property(x => x.HelpText).HasMaxLength(500);
                e.Property(x => x.DefaultValue).HasMaxLength(200);
                e.Property(x => x.ValidationRule).HasMaxLength(500);
            });

            // =============== DynamicFieldOptions =========
            m.Entity<DynamicFieldOption>(e =>
            {
                e.ToTable("DynamicFieldOptions");
                e.HasKey(x => x.DynamicFieldOptionId);
                e.Property(x => x.OptionValue).HasMaxLength(200).IsRequired();
                e.Property(x => x.OptionLabel).HasMaxLength(200).IsRequired();

                e.HasOne(x => x.Field)
                    .WithMany(x => x.Options)
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =============== FormDefinitions ============
            m.Entity<FormDefinition>(e =>
            {
                e.ToTable("FormDefinitions");
                e.HasKey(x => x.FormId);
                e.Property(x => x.FormKey).HasMaxLength(100).IsRequired();
                e.Property(x => x.FormName).HasMaxLength(150).IsRequired();
                e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);
            });

            // =============== FormFields =================
            m.Entity<FormFieldDefinition>(e =>
            {
                e.ToTable("FormFields");
                e.HasKey(x => x.FormFieldId);
                e.Property(x => x.GroupName).HasMaxLength(100);

                e.HasOne(x => x.Form)
                    .WithMany(x => x.Fields)
                    .HasForeignKey(x => x.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Field)
                    .WithMany(x => x.FormFields)
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============== DynamicFieldValues ==========
            m.Entity<DynamicFieldValue>(e =>
            {
                e.ToTable("DynamicFieldValues");
                e.HasKey(x => x.DynamicFieldValueId);
                e.Property(x => x.ValueDecimal).HasPrecision(18, 4);

                e.HasOne(x => x.Article)
                    .WithMany(x => x.DynamicFieldValues)
                    .HasForeignKey(x => x.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Field)
                    .WithMany()
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============== ParticipantDynamicValues ====
            m.Entity<ArticleParticipantDynamicFieldValue>(e =>
            {
                e.ToTable("ArticleParticipantDynamicFieldValues");
                e.HasKey(x => x.ArticleParticipantDynamicFieldValueId);
                e.Property(x => x.ValueDecimal).HasPrecision(18, 4);

                e.HasOne(x => x.ArticleParticipant)
                    .WithMany(x => x.DynamicFieldValues)
                    .HasForeignKey(x => x.ArticleParticipantId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Field)
                    .WithMany()
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============== ImportBatch ================
            m.Entity<ImportBatch>(e =>
            {
                e.ToTable("ImportBatch");
                e.HasKey(x => x.ImportBatchId);
                e.Property(x => x.BatchCode).HasMaxLength(100).IsRequired();
                e.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
                e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
                e.Property(x => x.FileName).HasMaxLength(260);
                e.Property(x => x.SourceReference).HasMaxLength(300);
                e.Property(x => x.Status).HasMaxLength(30).IsRequired();
                e.Property(x => x.CreatedBy).HasMaxLength(150);
                e.Property(x => x.Notes).HasMaxLength(500);

                e.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(x => x.Rows)
                    .WithOne(x => x.Batch)
                    .HasForeignKey(x => x.ImportBatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Errors)
                    .WithOne(x => x.Batch)
                    .HasForeignKey(x => x.ImportBatchId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =============== ImportBatchRow ============
            m.Entity<ImportBatchRow>(e =>
            {
                e.ToTable("ImportBatchRow");
                e.HasKey(x => x.ImportBatchRowId);
                e.Property(x => x.RowStatus).HasMaxLength(30).IsRequired();

                e.HasMany(x => x.Values)
                    .WithOne(x => x.Row)
                    .HasForeignKey(x => x.ImportBatchRowId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Errors)
                    .WithOne(x => x.Row)
                    .HasForeignKey(x => x.ImportBatchRowId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // =============== WorkflowDefinition ==========
            m.Entity<WorkflowDefinition>(e =>
            {
                e.ToTable("WorkflowDefinition");
                e.HasKey(x => x.WorkflowDefinitionId);
                e.Property(x => x.Key).HasMaxLength(100).IsRequired();
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => x.Key).IsUnique();
            });

            // =============== WorkflowStageDefinition =====
            m.Entity<WorkflowStageDefinition>(e =>
            {
                e.ToTable("WorkflowStageDefinition");
                e.HasKey(x => x.WorkflowStageDefinitionId);
                e.Property(x => x.StageKey).HasMaxLength(100).IsRequired();
                e.Property(x => x.StageName).HasMaxLength(150).IsRequired();
                e.Property(x => x.StageGroupKey).HasMaxLength(100);
                e.Property(x => x.StageGroupName).HasMaxLength(150);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.Property(x => x.CanEditData).HasDefaultValue(false);
                e.Property(x => x.CanReturn).HasDefaultValue(true);
                e.Property(x => x.CanApprove).HasDefaultValue(true);
                e.Property(x => x.CanProcessBatch).HasDefaultValue(false);
                e.Property(x => x.IsFinalStage).HasDefaultValue(false);

                e.HasOne(x => x.WorkflowDefinition)
                    .WithMany(x => x.Stages)
                    .HasForeignKey(x => x.WorkflowDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.WorkflowDefinitionId, x.DisplayOrder }).IsUnique();
                e.HasIndex(x => new { x.WorkflowDefinitionId, x.StageKey }).IsUnique();
            });

            // =============== WorkflowInstance ============
            m.Entity<WorkflowInstance>(e =>
            {
                e.ToTable("WorkflowInstance");
                e.HasKey(x => x.WorkflowInstanceId);
                e.Property(x => x.Status).HasMaxLength(30).IsRequired();

                e.HasOne(x => x.WorkflowDefinition)
                    .WithMany(x => x.Instances)
                    .HasForeignKey(x => x.WorkflowDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Batch)
                    .WithOne(x => x.WorkflowInstance)
                    .HasForeignKey<WorkflowInstance>(x => x.ImportBatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.CurrentStageDefinition)
                    .WithMany()
                    .HasForeignKey(x => x.CurrentStageDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.SubmittedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.SubmittedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => x.ImportBatchId).IsUnique();
            });

            // =============== WorkflowStageInstance =======
            m.Entity<WorkflowStageInstance>(e =>
            {
                e.ToTable("WorkflowStageInstance");
                e.HasKey(x => x.WorkflowStageInstanceId);
                e.Property(x => x.Status).HasMaxLength(30).IsRequired();
                e.Property(x => x.Notes).HasMaxLength(1000);

                e.HasOne(x => x.WorkflowInstance)
                    .WithMany(x => x.StageInstances)
                    .HasForeignKey(x => x.WorkflowInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.WorkflowStageDefinition)
                    .WithMany(x => x.StageInstances)
                    .HasForeignKey(x => x.WorkflowStageDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedToUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.WorkflowInstanceId, x.WorkflowStageDefinitionId }).IsUnique();
            });

            // =============== WorkflowActionLog ===========
            m.Entity<WorkflowActionLog>(e =>
            {
                e.ToTable("WorkflowActionLog");
                e.HasKey(x => x.WorkflowActionLogId);
                e.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
                e.Property(x => x.FromStatus).HasMaxLength(30);
                e.Property(x => x.ToStatus).HasMaxLength(30);
                e.Property(x => x.Comments).HasMaxLength(2000);

                e.HasOne(x => x.WorkflowInstance)
                    .WithMany(x => x.ActionLogs)
                    .HasForeignKey(x => x.WorkflowInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.WorkflowStageInstance)
                    .WithMany(x => x.ActionLogs)
                    .HasForeignKey(x => x.WorkflowStageInstanceId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.PerformedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.PerformedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============== ImportBatchRowValue =======
            m.Entity<ImportBatchRowValue>(e =>
            {
                e.ToTable("ImportBatchRowValue");
                e.HasKey(x => x.ImportBatchRowValueId);
                e.Property(x => x.ValueType).HasMaxLength(50);
                e.Property(x => x.ValidationMessage).HasMaxLength(500);

                e.HasOne(x => x.Field)
                    .WithMany()
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =============== ImportBatchError ==========
            m.Entity<ImportBatchError>(e =>
            {
                e.ToTable("ImportBatchError");
                e.HasKey(x => x.ImportBatchErrorId);
                e.Property(x => x.ErrorCode).HasMaxLength(100).IsRequired();
                e.Property(x => x.ErrorMessage).HasMaxLength(500).IsRequired();
                e.Property(x => x.Severity).HasMaxLength(20).IsRequired();

                e.HasOne(x => x.Field)
                    .WithMany()
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // =============== RegistrationMatrix ========
            m.Entity<RegistrationMatrix>(e =>
            {
                e.ToTable("RegistrationMatrix");
                e.HasKey(x => x.RegistrationMatrixId);
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
                e.Property(x => x.Status).HasMaxLength(30).IsRequired();
                e.Property(x => x.Notes).HasMaxLength(1000);
                e.Property(x => x.CreatedByUserId).HasMaxLength(450);
                e.HasIndex(x => x.CreatedByUserId);

                e.HasOne(x => x.LastImportBatch)
                    .WithMany()
                    .HasForeignKey(x => x.LastImportBatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(x => x.Columns)
                    .WithOne(x => x.Matrix)
                    .HasForeignKey(x => x.RegistrationMatrixId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Rows)
                    .WithOne(x => x.Matrix)
                    .HasForeignKey(x => x.RegistrationMatrixId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            m.Entity<RegistrationMatrixColumn>(e =>
            {
                e.ToTable("RegistrationMatrixColumn");
                e.HasKey(x => x.RegistrationMatrixColumnId);
                e.Property(x => x.WidthUnits).HasDefaultValue(1);
                e.HasIndex(x => new { x.RegistrationMatrixId, x.FieldId }).IsUnique();

                e.HasOne(x => x.Field)
                    .WithMany()
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            m.Entity<RegistrationMatrixRow>(e =>
            {
                e.ToTable("RegistrationMatrixRow");
                e.HasKey(x => x.RegistrationMatrixRowId);
                e.Property(x => x.Status).HasMaxLength(30).IsRequired();
                e.HasIndex(x => new { x.RegistrationMatrixId, x.RowNumber }).IsUnique();

                e.HasMany(x => x.Cells)
                    .WithOne(x => x.Row)
                    .HasForeignKey(x => x.RegistrationMatrixRowId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            m.Entity<RegistrationMatrixCell>(e =>
            {
                e.ToTable("RegistrationMatrixCell");
                e.HasKey(x => x.RegistrationMatrixCellId);
                e.Property(x => x.RawValue).HasMaxLength(4000);
                e.HasIndex(x => new { x.RegistrationMatrixRowId, x.FieldId }).IsUnique();

                e.HasOne(x => x.Field)
                    .WithMany()
                    .HasForeignKey(x => x.FieldId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
