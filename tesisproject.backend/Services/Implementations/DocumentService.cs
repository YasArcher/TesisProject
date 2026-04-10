using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        // Se mantiene como carpeta relativa que va a BD
        private const string DocumentsFolder = "uploads/documents";
        private const string DocumentDeletedMessage = "Document deleted.";
        private const string DefaultContentType = "application/octet-stream";

        // Root físico configurado (Storage:RootPath)
        private readonly string _storageRootFullPath;

        public DocumentService(
            IUnitOfWork uow,
            ICurrentUserService currentUser,
            IOptions<StorageOptions> storageOptions)
        {
            _uow = uow;
            _currentUser = currentUser;

            var root = storageOptions.Value.RootPath;

            Directory.CreateDirectory(root);
            _storageRootFullPath = Path.GetFullPath(root);
        }

        public async Task<ServiceResult<DocumentResponseDTO>> GetByIdAsync(
            int documentId,
            CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
                if (e is null)
                {
                    return ServiceResult<DocumentResponseDTO>.Fail(
                        ErrorMessages.Document.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Document.NotFound);
                }

                return ServiceResult<DocumentResponseDTO>.Ok(Map(e));
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<DocumentResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<DocumentResponseDTO>();
            }
        }

        public async Task<ServiceResult<DocumentResponseDTO>> UpdateAsync(
            int documentId,
            UpdateDocumentRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request.DocumentTypeId <= 0)
                {
                    return ValidationFailure<DocumentResponseDTO>(
                        ErrorMessages.Document.DocumentTypeIdRequired,
                        ErrorCodes.Document.DocumentTypeIdRequired,
                        nameof(UpdateDocumentRequestDTO.DocumentTypeId));
                }

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return FailActorUserNotFound<DocumentResponseDTO>();
                }

                var e = await _uow.Documents.GetByIdAsync([documentId], ct);
                if (e is null)
                {
                    return ServiceResult<DocumentResponseDTO>.Fail(
                        ErrorMessages.Document.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Document.NotFound);
                }

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
                return FailUnauthorized<DocumentResponseDTO>();
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<DocumentResponseDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<DocumentResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<DocumentResponseDTO>();
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
                {
                    return ValidationFailure<DocumentResponseDTO>(
                        ErrorMessages.Document.FileEmpty,
                        ErrorCodes.Document.FileEmpty,
                        nameof(ReplaceDocumentFileRequestDTO.File));
                }

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return FailActorUserNotFound<DocumentResponseDTO>();
                }

                var e = await _uow.Documents.GetByIdAsync([documentId], ct);
                if (e is null)
                {
                    return ServiceResult<DocumentResponseDTO>.Fail(
                        ErrorMessages.Document.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Document.NotFound);
                }

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

                    TryDeleteFile(oldPhysicalPath);

                    var refreshed = await _uow.Documents.GetByIdWithRefsAsync(documentId, ct);
                    return ServiceResult<DocumentResponseDTO>.Ok(Map(refreshed!));
                }
                catch (OperationCanceledException)
                {
                    TryDeleteFile(newPhysicalPath);
                    return FailOperationCanceled<DocumentResponseDTO>();
                }
                catch (DbUpdateException)
                {
                    TryDeleteFile(newPhysicalPath);
                    return FailConflict<DocumentResponseDTO>();
                }
                catch (Exception)
                {
                    TryDeleteFile(newPhysicalPath);

                    return ServiceResult<DocumentResponseDTO>.Fail(
                        ErrorMessages.Document.FileReplaceFailed,
                        ErrorType.Conflict,
                        ErrorCodes.Document.FileReplaceFailed);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return FailUnauthorized<DocumentResponseDTO>();
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<DocumentResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<DocumentResponseDTO>();
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int documentId,
            CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Documents.GetByIdAsync([documentId], ct);
                if (e is null)
                {
                    return ServiceResult<bool>.Fail(
                        ErrorMessages.Document.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Document.NotFound);
                }

                var physicalPath = ResolvePhysicalPath(e.DocumentPath);

                _uow.Documents.Remove(e);
                await _uow.SaveChangesAsync(ct);

                TryDeleteFile(physicalPath);

                return ServiceResult<bool>.Ok(true, DocumentDeletedMessage);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<bool>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<bool>();
            }
            catch (Exception)
            {
                return FailUnexpected<bool>();
            }
        }

        public async Task<ServiceResult<(Stream Stream, string ContentType, string FileName)>> GetContentAsync(
            int documentId,
            CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Documents.GetByIdAsync([documentId], ct);
                if (e is null)
                {
                    return ServiceResult<(Stream, string, string)>.Fail(
                        ErrorMessages.Document.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Document.NotFound);
                }

                var physicalPath = ResolvePhysicalPath(e.DocumentPath);
                if (!File.Exists(physicalPath))
                {
                    return ServiceResult<(Stream, string, string)>.Fail(
                        ErrorMessages.Document.FileNotFoundOnServer,
                        ErrorType.NotFound,
                        ErrorCodes.Document.FileNotFoundOnServer);
                }

                var fileName = Path.GetFileName(physicalPath);

                if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
                    contentType = DefaultContentType;

                var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return ServiceResult<(Stream, string, string)>.Ok((stream, contentType, fileName));
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<(Stream, string, string)>();
            }
            catch (Exception)
            {
                return FailUnexpected<(Stream, string, string)>();
            }
        }

        public async Task<ServiceResult<DocumentResponseDTO>> UploadAsync(
            UploadDocumentRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request.File is null || request.File.Length == 0)
                {
                    return ValidationFailure<DocumentResponseDTO>(
                        ErrorMessages.Document.FileEmpty,
                        ErrorCodes.Document.FileEmpty,
                        nameof(UploadDocumentRequestDTO.File));
                }

                if (request.DocumentTypeId <= 0)
                {
                    return ValidationFailure<DocumentResponseDTO>(
                        ErrorMessages.Document.DocumentTypeIdRequired,
                        ErrorCodes.Document.DocumentTypeIdRequired,
                        nameof(UploadDocumentRequestDTO.DocumentTypeId));
                }

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return FailActorUserNotFound<DocumentResponseDTO>();
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
                catch (OperationCanceledException)
                {
                    TryDeleteFile(physicalPath);
                    return FailOperationCanceled<DocumentResponseDTO>();
                }
                catch (DbUpdateException)
                {
                    TryDeleteFile(physicalPath);
                    return FailConflict<DocumentResponseDTO>();
                }
                catch (Exception)
                {
                    TryDeleteFile(physicalPath);

                    return ServiceResult<DocumentResponseDTO>.Fail(
                        ErrorMessages.Document.UploadFailed,
                        ErrorType.Conflict,
                        ErrorCodes.Document.UploadFailed);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return FailUnauthorized<DocumentResponseDTO>();
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<DocumentResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<DocumentResponseDTO>();
            }
        }

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            return user?.IdUser;
        }

        private static ServiceResult<T> FailUnauthorized<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Auth.UserNotAuthenticated,
                ErrorType.Unauthorized,
                ErrorCodes.Auth.UserNotAuthenticated);

        private static ServiceResult<T> FailActorUserNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Auth.ActorUserNotFound,
                ErrorType.NotFound,
                ErrorCodes.Auth.ActorUserNotFound);

        private static ServiceResult<T> FailConflict<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.PersistenceConflict,
                ErrorType.Conflict,
                ErrorCodes.Common.PersistenceConflict);

        private static ServiceResult<T> FailUnexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<T> FailOperationCanceled<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.OperationCanceled,
                ErrorType.Unexpected,
                ErrorCodes.Common.OperationCanceled);

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
        }

        private static void TryDeleteFile(string? physicalPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(physicalPath) && File.Exists(physicalPath))
                    File.Delete(physicalPath);
            }
            catch
            {
                // Best-effort delete
            }
        }

        private static DocumentResponseDTO Map(Document e)
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

            var relative = storedRelativePath.Replace("/", Path.DirectorySeparatorChar.ToString());

            var combined = Path.Combine(_storageRootFullPath, relative);
            var full = Path.GetFullPath(combined);

            var rootPrefix = _storageRootFullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!full.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid document path (path traversal).");

            return full;
        }

        private (string RelativePath, string PhysicalPath) BuildNewFilePath(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";

            var relativePath = Path.Combine(DocumentsFolder, fileName).Replace("\\", "/");
            var physicalPath = ResolvePhysicalPath(relativePath);

            return (relativePath, physicalPath);
        }
    }
}