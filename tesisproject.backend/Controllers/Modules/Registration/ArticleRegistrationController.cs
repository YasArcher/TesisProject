using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/articles/aggregate")]
    [AllowAnonymous]
    public class ArticleRegistrationController : ControllerBase
    {
        private readonly IArticleRegistrationService _articleRegistrationService;

        public ArticleRegistrationController(IArticleRegistrationService articleRegistrationService)
        {
            _articleRegistrationService = articleRegistrationService;
        }

        [HttpPost]
        public async Task<ActionResult<RegisterArticleAggregateResponse>> Register(
            [FromBody] RegisterArticleAggregateRequest request,
            CancellationToken ct)
        {
            try
            {
                var result = await _articleRegistrationService.RegisterArticleAggregateAsync(request, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                return Conflict(new { message = "El articulo no pudo guardarse porque ya existe un registro unico con los mismos datos." });
            }
        }
    }
}
