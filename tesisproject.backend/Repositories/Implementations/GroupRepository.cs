using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class GroupRepository : IGenericRepository<Group>, IGroupRepository
    {
        private readonly AppDbContext _ctx;
        private readonly DbSet<Group> _db;

        public GroupRepository(AppDbContext ctx)
        {
            _ctx = ctx;
            _db = _ctx.Set<Group>();
        }

        // IGenericRepository<Group>
        public async Task AddAsync(Group entity, CancellationToken ct = default)
            => await _db.AddAsync(entity, ct);

        // (Si tu IGenericRepository también expone AddAsync sin token)
        public Task AddAsync(Group entity)
            => _db.AddAsync(entity).AsTask();

        public async Task<IEnumerable<Group>> GetAllAsync(Expression<Func<Group, bool>>? filter = null)
        {
            IQueryable<Group> q = _db.AsNoTracking();
            if (filter is not null) q = q.Where(filter);
            return await q.OrderBy(g => g.Name).ToListAsync();
        }

        public async Task<Group?> GetByIdAsync(object id)
        {
            if (id is Guid guid)
                return await _db.AsNoTracking().FirstOrDefaultAsync(g => g.GroupId == guid);

            // si llega otro tipo, devuelve null o lanza
            return null;
        }

        public void Update(Group entity)
            => _db.Update(entity);
        //Aqui se puede aplciar la logica de softdelete pero hay que modificar la BD dando una flag para saber cual es activo y cual no
        public void Remove(Group entity)
            => _db.Remove(entity);

        // IGroupRepository
        public async Task<Group?> GetByIdAsync(Guid id, bool includeMembers, CancellationToken ct = default)
        {
            IQueryable<Group> q = _db.AsNoTracking();

            if (includeMembers)
                q = q.Include(g => g.Members);

            // si también quieres incluir catálogo:
            // q = q.Include(g => g.GroupType);

            return await q.FirstOrDefaultAsync(g => g.GroupId == id, ct);
        }

        public Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
            => _db.AnyAsync(g => g.Name == name, ct);

        public IQueryable<Group> Query()
            => _db.AsNoTracking().AsQueryable();
    }
}
