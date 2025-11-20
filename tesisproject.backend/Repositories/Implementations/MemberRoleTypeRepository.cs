using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.Repositories.Implementations
{
    public class MemberRoleTypeRepository : CatalogRepository<MemberRoleType>, IMemberRoleTypeRepository
    {
        public MemberRoleTypeRepository(AppDbContext ctx) : base(ctx) { }
    }
}
