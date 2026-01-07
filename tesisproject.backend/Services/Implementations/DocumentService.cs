using Microsoft.AspNetCore.StaticFiles;
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
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        private const string DocumentsFolder = "uploads/documents";

        public DocumentService(IUnitOfWork uow, IWebHostEnvironment env)
        {
            _uow = uow;
            _env = env;
        }

        public async Task<ServiceResult<DocumentResponseDTO>> GetByIdAsync(int documentId, CancellationToken ct = default)
        {
            var e = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
            if (e is null)
                return ServiceResult<DocumentResponseDTO>.Fail("Document not found.", ErrorType.NotFound);

            return ServiceResult<DocumentResponseDTO>.Ok(Map(e));
        }

        public async Task<ServiceResult<DocumentResponseDTO>> UpdateAsync(
            int documentId,
            UpdateDocumentRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            if (request.DocumentTypeId <= 0)
                return ServiceResult<DocumentResponseDTO>.Fail("DocumentTypeId is required.", ErrorType.Validation);

            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
                return ServiceResult<DocumentResponseDTO>.Fail("User not found.", ErrorType.NotFound);

            var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
            if (e is null)
                return ServiceResult<DocumentResponseDTO>.Fail("Document not found.", ErrorType.NotFound);

            e.DocumentTypeId = request.DocumentTypeId;
            e.ResolutionCode = request.ResolutionCode;
            e.ResolutionDate = request.ResolutionDate;

            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedByUserId = user.IdUser;

            await _uow.SaveChangesAsync(ct);

            var refreshed = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
            return ServiceResult<DocumentResponseDTO>.Ok(Map(refreshed!));
        }

        public async Task<ServiceResult<DocumentResponseDTO>> ReplaceFileAsync(
            int documentId,
            ReplaceDocumentFileRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            if (request.File is null || request.File.Length == 0)
                return ServiceResult<DocumentResponseDTO>.Fail("File is empty.", ErrorType.Validation);

            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
                return ServiceResult<DocumentResponseDTO>.Fail("User not found.", ErrorType.NotFound);

            var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
            if (e is null)
                return ServiceResult<DocumentResponseDTO>.Fail("Document not found.", ErrorType.NotFound);

            var oldPhysicalPath = ResolvePhysicalPath(e.DocumentPath);

            var (newRelativePath, newPhysicalPath) = BuildNewFilePath(request.File.FileName);

            using (var stream = new FileStream(newPhysicalPath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream, ct);
            }

            e.DocumentPath = newRelativePath;
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedByUserId = user.IdUser;

            await _uow.SaveChangesAsync(ct);

            try
            {
                if (File.Exists(oldPhysicalPath))
                    File.Delete(oldPhysicalPath);
            }
            catch
            {
                // Best-effort delete
            }

            var refreshed = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
            return ServiceResult<DocumentResponseDTO>.Ok(Map(refreshed!));
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int documentId, CancellationToken ct = default)
        {
            var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
            if (e is null)
                return ServiceResult<bool>.Fail("Document not found.", ErrorType.NotFound);

            var physicalPath = ResolvePhysicalPath(e.DocumentPath);

            _uow.Documents.Remove(e);
            await _uow.SaveChangesAsync(ct);

            try
            {
                if (File.Exists(physicalPath))
                    File.Delete(physicalPath);
            }
            catch
            {
                // Best-effort delete
            }

            return ServiceResult<bool>.Ok(true, "Document deleted.");
        }

        public async Task<ServiceResult<(Stream Stream, string ContentType, string FileName)>> GetContentAsync(
            int documentId,
            CancellationToken ct = default)
        {
            var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
            if (e is null)
                return ServiceResult<(Stream, string, string)>.Fail("Document not found.", ErrorType.NotFound);

            var physicalPath = ResolvePhysicalPath(e.DocumentPath);
            if (!File.Exists(physicalPath))
                return ServiceResult<(Stream, string, string)>.Fail("File not found on server.", ErrorType.NotFound);

            var fileName = Path.GetFileName(physicalPath);

            if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
                contentType = "application/octet-stream";

            var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return ServiceResult<(Stream, string, string)>.Ok((stream, contentType, fileName));
        }

        public async Task<ServiceResult<DocumentResponseDTO>> UploadAsync(
            UploadDocumentRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            // ===== Validaciones básicas =====
            if (request.File is null || request.File.Length == 0)
                return ServiceResult<DocumentResponseDTO>.Fail("File is empty.", ErrorType.Validation);

            if (request.DocumentTypeId <= 0)
                return ServiceResult<DocumentResponseDTO>.Fail("DocumentTypeId is required.", ErrorType.Validation);

            // ===== Validar usuario =====
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
                return ServiceResult<DocumentResponseDTO>.Fail("User not found.", ErrorType.NotFound);

            // ===== Construir rutas con tu helper nuevo =====
            var (relativePath, physicalPath) = BuildNewFilePath(request.File.FileName);

            try
            {
                // 1) Guardar archivo
                await using (var stream = new FileStream(
                    physicalPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await request.File.CopyToAsync(stream, ct);
                }

                // 2) Crear entidad
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

                // 3) Respuesta
                return ServiceResult<DocumentResponseDTO>.Ok(Map(entity));
            }
            catch (Exception ex)
            {
                // Best-effort cleanup del archivo si falló algo luego de crearlo
                try
                {
                    if (File.Exists(physicalPath))
                        File.Delete(physicalPath);
                }
                catch
                {
                    // Best-effort delete
                }

                return ServiceResult<DocumentResponseDTO>.Fail(ex.Message, ErrorType.Conflict);
            }
        }

        private DocumentResponseDTO Map(Document e)
        {
            return new DocumentResponseDTO
            {
                DocumentId = e.DocumentId,
                DocumentTypeId = e.DocumentTypeId,
                DocumentPath = e.DocumentPath,
                ResolutionCode = e.ResolutionCode,
                ResolutionDate = e.ResolutionDate,
                CreatedAt = e.CreatedAt,
                CreatedByUserId = e.CreatedByUserId
            };
        }

        private string ResolvePhysicalPath(string storedRelativePath)
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
                if (!Directory.Exists(webRoot))
                    Directory.CreateDirectory(webRoot);
            }

            return Path.Combine(webRoot, storedRelativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private (string RelativePath, string PhysicalPath) BuildNewFilePath(string originalFileName)
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
                if (!Directory.Exists(webRoot))
                    Directory.CreateDirectory(webRoot);
            }

            var documentsRoot = Path.Combine(webRoot, DocumentsFolder);
            if (!Directory.Exists(documentsRoot))
                Directory.CreateDirectory(documentsRoot);

            var extension = Path.GetExtension(originalFileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(documentsRoot, fileName);
            var relativePath = Path.Combine(DocumentsFolder, fileName).Replace("\\", "/");

            return (relativePath, physicalPath);
        }
    }
}