using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentsController(IDocumentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Uploads a document file, stores it on the server,
        /// creates a Document record and returns its data.
        /// </summary>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(ApiResponse<DocumentResponseDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<DocumentResponseDTO>>> Upload(
            [FromForm] UploadDocumentRequestDTO request,
            CancellationToken ct)
        {
            var result = await _service.UploadAsync(request, ct);
            return result.ToActionResult();
        }
    }
}