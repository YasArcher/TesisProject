using AutoMapper;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Abstractions;
using tesisproject.shared.Abstractions.Articles;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Implementations
{
    public class ArticlesService : IArticlesService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _map;

        public ArticlesService(IUnitOfWork uow, IMapper map)
        {
            _uow = uow;
            _map = map;
        }

        public async Task<Result<IReadOnlyList<ArticleDto>>> GetAllAsync(CancellationToken ct = default)
        {
            var list = await _uow.Articles.GetAllAsync(ct);
            return Result<IReadOnlyList<ArticleDto>>.Ok(list.Select(_map.Map<ArticleDto>).ToList());
        }

        public async Task<Result<ArticleDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var e = await _uow.Articles.GetByIdAsync(id, ct);
            return e is null ? Result<ArticleDto>.Fail("Not found") : Result<ArticleDto>.Ok(_map.Map<ArticleDto>(e));
        }

        public async Task<Result<int>> CreateAsync(CreateArticleRequest req, CancellationToken ct = default)
        {
            var e = _map.Map<Article>(req);
            await _uow.Articles.AddAsync(e, ct);
            await _uow.SaveChangesAsync(ct);
            return Result<int>.Ok(e.Id);
        }

        public async Task<Result> UpdateAsync(UpdateArticleRequest req, CancellationToken ct = default)
        {
            var e = await _uow.Articles.GetByIdAsync(req.Id, ct);
            if (e is null) return Result.Fail("Not found");

            _map.Map(req, e);

            e.Participantes.Clear();
            foreach (var p in req.Participantes ?? new())
                e.Participantes.Add(_map.Map<ArticleParticipant>(p));

            await _uow.SaveChangesAsync(ct);
            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _uow.Articles.GetByIdAsync(id, ct);
            if (e is null) return Result.Fail("Not found");
            _uow.Articles.Remove(e);
            await _uow.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
