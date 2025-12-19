using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IWebHostEnvironment _env;

        // Carpeta relativa donde se guardarán los documentos (ajústala si quieres)
        private const string DocumentsFolder = "uploads/documents";

        public DocumentService(IUnitOfWork uow, IWebHostEnvironment env)
        {
            _uow = uow;
            _env = env;
        }

        public async Task<ServiceResult<DocumentResponseDTO>> UploadAsync(
            UploadDocumentRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            // ===== Validaciones básicas =====
            if (request.File is null || request.File.Length == 0)
                return ServiceResult<DocumentResponseDTO>.Fail(
                    "File is empty.",
                    ErrorType.Validation);

            if (request.DocumentTypeId <= 0)
                return ServiceResult<DocumentResponseDTO>.Fail(
                    "DocumentTypeId is required.",
                    ErrorType.Validation);

            try
            {
                // ================================
                // 1. Guardar archivo en el servidor
                // ================================
                var webRoot = _env.WebRootPath;

                // Si por alguna razón no hay wwwroot, lo creamos bajo ContentRoot
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
                    if (!Directory.Exists(webRoot))
                        Directory.CreateDirectory(webRoot);
                }

                var documentsRoot = Path.Combine(webRoot, DocumentsFolder);

                if (!Directory.Exists(documentsRoot))
                    Directory.CreateDirectory(documentsRoot);

                var extension = Path.GetExtension(request.File.FileName);
                var fileName = $"{Guid.NewGuid():N}{extension}";

                var physicalPath = Path.Combine(documentsRoot, fileName);

                // Ruta relativa que se guardará en DB (para servirlo luego como estático o con endpoint)
                var relativePath = Path.Combine(DocumentsFolder, fileName)
                    .Replace("\\", "/");

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream, ct);
                }
                var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
                if (user is null)
                {
                    return ServiceResult<DocumentResponseDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound
                    );
                }

                // ================================
                // 2. Crear la entidad Document
                // ================================
                var nowUtc = DateTime.UtcNow;

                var entity = new Document
                {
                    DocumentTypeId = request.DocumentTypeId,
                    DocumentPath = relativePath,
                    ResolutionCode = request.ResolutionCode,
                    ResolutionDate = request.ResolutionDate,
                    CreatedAt = nowUtc,
                    CreatedByUserId = user.IdUser
                };

                await _uow.Documents.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                // ================================
                // 3. Mapear a DTO de respuesta
                // ================================
                var dto = new DocumentResponseDTO
                {
                    DocumentId = entity.DocumentId,
                    DocumentTypeId = entity.DocumentTypeId,
                    DocumentPath = entity.DocumentPath,
                    ResolutionCode = entity.ResolutionCode,
                    ResolutionDate = entity.ResolutionDate,
                    CreatedAt = entity.CreatedAt,
                    CreatedByUserId = entity.CreatedByUserId
                };

                return ServiceResult<DocumentResponseDTO>.Ok(dto);
            }
            catch (Exception ex)
            {
                // Aquí podrías loguear ex
                return ServiceResult<DocumentResponseDTO>.Fail(
                    ex.Message,
                    ErrorType.Conflict);
            }
        }
    }
}