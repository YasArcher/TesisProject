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
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env; // se mantiene por compatibilidad, pero ya no se usa para rutas
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        // Se mantiene como carpeta relativa que va a BD
        private const string DocumentsFolder = "uploads/documents";
        private const string DocumentNotFoundMessage = "Document not found.";
        private const string DocumentTypeIdRequiredMessage = "DocumentTypeId is required.";
        private const string UserNotFoundMessage = "User not found.";
        private const string UserNotAuthenticatedMessage = "User not authenticated.";
        private const string AuthUserNotAuthenticatedCode = "AUTH_USER_NOT_AUTHENTICATED";
        private const string FileIsEmptyMessage = "File is empty.";
        private const string DocumentDeletedMessage = "Document deleted.";
        private const string FileNotFoundOnServerMessage = "File not found on server.";
        private const string DefaultContentType = "application/octet-stream";

        // Root físico configurado (Storage:RootPath)
        private readonly string _storageRootFullPath;

        public DocumentService(
            IUnitOfWork uow,
            ICurrentUserService currentUser,
            IWebHostEnvironment env,
            IOptions<StorageOptions> storageOptions)
        {
            _uow = uow;
            _currentUser = currentUser;
            _env = env;

            var root = storageOptions.Value.RootPath;

            // Asegura existencia y normaliza
            Directory.CreateDirectory(root);
            _storageRootFullPath = Path.GetFullPath(root);
        }

        public async Task<ServiceResult<DocumentResponseDTO>> GetByIdAsync(
            int documentId,
            CancellationToken ct = default)
        {
            var e = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
            if (e is null)
                return ServiceResult<DocumentResponseDTO>.Fail(DocumentNotFoundMessage, ErrorType.NotFound);

            return ServiceResult<DocumentResponseDTO>.Ok(Map(e));
        }

        public async Task<ServiceResult<DocumentResponseDTO>> UpdateAsync(
            int documentId,
            UpdateDocumentRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request.DocumentTypeId <= 0)
                    return ServiceResult<DocumentResponseDTO>.Fail(DocumentTypeIdRequiredMessage, ErrorType.Validation);

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<DocumentResponseDTO>.Fail(
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

                var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
                if (e is null)
                    return ServiceResult<DocumentResponseDTO>.Fail(DocumentNotFoundMessage, ErrorType.NotFound);

                e.DocumentTypeId = request.DocumentTypeId;
                e.ResolutionCode = request.ResolutionCode;
                e.ResolutionDate = request.ResolutionDate;
                e.UpdatedAt = DateTime.UtcNow;
                e.UpdatedByUserId = actorUserId.Value;

                await _uow.SaveChangesAsync(ct);

                var refreshed = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
                return ServiceResult<DocumentResponseDTO>.Ok(Map(refreshed!));
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<DocumentResponseDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
            }
            catch (Exception ex)
            {
                return ServiceResult<DocumentResponseDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<DocumentResponseDTO>> ReplaceFileAsync(
            int documentId,
            ReplaceDocumentFileRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request.File is null || request.File.Length == 0)
                    return ServiceResult<DocumentResponseDTO>.Fail(FileIsEmptyMessage, ErrorType.Validation);

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<DocumentResponseDTO>.Fail(
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

                var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
                if (e is null)
                    return ServiceResult<DocumentResponseDTO>.Fail(DocumentNotFoundMessage, ErrorType.NotFound);

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
                    e.UpdatedByUserId = actorUserId.Value;

                    await _uow.SaveChangesAsync(ct);

                    // Best-effort delete del archivo anterior
                    try
                    {
                        if (File.Exists(oldPhysicalPath))
                            File.Delete(oldPhysicalPath);
                    }
                    catch
                    {
                        // Best-effort
                    }

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
                    catch
                    {
                        // Best-effort
                    }

                    return ServiceResult<DocumentResponseDTO>.Fail(ex.Message, ErrorType.Conflict);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<DocumentResponseDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
            }
            catch (Exception ex)
            {
                return ServiceResult<DocumentResponseDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int documentId,
            CancellationToken ct = default)
        {
            var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
            if (e is null)
                return ServiceResult<bool>.Fail(DocumentNotFoundMessage, ErrorType.NotFound);

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

            return ServiceResult<bool>.Ok(true, DocumentDeletedMessage);
        }

        public async Task<ServiceResult<(Stream Stream, string ContentType, string FileName)>> GetContentAsync(
            int documentId,
            CancellationToken ct = default)
        {
            var e = await _uow.Documents.GetByIdAsync(new object[] { documentId }, ct);
            if (e is null)
                return ServiceResult<(Stream, string, string)>.Fail(DocumentNotFoundMessage, ErrorType.NotFound);

            var physicalPath = ResolvePhysicalPath(e.DocumentPath);
            if (!File.Exists(physicalPath))
                return ServiceResult<(Stream, string, string)>.Fail(FileNotFoundOnServerMessage, ErrorType.NotFound);

            var fileName = Path.GetFileName(physicalPath);

            if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
                contentType = DefaultContentType;

            var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return ServiceResult<(Stream, string, string)>.Ok((stream, contentType, fileName));
        }

        public async Task<ServiceResult<DocumentResponseDTO>> UploadAsync(
            UploadDocumentRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request.File is null || request.File.Length == 0)
                    return ServiceResult<DocumentResponseDTO>.Fail(FileIsEmptyMessage, ErrorType.Validation);

                if (request.DocumentTypeId <= 0)
                    return ServiceResult<DocumentResponseDTO>.Fail(DocumentTypeIdRequiredMessage, ErrorType.Validation);

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<DocumentResponseDTO>.Fail(
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

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
                        CreatedByUserId = actorUserId.Value
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
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<DocumentResponseDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
            }
            catch (Exception ex)
            {
                return ServiceResult<DocumentResponseDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            return user?.IdUser;
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
