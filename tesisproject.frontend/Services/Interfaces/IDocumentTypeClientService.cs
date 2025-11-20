using tesisproject.shared.DTOs.Catalog.DocumentType.Request;
using tesisproject.shared.DTOs.Catalog.DocumentType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IDocumentTypeClientService
    {
        // LIST
        Task<HttpResponseWrapper<List<DocumentTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        // SINGLE
        Task<HttpResponseWrapper<DocumentTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // KEY VALUES (search + take)
        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term,
            int? take,
            CancellationToken ct = default);

        // CREATE
        Task<HttpResponseWrapper<DocumentTypeDetailDTO?>> CreateAsync(
            AddDocumentTypeRequestDTO request,
            CancellationToken ct = default);

        // UPDATE
        Task<HttpResponseWrapper<DocumentTypeDetailDTO?>> UpdateAsync(
            UpdateDocumentTypeRequestDTO request,
            CancellationToken ct = default);
    }
}
