using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/articles/aggregate")]
    [Authorize(Policy = AppPolicies.AuthorSubmission)]
    public class ArticleRegistrationController : ControllerBase
    {
        private readonly IArticleRegistrationService _articleRegistrationService;
        private readonly IRegistrationWorkflowSettingsService _registrationWorkflowSettings;
        private readonly UserManager<ApplicationUser> _userManager;

        public ArticleRegistrationController(
            IArticleRegistrationService articleRegistrationService,
            IRegistrationWorkflowSettingsService registrationWorkflowSettings,
            UserManager<ApplicationUser> userManager)
        {
            _articleRegistrationService = articleRegistrationService;
            _registrationWorkflowSettings = registrationWorkflowSettings;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.ArticlesWrite)]
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

        [HttpPost("submit-for-review")]
        [Authorize(Policy = AppPolicies.AuthorSubmission)]
        public async Task<ActionResult<BulkImportActionResultDto>> SubmitForReview(
            [FromBody] RegisterArticleAggregateRequest request,
            CancellationToken ct)
        {
            try
            {
                if (!await _registrationWorkflowSettings.CanInitiateArticleRegistrationAsync(User, ct))
                {
                    return Problem(
                        title: "Registro no habilitado para tu rol.",
                        detail: "El modo institucional actual no permite que este perfil inicie registros de artículos.",
                        statusCode: StatusCodes.Status403Forbidden);
                }

                if (!await HasAcceptedAuthorTermsAsync())
                {
                    return BadRequest(new { message = "Debes aceptar los términos y condiciones de manejo de información antes de enviar artículos a revisión." });
                }

                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.Identity?.Name;
                var result = await _articleRegistrationService.SubmitArticleAggregateForReviewAsync(request, userId, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude enviar el artículo a revisión.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<bool> HasAcceptedAuthorTermsAsync()
        {
            if (!User.IsInRole(AppRoles.Author) || User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Analyst))
            {
                return true;
            }

            var user = await _userManager.GetUserAsync(User);
            return user?.TermsAcceptedAt is not null;
        }
    }
}
