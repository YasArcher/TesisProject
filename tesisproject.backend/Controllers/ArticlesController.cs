using Microsoft.AspNetCore.Mvc;
using tesisproject.shared.Abstractions.Articles;
using tesisproject.shared.Abstractions;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticlesService _svc;
        public ArticlesController(IArticlesService svc) => _svc = svc;

        [HttpGet]
        public Task<Result<IReadOnlyList<ArticleDto>>> GetAll(CancellationToken ct) => _svc.GetAllAsync(ct);

        [HttpGet("{id:int}")]
        public Task<Result<ArticleDto>> GetById(int id, CancellationToken ct) => _svc.GetByIdAsync(id, ct);

        [HttpPost]
        public Task<Result<int>> Create([FromBody] CreateArticleRequest req, CancellationToken ct) => _svc.CreateAsync(req, ct);

        [HttpPut]
        public Task<Result> Update([FromBody] UpdateArticleRequest req, CancellationToken ct) => _svc.UpdateAsync(req, ct);

        [HttpDelete("{id:int}")]
        public Task<Result> Delete(int id, CancellationToken ct) => _svc.DeleteAsync(id, ct);
    }
}
