using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    //[Authorize] // opcional
    public class ArticlesController : ControllerBase
    {
        private readonly IArticlesService _svc;

        public ArticlesController(IArticlesService svc)
        {
            _svc = svc;
        }

        // GET: api/articles?page=1&pageSize=20&search=...&year=2024
        [HttpGet]
        public async Task<ActionResult<PagedResult<ArticleListItemDto>>> GetList([FromQuery] ArticleListQuery query, CancellationToken ct)
        {
            var result = await _svc.GetListAsync(query, ct);
            return Ok(result);
        }

        // GET: api/articles/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ArticleDetailDto>> GetById(int id, CancellationToken ct)
        {
            var dto = await _svc.GetByIdAsync(id, ct);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // POST: api/articles
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateArticleRequest req, CancellationToken ct)
        {
            // Usa el userId real si ya tienes identidad
            var userId = User?.Identity?.Name ?? "system";
            var id = await _svc.CreateAsync(req, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        // PUT: api/articles/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateArticleRequest req, CancellationToken ct)
        {
            var userId = User?.Identity?.Name ?? "system";
            var ok = await _svc.UpdateAsync(id, req, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        // DELETE: api/articles/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = User?.Identity?.Name ?? "system";
            var ok = await _svc.DeleteAsync(id, userId, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
