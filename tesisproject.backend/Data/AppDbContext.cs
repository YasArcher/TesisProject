using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using tesisproject.shared.Entities.Auth;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Data
{
    public class AppDbContext
        : IdentityDbContext<IdentityUser<int>, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ===== DbSets Core =====
        public DbSet<Budget> Budgets => Set<Budget>();
        public DbSet<BudgetTransaction> BudgetTransactions => Set<BudgetTransaction>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectExtension> ProjectExtensions => Set<ProjectExtension>();
        public DbSet<ProjectScope> ProjectScopes => Set<ProjectScope>();
        public DbSet<Visit> Visits => Set<Visit>();
        public DbSet<Researcher> Researchers => Set<Researcher>();
        public DbSet<ProjectObjective> ProjectObjectives => Set<ProjectObjective>();
        public DbSet<ObjectiveActivitie> ObjectiveActivities => Set<ObjectiveActivitie>();
        public DbSet<ExternalResearcher> ExternalResearchers => Set<ExternalResearcher>();
        public DbSet<ExternalResearcherProject> ExternalResearcherProjects => Set<ExternalResearcherProject>();

        // ===== DbSets Catalogs =====
        public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
        public DbSet<GroupType> GroupTypes => Set<GroupType>();
        public DbSet<ProjectType> ProjectTypes => Set<ProjectType>();
        public DbSet<ScopeType> ScopeTypes => Set<ScopeType>();
        public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
        public DbSet<VisitState> VisitStates => Set<VisitState>();
        public DbSet<FundingType> FundingTypes => Set<FundingType>();
        public DbSet<ProjectState> ProjectStates => Set<ProjectState>();
        public DbSet<ProjectExtensionType> ProjectExtensionTypes => Set<ProjectExtensionType>();
        public DbSet<ResearchLineType> ResearchLineTypes => Set<ResearchLineType>();
        public DbSet<ObjectiveType> ObjectiveTypes => Set<ObjectiveType>();
        public DbSet<ResearcherType> ResearcherTypes => Set<ResearcherType>();
        public DbSet<ResearchDomainType> ResearchDomainTypes => Set<ResearchDomainType>();
        public DbSet<MemberRoleType> MemberRoleTypes => Set<MemberRoleType>();
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<Institution> Institutions => Set<Institution>();

        // ===== Auth =====
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ========================================
            // HELPERS PARA MAPEO DE FK A USUARIOS
            // ========================================

            /// <summary>
            /// Convierte una expresión fuertemente tipada a Expression que retorna object?
            /// Necesario porque HasIndex y HasForeignKey requieren Expression con Func que retorna object?
            /// </summary>
            static Expression<Func<TEntity, object?>> ToObjectExpr<TEntity, TProp>(
                Expression<Func<TEntity, TProp>> expr)
                where TEntity : class
            {
                var param = expr.Parameters[0];
                var body = Expression.Convert(expr.Body, typeof(object));
                return Expression.Lambda<Func<TEntity, object?>>(body, param);
            }

            /// <summary>
            /// Mapea una FK hacia AspNetUsers (IdentityUser) de forma genérica.
            /// Crea índice y configura la relación con el comportamiento de eliminación especificado.
            /// Nota: En sistemas con soft delete, usar NoAction evita problemas de cascada múltiple en SQL Server.
            /// </summary>
            static void MapUserFK<TEntity, TProp>(
                ModelBuilder mb,
                Expression<Func<TEntity, TProp>> fkExpr,
                DeleteBehavior delete = DeleteBehavior.NoAction)
                where TEntity : class
            {
                var e = mb.Entity<TEntity>();
                var objExpr = ToObjectExpr(fkExpr);

                // Crear índice en la FK para mejorar rendimiento
                e.HasIndex(objExpr);

                // Configurar relación con AspNetUsers<int>
                e.HasOne<IdentityUser<int>>()
                 .WithMany()
                 .HasForeignKey(objExpr)
                 .HasPrincipalKey(u => u.Id)
                 .OnDelete(delete);
            }

            // ========================================
            // MAPEO DE COLUMNAS DE AUDITORÍA Y USUARIOS
            // ========================================

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

            // Researcher → CreatedByUserId y UpdatedByUserId
            MapUserFK<Researcher, int>(builder, r => r.CreatedByUserId);
            MapUserFK<Researcher, int?>(builder, r => r.UpdatedByUserId);

            // Visit → PerformedByUserId
            MapUserFK<Visit, int>(builder, v => v.PerformedByUserId);

            // ========================================
            // REFRESH TOKENS
            // ========================================
            builder.Entity<RefreshToken>(b =>
            {
                b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
                b.HasIndex(x => x.TokenHash).IsUnique();

                b.HasOne<IdentityUser<int>>()
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .HasPrincipalKey(u => u.Id)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ========================================
            // OTRAS CONFIGURACIONES
            // ========================================

            // Institution → Índice único (Name, CountryId)
            builder.Entity<Institution>()
                   .HasIndex(i => new { i.Name, i.CountryId })
                   .IsUnique();

            // Document → Relación 1:1 auto-referencial (RelatedDocument)
            builder.Entity<Document>(b =>
            {
                b.HasOne(d => d.RelatedDocument)
                 .WithOne(d => d!.ReverseRelation)
                 .HasForeignKey<Document>(d => d.RelatedDocumentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Project ↔ Budget → Relación 1:1 (FK en Budget.ProjectId)
            // NoAction porque usamos soft delete en toda la BD
            builder.Entity<Project>()
                   .HasOne(p => p.Budget)
                   .WithOne()
                   .HasForeignKey<Budget>(b => b.ProjectId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}