using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;

namespace tesisproject.backend.UnitOfWork.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _ctx;

        public IProjectRepository Projects { get; }
        public IGroupRepository Groups { get; }
        public IGroupMemberRepository GroupMembers { get; }
        public IBudgetRepository Budgets { get; }
        public IVisitRepository Visits { get; }

        public IProjectExtensionRepository ProjectExtensions { get; set; }

        public UnitOfWork(
            AppDbContext ctx,
            IProjectRepository projectRepository,
            IGroupRepository groupRepository,
            IGroupMemberRepository groupMemberRepository,
            IBudgetRepository budgets,
            IVisitRepository visitRepository,
            IProjectExtensionRepository projectExtensions)
        {
            _ctx = ctx;
            Projects = projectRepository;
            Groups = groupRepository;
            GroupMembers = groupMemberRepository;
            Budgets = budgets;
            Visits = visitRepository;
            ProjectExtensions = projectExtensions;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _ctx.SaveChangesAsync(ct);

        public ValueTask DisposeAsync() => _ctx.DisposeAsync();
    }
}
