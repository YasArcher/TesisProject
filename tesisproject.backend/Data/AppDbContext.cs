using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Identity;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Data
{
    public class AppDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets Core
        public DbSet<Budget> budgets => Set<Budget>();
        public DbSet<BudgetTransaction> budgetTransactions => Set<BudgetTransaction>();
        public DbSet<Document> documents => Set<Document>();
        public DbSet<Group> groups => Set<Group>();
        public DbSet<GroupMember> groupMembers => Set<GroupMember>();
        public DbSet<Project> projects => Set<Project>();
        public DbSet<ProjectExtension> projectExtensions => Set<ProjectExtension>();
        public DbSet<ProjectScope> projectScopes => Set<ProjectScope>();
        public DbSet<Visit> visits => Set<Visit>();
        // DbSets Catalogs
        public DbSet<DocumentType> documentTypes => Set<DocumentType>();
        public DbSet<GroupType> groupTypes => Set<GroupType>();
        public DbSet<ProjectType> projectTypes => Set<ProjectType>();
        public DbSet<ScopeType> scopeTypes => Set<ScopeType>();
        public DbSet<TransactionType> transactionTypes => Set<TransactionType>();
        public DbSet<VisitState> visitStates => Set<VisitState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
