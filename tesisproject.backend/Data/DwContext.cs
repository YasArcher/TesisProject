using Microsoft.EntityFrameworkCore;
using System.Linq;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.backend.Data
{
    public class DwContext : DbContext
    {
        public DwContext(DbContextOptions<DwContext> options) : base(options) { }

        // ============================================
        // DbSets - Dimensions
        // ============================================
        public DbSet<DimDate> DimDates => Set<DimDate>();
        public DbSet<DimFaculty> DimFaculties => Set<DimFaculty>();
        public DbSet<DimProjectState> DimProjectStates => Set<DimProjectState>();
        public DbSet<DimFundingType> DimFundingTypes => Set<DimFundingType>();
        public DbSet<DimProductType> DimProductTypes => Set<DimProductType>();
        public DbSet<DimIndexingDatabase> DimIndexingDatabases => Set<DimIndexingDatabase>();
        public DbSet<DimResearchCategory> DimResearchCategories => Set<DimResearchCategory>();
        public DbSet<DimQuartile> DimQuartiles => Set<DimQuartile>();


        // ============================================
        // DbSets - Facts
        // ============================================
        public DbSet<FactProject> FactProjects => Set<FactProject>();
        public DbSet<FactBudget> FactBudgets => Set<FactBudget>();
        public DbSet<FactProduct> FactProducts => Set<FactProduct>();

        // ============================================
        // DbSets - Bridges
        // ============================================
        public DbSet<BridgeProjectResearchCategory> BridgeProjectResearchCategories
            => Set<BridgeProjectResearchCategory>();

        // ============================================
        // OnModelCreating
        // ============================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Esquema por defecto para el DW
            modelBuilder.HasDefaultSchema("DW");

            ConfigureDimensions(modelBuilder);
            ConfigureFacts(modelBuilder);
            ConfigureBridges(modelBuilder);

            // En un DW normalmente no se quiere borrado en cascada
            DisableCascadeDeletesGlobally(modelBuilder);
        }

        // ============================================
        // Configuración de dimensiones
        // ============================================
        private static void ConfigureDimensions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DimDate>(b =>
            {
                b.HasKey(d => d.DateKey);

                // Índices típicos para filtros de tiempo
                b.HasIndex(d => d.Date);
                b.HasIndex(d => new { d.Year, d.Month });
            });

            modelBuilder.Entity<DimFaculty>(b =>
            {
                b.HasKey(f => f.FacultyKey);

                // Natural key (viene de Projects.FacultyId)
                b.HasIndex(f => f.FacultyId);
            });

            modelBuilder.Entity<DimProjectState>(b =>
            {
                b.HasKey(s => s.ProjectStateKey);
                b.HasIndex(s => s.ProjectStateId);
                b.HasIndex(s => s.Name);
            });

            modelBuilder.Entity<DimFundingType>(b =>
            {
                b.HasKey(f => f.FundingTypeKey);
                b.HasIndex(f => f.FundingTypeId);
                b.HasIndex(f => f.Name);
            });

            modelBuilder.Entity<DimProductType>(b =>
            {
                b.HasKey(p => p.ProductTypeKey);
                b.HasIndex(p => p.ProductTypeId);
                b.HasIndex(p => p.Name);
            });

            modelBuilder.Entity<DimIndexingDatabase>(b =>
            {
                b.HasKey(d => d.IndexingDatabaseKey);
                b.HasIndex(d => d.Name);
            });

            modelBuilder.Entity<DimResearchCategory>(b =>
            {
                b.HasKey(rc => rc.ResearchCategoryKey);
                b.HasIndex(rc => rc.ResearchCategoryId);
                b.HasIndex(rc => rc.CategoryTypeName);
                b.HasIndex(rc => rc.ResearchCategoryGroupName);
            });

            modelBuilder.Entity<DimQuartile>(b =>
            {
                b.HasKey(q => q.QuartileKey);
                b.HasIndex(q => q.Code);
            });
        }

        // ============================================
        // Configuración de hechos
        // ============================================
        private static void ConfigureFacts(ModelBuilder modelBuilder)
        {
            // ---------- FactProject ----------
            modelBuilder.Entity<FactProject>(b =>
            {
                b.HasKey(f => f.FactProjectId);

                // Índices para consultas típicas
                b.HasIndex(f => f.ProjectId);
                b.HasIndex(f => f.FacultyKey);
                b.HasIndex(f => f.ProjectStateKey);
                b.HasIndex(f => f.ApprovalDateKey);
                b.HasIndex(f => f.StartDateKey);
                b.HasIndex(f => f.EndDateKey);

                // Relaciones a dimensiones

                // Faculty
                b.HasOne(f => f.Faculty)
                 .WithMany(d => d.Projects)
                 .HasForeignKey(f => f.FacultyKey);

                // ProjectState
                b.HasOne(f => f.ProjectState)
                 .WithMany(d => d.Projects)
                 .HasForeignKey(f => f.ProjectStateKey);

                // DimDate - Approval
                b.HasOne(f => f.ApprovalDate)
                 .WithMany(d => d.ApprovalProjects)
                 .HasForeignKey(f => f.ApprovalDateKey)
                 .OnDelete(DeleteBehavior.NoAction);

                // DimDate - Start
                b.HasOne(f => f.StartDate)
                 .WithMany(d => d.StartProjects)
                 .HasForeignKey(f => f.StartDateKey)
                 .OnDelete(DeleteBehavior.NoAction);

                // DimDate - End
                b.HasOne(f => f.EndDate)
                 .WithMany(d => d.EndProjects)
                 .HasForeignKey(f => f.EndDateKey)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ---------- FactBudget ----------
            modelBuilder.Entity<FactBudget>(b =>
            {
                b.HasKey(f => f.FactBudgetId);

                b.HasIndex(f => f.BudgetId);
                b.HasIndex(f => f.ProjectId);
                b.HasIndex(f => f.FacultyKey);
                b.HasIndex(f => f.FundingTypeKey);
                b.HasIndex(f => f.ApprovedDateKey);

                // Faculty
                b.HasOne(f => f.Faculty)
                 .WithMany(d => d.Budgets)
                 .HasForeignKey(f => f.FacultyKey);

                // FundingType
                b.HasOne(f => f.FundingType)
                 .WithMany(d => d.Budgets)
                 .HasForeignKey(f => f.FundingTypeKey);

                // DimDate - Approved
                b.HasOne(f => f.ApprovedDate)
                 .WithMany(d => d.ApprovedBudgets)
                 .HasForeignKey(f => f.ApprovedDateKey)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ---------- FactProduct ----------
            modelBuilder.Entity<FactProduct>(b =>
            {
                b.HasKey(f => f.FactProductId);

                b.HasIndex(f => f.ProductId);
                b.HasIndex(f => f.ProjectId);
                b.HasIndex(f => f.FacultyKey);
                b.HasIndex(f => f.ProductTypeKey);
                b.HasIndex(f => f.CreatedDateKey);
                b.HasIndex(f => f.IsActiveFlag);
                b.HasIndex(f => f.IndexingDatabaseKey);
                b.HasIndex(f => f.QuartileKey);

                // Faculty
                b.HasOne(f => f.Faculty)
                 .WithMany(d => d.Products)
                 .HasForeignKey(f => f.FacultyKey);

                // ProductType
                b.HasOne(f => f.ProductType)
                 .WithMany(d => d.Products)
                 .HasForeignKey(f => f.ProductTypeKey);

                // DimDate - Created
                b.HasOne(f => f.CreatedDate)
                 .WithMany(d => d.CreatedProducts)
                 .HasForeignKey(f => f.CreatedDateKey)
                 .OnDelete(DeleteBehavior.NoAction);

                // DimIndexingDatabase (nullable)
                b.HasOne(f => f.IndexingDatabase)
                 .WithMany(d => d.Products)
                 .HasForeignKey(f => f.IndexingDatabaseKey)
                 .OnDelete(DeleteBehavior.NoAction);

                // DimQuartile (nullable)
                b.HasOne(f => f.Quartile)
                 .WithMany(q => q.Products)
                 .HasForeignKey(f => f.QuartileKey)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        // ============================================
        // Configuración de bridges
        // ============================================
        private static void ConfigureBridges(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BridgeProjectResearchCategory>(b =>
            {
                b.HasKey(x => x.BridgeProjectResearchCategoryId);

                // Un proyecto no debería repetirse con la misma categoría
                b.HasIndex(x => new { x.ProjectId, x.ResearchCategoryKey })
                 .IsUnique();

                b.HasIndex(x => x.ProjectId);
                b.HasIndex(x => x.ResearchCategoryKey);

                b.HasOne(x => x.ResearchCategory)
                 .WithMany(rc => rc.ProjectLinks)
                 .HasForeignKey(x => x.ResearchCategoryKey)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }

        /// <summary>
        /// Fuerza DeleteBehavior.NoAction en TODAS las FKs del modelo DW
        /// para evitar borrados en cascada.
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
