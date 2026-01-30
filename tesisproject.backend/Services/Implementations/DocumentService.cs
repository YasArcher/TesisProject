using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IWebHostEnvironment _env; // se mantiene por compatibilidad, pero ya no se usa para rutas
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        // Se mantiene como carpeta relativa que va a BD
        private const string DocumentsFolder = "uploads/documents";

        // Root físico configurado (Storage:RootPath)
        private readonly string _storageRootFullPath;

        public DocumentService(
            IUnitOfWork uow,
            IWebHostEnvironment env,
            IOptions<StorageOptions> storageOptions)
        {
            _uow = uow;
            _env = env;

            var root = storageOptions.Value.RootPath;

            // Fallback defensivo si no está configurado
            if (string.IsNullOrWhiteSpace(root))
            {
                // Por defecto, una carpeta "files" al lado del binario
                root = Path.Combine(_env.ContentRootPath, "files");
            }

            // Asegura existencia y normaliza
            Directory.CreateDirectory(root);
            _storageRootFullPath = Path.GetFullPath(root);
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

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newPhysicalPath)!);

                await using (var stream = new FileStream(
                    newPhysicalPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await request.File.CopyToAsync(stream, ct);
                }

                e.DocumentPath = newRelativePath;
                e.UpdatedAt = DateTime.UtcNow;
                e.UpdatedByUserId = user.IdUser;

                await _uow.SaveChangesAsync(ct);

                // Best-effort delete del archivo anterior
                try
                {
                    if (File.Exists(oldPhysicalPath))
                        File.Delete(oldPhysicalPath);
                }
                catch { /* best-effort */ }

                var refreshed = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
                return ServiceResult<DocumentResponseDTO>.Ok(Map(refreshed!));
            }
            catch (Exception ex)
            {
                // Best-effort cleanup del nuevo archivo si falló algo
                try
                {
                    if (File.Exists(newPhysicalPath))
                        File.Delete(newPhysicalPath);
                }
                catch { /* best-effort */ }

                return ServiceResult<DocumentResponseDTO>.Fail(ex.Message, ErrorType.Conflict);
            }
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
            if (request.File is null || request.File.Length == 0)
                return ServiceResult<DocumentResponseDTO>.Fail("File is empty.", ErrorType.Validation);

            if (request.DocumentTypeId <= 0)
                return ServiceResult<DocumentResponseDTO>.Fail("DocumentTypeId is required.", ErrorType.Validation);

            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
                return ServiceResult<DocumentResponseDTO>.Fail("User not found.", ErrorType.NotFound);

            var (relativePath, physicalPath) = BuildNewFilePath(request.File.FileName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

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

                return ServiceResult<DocumentResponseDTO>.Ok(Map(entity));
            }
            catch (Exception ex)
            {
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

        /// <summary>
        /// Convierte un path relativo guardado en BD (ej. uploads/documents/x.pdf)
        /// a path físico dentro del StorageRoot, bloqueando path traversal.
        /// </summary>
        private string ResolvePhysicalPath(string storedRelativePath)
        {
            if (string.IsNullOrWhiteSpace(storedRelativePath))
                throw new InvalidOperationException("Stored relative path is empty.");

            // Normaliza separadores
            var relative = storedRelativePath.Replace("/", Path.DirectorySeparatorChar.ToString());

            var combined = Path.Combine(_storageRootFullPath, relative);
            var full = Path.GetFullPath(combined);

            // Seguridad: evita que salgan de la raíz
            var rootPrefix = _storageRootFullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!full.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid document path (path traversal).");

            return full;
        }

        private (string RelativePath, string PhysicalPath) BuildNewFilePath(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";

            // Path relativo que se guarda en BD
            var relativePath = Path.Combine(DocumentsFolder, fileName).Replace("\\", "/");

            // Path físico real dentro del root configurado
            var physicalPath = ResolvePhysicalPath(relativePath);

            return (relativePath, physicalPath);
        }
    }
}
