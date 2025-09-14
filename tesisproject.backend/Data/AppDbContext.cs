using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Entities.Auth;

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

        // ===== DbSets Catalogs =====
        public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
        public DbSet<GroupType> GroupTypes => Set<GroupType>();
        public DbSet<ProjectType> ProjectTypes => Set<ProjectType>();
        public DbSet<ScopeType> ScopeTypes => Set<ScopeType>();
        public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
        public DbSet<VisitState> VisitStates => Set<VisitState>();

        // ===== Auth =====
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // FK de Project → AspNetUsers (int ahora)
            builder.Entity<Project>(b =>
            {
                b.HasIndex(p => p.CreatedByUserId);

                b.HasOne<IdentityUser<int>>()
                 .WithMany()
                 .HasForeignKey(p => p.CreatedByUserId)
                 .HasPrincipalKey(u => u.Id)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // RefreshTokens → AspNetUsers (int)
            builder.Entity<RefreshToken>(b =>
            {
                b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
                b.HasIndex(x => x.TokenHash).IsUnique();

                b.HasOne<IdentityUser<int>>()
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .HasPrincipalKey(u => u.Id)
                 .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
