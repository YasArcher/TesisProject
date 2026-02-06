using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tesisproject.shared.Entities.Auth;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Entities.Core.Products;
using tesisproject.shared.Entities.Export;

namespace tesisproject.backend.Data
{
    public class AppDbContext
        : IdentityDbContext<IdentityUser<int>, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // =========================================================
        // DbSets - Core
        // =========================================================
        public DbSet<Budget> Budgets => Set<Budget>();
        public DbSet<BudgetTransaction> BudgetTransactions => Set<BudgetTransaction>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<ExternalResearcher> ExternalResearchers => Set<ExternalResearcher>();
        public DbSet<ExternalResearcherProject> ExternalResearcherProjects => Set<ExternalResearcherProject>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
        public DbSet<ObjectiveActivity> ObjectiveActivities => Set<ObjectiveActivity>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectExtension> ProjectExtensions => Set<ProjectExtension>();
        public DbSet<ProjectObjective> ProjectObjectives => Set<ProjectObjective>();
        public DbSet<Visit> Visits => Set<Visit>();
        public DbSet<VisitIssue> VisitIssues => Set<VisitIssue>();
        public DbSet<UserFacultyScope> UserFacultyScopes => Set<UserFacultyScope>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductAttributeDefinition> ProductAttributeDefinitions => Set<ProductAttributeDefinition>();
        public DbSet<ProductValue> ProductValues => Set<ProductValue>();
        public DbSet<ProductAuthor> ProductAuthors => Set<ProductAuthor>();
        public DbSet<ObjectiveActivityUser> ObjectiveActivityUsers => Set<ObjectiveActivityUser>();
        public DbSet<Convocation> Convocations => Set<Convocation>();
        public DbSet<ConvocationRule> ConvocationRules => Set<ConvocationRule>();
        public DbSet<ProjectResearchCategory> ProjectResearchCategories => Set<ProjectResearchCategory>();
        public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();
        public DbSet<ExportField> ExportFields { get; set; }
        public DbSet<ExportTemplate> ExportTemplates { get; set; }
        public DbSet<ExportTemplateColumn> ExportTemplateColumns { get; set; }
        public DbSet<VisitObjectiveActivityProgress> VisitObjectiveActivityProgresses => Set<VisitObjectiveActivityProgress>();


        // =========================================================
        // DbSets - Catalogs
        // =========================================================
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
        public DbSet<FundingType> FundingTypes => Set<FundingType>();
        public DbSet<GroupType> GroupTypes => Set<GroupType>();
        public DbSet<Institution> Institutions => Set<Institution>();
        public DbSet<MemberRoleType> MemberRoleTypes => Set<MemberRoleType>();
        public DbSet<ObjectiveType> ObjectiveTypes => Set<ObjectiveType>();
        public DbSet<ProjectState> ProjectStates => Set<ProjectState>();
        public DbSet<ProjectType> ProjectTypes => Set<ProjectType>();
        public DbSet<ResearchCategory> ResearchCategories => Set<ResearchCategory>();
        public DbSet<ResearchCategoryType> ResearchCategoryTypes => Set<ResearchCategoryType>();
        public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
        public DbSet<VisitState> VisitStates => Set<VisitState>();
        public DbSet<ProductType> ProductTypes => Set<ProductType>();
        public DbSet<IndexingSource> IndexingSources => Set<IndexingSource>();
        public DbSet<ResearchCategoryGroup> ResearchCategoryGroups => Set<ResearchCategoryGroup>();
        public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
        public DbSet<ProjectOriginType> ProjectOriginTypes => Set<ProjectOriginType>();
        public DbSet<ProjectExtensionType> ProjectExtensionTypes => Set<ProjectExtensionType>();

        // =========================================================
        // DbSets - Auth
        // =========================================================
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();

        // =========================================================
        // OnModelCreating
        // =========================================================
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1) Relaciones de FK hacia usuarios (auditoría/propietario)
            ConfigureUserForeignKeys(builder);

            // 2) RefreshTokens
            ConfigureRefreshTokens(builder);
            ConfigureAppUser(builder);

            // 3) Otras configuraciones puntuales
            ConfigureInstitution(builder);
            ConfigureDocument(builder);
            ConfigureProjectBudget(builder);
            ConfigureVisitIssue(builder);
            ConfigureProducts(builder);
            ConfigureConvocations(builder);
            ConfigureProjectResearchCategory(builder);
            ConfigureResearchCategories(builder);
            ConfigureProjectDocuments(builder);
            ConfigureProjectExtensions(builder);
            ConfigureVisitObjectiveActivityProgress(builder);

            // 4) Visit (dos FKs hacia Documents, sin cascada)
            ConfigureVisit(builder);

            // 5) Deshabilitar cascada en TODAS las FKs restantes (garantía global)
            DisableCascadeDeletesGlobally(builder);
        }

        // =========================================================
        // Helpers (privados) para mapear FKs de usuario
        // =========================================================

        // Convierte una expresión fuertemente tipada en una expresión a object
        private static Expression<Func<TEntity, object?>> ToObjectExpr<TEntity, TProp>(
            Expression<Func<TEntity, TProp>> expr) where TEntity : class
        {
            var param = expr.Parameters[0];
            var body = Expression.Convert(expr.Body, typeof(object));
            return Expression.Lambda<Func<TEntity, object?>>(body, param);
        }

        private static void MapUserFK<TEntity, TProp>(
            ModelBuilder mb,
            Expression<Func<TEntity, TProp>> fkExpr,
            DeleteBehavior delete = DeleteBehavior.NoAction)
            where TEntity : class
        {
            var e = mb.Entity<TEntity>();
            var objExpr = ToObjectExpr(fkExpr);

            // Índice sobre la columna de auditoría (ApprovedByUserId, CreatedByUserId, etc.)
            e.HasIndex(objExpr);

            // Ahora la FK apunta a AppUser.IdUser (no a IdentityUser<int>)
            e.HasOne<AppUser>()
             .WithMany()
             .HasForeignKey(objExpr)         // columna en la entidad (p.ej. ApprovedByUserId)
             .HasPrincipalKey(u => u.IdUser) // PK en AppUser
             .OnDelete(delete);
        }

        private static void ConfigureProjectExtensions(ModelBuilder builder)
        {
            // ============== ProjectExtensionType (Catalog) ==============
            builder.Entity<ProjectExtensionType>(b =>
            {
                b.Property(x => x.Name)
                 .HasMaxLength(200)
                 .IsRequired();

                // Opcional: evitar duplicados en catálogo
                b.HasIndex(x => x.Name).IsUnique();
            });

            // =================== ProjectExtension (Core) ===================
            builder.Entity<ProjectExtension>(b =>
            {
                b.HasKey(x => x.ProjectExtensionId);

                // Índices útiles
                b.HasIndex(x => x.ProjectId);
                b.HasIndex(x => x.ProjectExtensionTypeId);
                b.HasIndex(x => x.DocumentId);

                // FK -> Project
                b.HasOne(x => x.Project)
                 .WithMany(p => p.ProjectExtensions)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK -> ProjectExtensionType (Catalog)
                b.HasOne(x => x.ProjectExtensionType)
                 .WithMany(t => t.ProjectExtensions)
                 .HasForeignKey(x => x.ProjectExtensionTypeId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK -> Document (opcional)
                b.HasOne(x => x.Document)
                 .WithMany()
                 .HasForeignKey(x => x.DocumentId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private static void ConfigureVisitObjectiveActivityProgress(ModelBuilder builder)
        {
            builder.Entity<VisitObjectiveActivityProgress>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasIndex(x => x.VisitId);
                b.HasIndex(x => x.ObjectiveActivityId);

                // 1 registro por actividad por visita
                b.HasIndex(x => new { x.VisitId, x.ObjectiveActivityId }).IsUnique();

                // FK -> ObjectiveActivity
                b.HasOne(x => x.ObjectiveActivity)
                 .WithMany(a => a.VisitProgresses)
                 .HasForeignKey(x => x.ObjectiveActivityId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK -> Visit
                b.HasOne(x => x.Visit)
                 .WithMany(v => v.ActivityProgresses)
                 .HasForeignKey(x => x.VisitId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private static void ConfigureAppUser(ModelBuilder builder)
        {
            builder.Entity<AppUser>(b =>
            {
                // PK interno estable que usarán las tablas de negocio
                b.HasKey(u => u.IdUser);

                // Índices para resolver rápido por IdLocal / IdAsp
                b.HasIndex(u => u.IdLocal);
                b.HasIndex(u => u.IdAsp);

                // Mientras usas Identity local, puedes tener esta FK opcional:
                b.HasOne<IdentityUser<int>>()
                 .WithMany()
                 .HasForeignKey(u => u.IdLocal)
                 .HasPrincipalKey(i => i.Id)
                 .OnDelete(DeleteBehavior.NoAction);

                // (Con IdAsp NO hacemos FK porque viene del ASP externo)
            });
        }


        // Agrupa todos los mapeos de FK a usuarios en un solo lugar
        private static void ConfigureUserForeignKeys(ModelBuilder builder)
        {
            // Budget → ApprovedByUserId
            MapUserFK<Budget, int>(builder, b => b.ApprovedByUserId);

            // BudgetTransaction → CertifiedByUserId y ExecutedByUserId
            MapUserFK<BudgetTransaction, int>(builder, t => t.CertifiedByUserId);
            MapUserFK<BudgetTransaction, int?>(builder, t => t.ExecutedByUserId);

            // Document → CreatedByUserId y UpdatedByUserId
            MapUserFK<Document, int>(builder, d => d.CreatedByUserId);
            MapUserFK<Document, int?>(builder, d => d.UpdatedByUserId);

            // ExternalResearcherProject → CreatedByUserId
            MapUserFK<ExternalResearcherProject, int>(builder, erp => erp.CreatedByUserId);

            // GroupMember → UserId (usuario externo)
            MapUserFK<GroupMember, int>(builder, gm => gm.UserId);

            // Project → CreatedByUserId
            MapUserFK<Project, int>(builder, p => p.CreatedByUserId);

            // Visit → PerformedByUserId
            MapUserFK<Visit, int?>(builder, v => v.PerformedByUserId);

            // VisitIssue → ReportedByUserId
            MapUserFK<VisitIssue, int?>(builder, vi => vi.ReportedByUserId);

            // UserFacultyScope → IdentityUserId
            MapUserFK<UserFacultyScope, int>(builder, ufs => ufs.IdentityUserId);
            // ProductAuthor → UserId
            MapUserFK<ProductAuthor, int>(builder, pa => pa.UserId);
        }

        // =========================================================
        // Configs específicas por entidad
        // =========================================================

        private static void ConfigureRefreshTokens(ModelBuilder builder)
        {
            builder.Entity<RefreshToken>(b =>
            {
                b.Property(x => x.TokenHash)
                 .IsRequired()
                 .HasMaxLength(128);

                b.HasIndex(x => x.TokenHash)
                 .IsUnique();

                b.HasOne<IdentityUser<int>>()
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .HasPrincipalKey(u => u.Id)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private static void ConfigureResearchCategories(ModelBuilder builder)
        {
            // ResearchCategoryGroup 1:N ResearchCategoryType
            builder.Entity<ResearchCategoryGroup>(b =>
            {
                b.HasMany(g => g.Types)
                 .WithOne(t => t.ResearchCategoryGroup)
                 .HasForeignKey(t => t.ResearchCategoryGroupId)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ResearchCategoryType 1:N ResearchCategory
            builder.Entity<ResearchCategoryType>(b =>
            {
                b.HasMany(t => t.ResearchCategories)
                 .WithOne(rc => rc.ResearchCategoryType)
                 .HasForeignKey(rc => rc.ResearchCategoryTypeId)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ResearchCategory self-reference (Padre/Hijos)
            builder.Entity<ResearchCategory>(b =>
            {
                b.HasOne(rc => rc.ParentCategory)
                 .WithMany(rc => rc.SubCategories)
                 .HasForeignKey(rc => rc.ParentCategoryId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        protected static void ConfigureProjectResearchCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectResearchCategory>(b =>
            {
                // ✅ Clave primaria simple
                b.HasKey(prc => prc.ProjectResearchCategoryId);

                // ✅ (Opcional pero MUY recomendable) 
                // Para evitar duplicados Project + Category
                b.HasIndex(prc => new { prc.ProjectId, prc.ResearchCategoryId })
                 .IsUnique();

                // FK → Project
                b.HasOne(prc => prc.Project)
                 .WithMany(p => p.ProjectResearchCategories)
                 .HasForeignKey(prc => prc.ProjectId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK → ResearchCategory
                b.HasOne(prc => prc.ResearchCategory)
                 .WithMany(rc => rc.ProjectResearchCategories)
                 .HasForeignKey(prc => prc.ResearchCategoryId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private static void ConfigureInstitution(ModelBuilder builder)
        {
            // Institution → índice único (Name, CountryId)
            builder.Entity<Institution>()
                   .HasIndex(i => new { i.Name, i.CountryId })
                   .IsUnique();
        }

        private static void ConfigureDocument(ModelBuilder builder)
        {
            builder.Entity<Document>(b =>
            {
                b.HasIndex(d => new { d.DocumentTypeId, d.ResolutionCode })
                 .IsUnique()
                 .HasDatabaseName("UX_Documents_Type1_ResolutionCode")
                 .HasFilter("[ResolutionCode] IS NOT NULL AND [DocumentTypeId] = 1");
            });
        }


        private static void ConfigureProjectBudget(ModelBuilder builder)
        {
            // Project ↔ Budget → 1:N (un proyecto con muchos presupuestos)
            builder.Entity<Project>()
                   .HasMany(p => p.Budgets)
                   .WithOne(b => b.Project)
                   .HasForeignKey(b => b.ProjectId)
                   .OnDelete(DeleteBehavior.NoAction);

            // Budget → FundingType (cada presupuesto tiene un tipo de financiamiento)
            builder.Entity<Budget>(b =>
            {
                b.HasOne(bu => bu.FundingType)
                 .WithMany()
                 .HasForeignKey(bu => bu.FundingTypeId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }


        private static void ConfigureVisitIssue(ModelBuilder builder)
        {
            builder.Entity<VisitIssue>(b =>
            {
                b.HasOne(vi => vi.Visit)
                 .WithMany(v => v.Issues)
                 .HasForeignKey(vi => vi.VisitId)
                 .OnDelete(DeleteBehavior.NoAction);
            });

        }

        private static void ConfigureVisit(ModelBuilder builder)
        {
            builder.Entity<Visit>(b =>
            {
                // Visit -> Documents (DocumentId = informe de visita) SIN cascada
                b.HasOne(v => v.Document)
                 .WithMany()
                 .HasForeignKey(v => v.DocumentId)
                 .OnDelete(DeleteBehavior.NoAction)
                 .HasConstraintName("FK_Visits_Documents_DocumentId");

                // Visit -> Documents (FundingDocumentId = informe económico) SIN cascada
                b.HasOne(v => v.FundingDocument)
                 .WithMany()
                 .HasForeignKey(v => v.FundingDocumentId)
                 .OnDelete(DeleteBehavior.NoAction)
                 .HasConstraintName("FK_Visits_Documents_FundingDocumentId");

                // Visit -> Documents (ProgressDocumentId = informe de avance) SIN cascada
                b.HasOne(v => v.ProgressDocument)
                 .WithMany()
                 .HasForeignKey(v => v.ProgressDocumentId)
                 .OnDelete(DeleteBehavior.NoAction)
                 .HasConstraintName("FK_Visits_Documents_ProgressDocumentId");

                // Visit -> Project SIN cascada
                b.HasOne(v => v.Project)
                 .WithMany(p => p.Visits)
                 .HasForeignKey(v => v.ProjectId)
                 .OnDelete(DeleteBehavior.NoAction);

                // Visit -> VisitState SIN cascada
                b.HasOne(v => v.VisitState)
                 .WithMany()
                 .HasForeignKey(v => v.VisitStateId)
                 .OnDelete(DeleteBehavior.NoAction);

                // Índices útiles
                b.HasIndex(v => v.DocumentId);
                b.HasIndex(v => v.FundingDocumentId);
                b.HasIndex(v => v.ProgressDocumentId);
                b.HasIndex(v => v.ProjectId);
                b.HasIndex(v => v.VisitStateId);
            });
        }


        private static void ConfigureProducts(ModelBuilder builder)
        {
            // ============== ProductType (catálogo) ==============
            builder.Entity<ProductType>(b =>
            {
                b.Property(x => x.Name).HasMaxLength(100).IsRequired();
                b.HasIndex(x => x.Name).IsUnique(); // opcional
            });

            // ============== Product (ProjectId requerido, VisitId opcional) ==============
            builder.Entity<Product>(b =>
            {
                b.Property(p => p.Title).HasMaxLength(1024).IsRequired();
                b.Property(p => p.Description).HasMaxLength(4000);

                // FK → ProductType
                b.HasOne(p => p.ProductType)
                 .WithMany()
                 .HasForeignKey(p => p.ProductTypeId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK → Project (requerido)
                b.HasOne(p => p.Project)
                 .WithMany(pr => pr.Products!) // si no tienes navegación en Project, usa .WithMany()
                 .HasForeignKey(p => p.ProjectId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK → Visit (opcional)
                b.HasOne(p => p.Visit)
                 .WithMany(v => v.Products)// si quieres navegación, agrega ICollection<Product> en Visit y cámbialo a v => v.Products!
                 .HasForeignKey(p => p.VisitId)
                 .OnDelete(DeleteBehavior.NoAction);

                // Índices útiles
                b.HasIndex(p => p.ProjectId);
                b.HasIndex(p => p.VisitId);
                b.HasIndex(p => p.ProductTypeId);
                b.HasIndex(p => p.IsActive);
            });

            // ============== ProductAttributeDefinition ==========
            //builder.Entity<ProductAttributeDefinition>(b =>
            //{
            //    b.Property(a => a.AttributeName).HasMaxLength(128).IsRequired();
            //    b.Property(a => a.DataType).HasMaxLength(32).IsRequired();
            //    b.Property(a => a.Unit).HasMaxLength(32);

            //    b.HasOne(a => a.ProductType)
            //     .WithMany()
            //     .HasForeignKey(a => a.ProductTypeId)
            //     .OnDelete(DeleteBehavior.NoAction);

            //    // Evita duplicados por (Tipo, Nombre)
            //    b.HasIndex(a => new { a.ProductTypeId, a.AttributeName })
            //     .IsUnique();
            //});

            // ============== ProductValue ========================
            builder.Entity<ProductValue>(b =>
            {
                // Un valor por (Producto, AtributoDef)
                b.HasIndex(v => new { v.ProductId, v.AttributeDefinitionId })
                 .IsUnique();

                b.HasOne(v => v.Product)
                 .WithMany(p => p.Values!)
                 .HasForeignKey(v => v.ProductId)
                 .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(v => v.AttributeDefinition)
                 .WithMany(d => d.ProductValues!)
                 .HasForeignKey(v => v.AttributeDefinitionId)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ============== ProductAuthor (User como autor ASP) =====
            builder.Entity<ProductAuthor>(b =>
            {
                // Un mismo usuario no puede repetirse en el mismo producto
                b.HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();

                b.HasOne(x => x.Product)
                 .WithMany(p => p.Authors!)
                 .HasForeignKey(x => x.ProductId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private static void ConfigureConvocations(ModelBuilder builder)
        {
            // ================= Convocation =================
            builder.Entity<Convocation>(b =>
            {
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.HasIndex(x => x.IsActive);
                // Si quieres evitar solapes por nombre+fechas (opcional):
                // b.HasIndex(x => new { x.Name, x.StartDate, x.EndDate }).IsUnique();
            });

            // ================= ConvocationRule ==============
            builder.Entity<ConvocationRule>(b =>
            {
                // Longitudes
                b.Property(x => x.GroupCode).HasMaxLength(64);
                b.Property(x => x.Notes).HasMaxLength(256);

                // Índices útiles para consultas
                b.HasIndex(x => x.ConvocationId);
                b.HasIndex(x => new { x.ConvocationId, x.ProductTypeId });
                b.HasIndex(x => new { x.ConvocationId, x.GroupCode }); // grupos OR
                b.HasIndex(x => new { x.MinDurationMonths, x.MaxDurationMonths });
                b.HasIndex(x => x.IsActive);

                // FK → Convocation (1:N)
                b.HasOne(x => x.Convocation)
                 .WithMany(c => c.Rules!)
                 .HasForeignKey(x => x.ConvocationId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK → ProductType (catálogo)
                b.HasOne<ProductType>()
                 .WithMany()
                 .HasForeignKey(x => x.ProductTypeId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private static void ConfigureProjectDocuments(ModelBuilder builder)
        {
            builder.Entity<ProjectDocument>(b =>
            {
                b.HasKey(x => x.ProjectDocumentId);

                // FK → Project (1 Proyecto tiene muchos ProjectDocuments)
                b.HasOne(x => x.Project)
                 .WithMany(p => p.ProjectDocuments)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.NoAction);

                // FK → Document (1 Document puede ser usado por muchos ProjectDocuments)
                b.HasOne(x => x.Document)
                 .WithMany(d => d.ProjectDocuments)
                 .HasForeignKey(x => x.DocumentId)
                 .OnDelete(DeleteBehavior.NoAction);

                // Índices útiles
                b.HasIndex(x => x.ProjectId);
                b.HasIndex(x => x.DocumentId);

                // evitar duplicados 
                b.HasIndex(x => new { x.ProjectId, x.DocumentId }).IsUnique();
            });
        }




        /// <summary>
        /// Fuerza DeleteBehavior.NoAction en TODAS las FKs del modelo
        /// (incluye las no configuradas explícitamente), para garantizar
        /// que no haya borrados en cascada en ninguna entidad.
        /// </summary>
        private static void DisableCascadeDeletesGlobally(ModelBuilder builder)
        {
            foreach (var fk in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}