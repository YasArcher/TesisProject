using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedCatalogRepository<T> : GenericRepository<T>, IUnifiedCatalogRepository<T>
        where T : CatalogEntityBase
    {
        public UnifiedCatalogRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<IReadOnlyList<T>> ListAsync(
            bool onlyActives = true,
            Expression<Func<T, bool>>? where = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            IQueryable<T> q = _db;

            if (onlyActives)
                q = q.Where(e => e.IsActive);

            if (where is not null)
                q = q.Where(where);

            if (include is not null)
                q = include(q);

            return await q.AsNoTracking()
                          .OrderBy(e => e.Name)
                          .ToListAsync();
        }

        public async Task<Dictionary<int, T>> GetByIdsAsync(
            IEnumerable<int> ids,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            var keys = ids?.Distinct().ToList() ?? [];
            if (keys.Count == 0) return new();

            IQueryable<T> q = _db.Where(e => keys.Contains(e.Id));

            if (include is not null)
                q = include(q);

            var list = await q.AsNoTracking().ToListAsync(ct);
            return list.ToDictionary(e => e.Id, e => e);
        }

        public async Task<T?> GetByNameAsync(
            string name,
            bool onlyActives = true,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            var n = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(n)) return null;

            IQueryable<T> q = _db;

            if (onlyActives)
                q = q.Where(e => e.IsActive);

            if (include is not null)
                q = include(q);

            return await q.AsNoTracking()
                          .FirstOrDefaultAsync(e => e.Name == n, ct);
        }

        public async Task<bool> NameExistsAsync(
            string name,
            int? excludeId = null,
            CancellationToken ct = default)
        {
            var n = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(n)) return false;

            var q = _db.Where(e => e.Name == n);

            if (excludeId.HasValue)
                q = q.Where(e => e.Id != excludeId.Value);

            return await q.AnyAsync(ct);
        }

        public async Task<List<KeyValueItemDTO>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var q = _db.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));

            if (take is > 0)
                q = q.Take(take.Value);

            return await q.OrderBy(x => x.Name)
                          .Select(x => new KeyValueItemDTO { Id = x.Id, Name = x.Name })
                          .ToListAsync(ct);
        }

        public async Task<bool> HasReferencesAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0) return false;

            var principalType = _ctx.Model.FindEntityType(typeof(T));
            if (principalType is null) return false;

            // Recorremos todas las FKs que tienen como principal este catálogo
            var foreignKeys = _ctx.Model.GetEntityTypes()
                .SelectMany(et => et.GetForeignKeys())
                .Where(fk => fk.PrincipalEntityType == principalType)
                .ToList();

            if (foreignKeys.Count == 0) return false;

            foreach (var fk in foreignKeys)
            {
                // Solo soportamos FK simples (1 columna). Si tienes compuestas, se ignoran aquí.
                if (fk.Properties.Count != 1) continue;

                var fkProp = fk.Properties[0];
                var dependentClr = fk.DeclaringEntityType.ClrType;

                // Crear: e => EF.Property<fkType>(e, "FkPropName") == id
                var param = Expression.Parameter(dependentClr, "e");
                var fkType = fkProp.ClrType;

                var efPropertyMethod = typeof(EF)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == nameof(EF.Property) && m.IsGenericMethod && m.GetParameters().Length == 2)
                    .MakeGenericMethod(fkType);

                var left = Expression.Call(
                    efPropertyMethod,
                    param,
                    Expression.Constant(fkProp.Name));

                // Si la FK es nullable int? convertimos id a int?
                Expression right = fkType == typeof(int?)
                    ? Expression.Constant((int?)id, typeof(int?))
                    : Expression.Constant(id, fkType);

                // Si la FK no es int/int?, no podemos comparar con id (lo ignoramos)
                if (fkType != typeof(int) && fkType != typeof(int?))
                    continue;

                var predicateBody = Expression.Equal(left, right);
                var predicateType = typeof(Func<,>).MakeGenericType(dependentClr, typeof(bool));
                var predicate = Expression.Lambda(predicateType, predicateBody, param);

                // IQueryable depSet = _ctx.Set(dependentClr).AsNoTracking();
                // DbSet<TDependent> set = _ctx.Set<TDependent>();
                var setMethod = typeof(DbContext)
                    .GetMethods()
                    .Single(m => m.Name == nameof(DbContext.Set)
                                 && m.IsGenericMethod
                                 && m.GetParameters().Length == 0);

                var dbSetObj = setMethod
                    .MakeGenericMethod(dependentClr)
                    .Invoke(_ctx, null)!;

                // setNoTracking = set.AsNoTracking()
                var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .Single(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking)
                                 && m.IsGenericMethod
                                 && m.GetParameters().Length == 1);

                var setNoTrackingObj = asNoTrackingMethod
                    .MakeGenericMethod(dependentClr)
                    .Invoke(null, new object[] { dbSetObj })!;


                // Ejecutar AnyAsync<TDependent>(set, predicate, ct) vía reflexión
                var anyAsync = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync))
                    .Where(m => m.IsGenericMethodDefinition)
                    .Where(m =>
                    {
                        var p = m.GetParameters();
                        return p.Length == 3
                               && p[0].ParameterType.IsGenericType
                               && p[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>);
                    })
                    .Single()
                    .MakeGenericMethod(dependentClr);

                var taskObj = (Task)anyAsync.Invoke(null, new object[] { setNoTrackingObj, predicate, ct })!;
                await taskObj.ConfigureAwait(false);

                // Leer Task<bool>.Result
                var resultProp = taskObj.GetType().GetProperty(nameof(Task<bool>.Result))!;
                var hasAny = (bool)resultProp.GetValue(taskObj)!;

                if (hasAny) return true;
            }

            return false;
        }
    }
}
