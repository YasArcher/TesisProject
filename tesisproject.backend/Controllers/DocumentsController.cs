using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Utils;
using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentsController(IDocumentService service) => _service = service;

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<DocumentResponseDTO>>> Create(
            [FromForm] UploadDocumentRequestDTO request,
            CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(ApiResponse<DocumentResponseDTO>.Fail("User not authenticated."));

            return (await _service.UploadAsync(request, userId.Value, ct))
                .ToActionResult();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<DocumentResponseDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<DocumentResponseDTO>>> Update(
            int id,
            [FromBody] UpdateDocumentRequestDTO request,
            CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(ApiResponse<DocumentResponseDTO>.Fail("User not authenticated."));

            return (await _service.UpdateAsync(id, request, userId.Value, ct))
                .ToActionResult();
        }

        [HttpPost("{id:int}/file")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<DocumentResponseDTO>>> ReplaceFile(
            int id,
            [FromForm] ReplaceDocumentFileRequestDTO request,
            CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(ApiResponse<DocumentResponseDTO>.Fail("User not authenticated."));

            return (await _service.ReplaceFileAsync(id, request, userId.Value, ct))
                .ToActionResult();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        [HttpGet("{id:int}/content")]
        public async Task<ActionResult> ViewContent(
            int id,
            CancellationToken ct)
        {
            var r = await _service.GetContentAsync(id, ct);
            if (!r.Success)
                return r.ToActionResult().Result!;

            var (stream, contentType, fileName) = r.Data;

            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(stream, contentType);
        }

        [HttpGet("{id:int}/download")]
        public async Task<ActionResult> Download(
            int id,
            CancellationToken ct)
        {
            var r = await _service.GetContentAsync(id, ct);
            if (!r.Success)
                return r.ToActionResult().Result!;

            var (stream, contentType, fileName) = r.Data;
            return File(stream, contentType, fileName);
        }
    }
}