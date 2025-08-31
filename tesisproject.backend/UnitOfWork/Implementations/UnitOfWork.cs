using System.Threading.Tasks;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
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

        public UnitOfWork(AppDbContext ctx)
        {
            _ctx = ctx;
            Projects = new ProjectRepository(_ctx);
            Groups = new GroupRepository(_ctx);
            GroupMembers = new GroupMemberRepository(_ctx);
        }

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();
        public ValueTask DisposeAsync() => _ctx.DisposeAsync();
    }
}
