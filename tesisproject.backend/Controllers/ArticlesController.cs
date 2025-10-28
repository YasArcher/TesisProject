using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.shared.Abstractions;
using tesisproject.shared.Abstractions.Articles;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación por defecto (se puede permitir anónimo por acción)
public class ArticlesController : ControllerBase
{
    private readonly IArticlesService _svc;
    public ArticlesController(IArticlesService svc) => _svc = svc;

    // Listar (si quieres permitir ver sin login, quita este Authorize y usa [AllowAnonymous])
    [HttpGet]
    [AllowAnonymous] // ← opcional: si quieres que la lista sea pública
    public async Task<ActionResult<Result<IReadOnlyList<ArticleDto>>>> GetAll(CancellationToken ct = default)
        => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [Authorize] // o [AllowAnonymous] si quieres público también
    public async Task<ActionResult<Result<ArticleDto>>> GetById(int id, CancellationToken ct = default)
    {
        var res = await _svc.GetByIdAsync(id, ct);
        if (!res.Succeeded || res.Value is null) return NotFound(res);
        return Ok(res);
    }

    // Crear — requiere rol Admin, Editor o SuperAdmin
    [HttpPost]
    [Authorize(Policy = "ArticlesWrite")]
    public async Task<ActionResult<Result<int>>> Create([FromBody] CreateArticleRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(Result<int>.Fail("Modelo inválido."));
        var res = await _svc.CreateAsync(req, ct);
        if (!res.Succeeded) return BadRequest(res);
        return CreatedAtAction(nameof(GetById), new { id = res.Value }, res);
    }

    // Actualizar — requiere rol Admin, Editor o SuperAdmin
    [HttpPut]
    [Authorize(Policy = "ArticlesWrite")]
    public async Task<ActionResult<Result>> Update([FromBody] UpdateArticleRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(Result.Fail("Modelo inválido."));
        var res = await _svc.UpdateAsync(req, ct);
        if (!res.Succeeded) return BadRequest(res);
        return Ok(res);
    }

    // Borrar — requiere rol Admin, Editor o SuperAdmin (si lo deseas solo Admin y SuperAdmin, ajusta la policy)
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ArticlesWrite")]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken ct = default)
    {
        var res = await _svc.DeleteAsync(id, ct);
        if (!res.Succeeded) return NotFound(res);
        return Ok(res);
    }
}