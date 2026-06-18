using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExportTemplateClientService
    {
        // FIELDS
        Task<HttpResponseWrapper<List<ExportFieldListItemDTO>?>> ListFieldsAsync(
            CancellationToken ct = default);

        // TEMPLATES
        Task<HttpResponseWrapper<List<ExportTemplateListItemDTO>?>> ListTemplatesAsync(
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ExportTemplateDetailDTO?>> GetTemplateAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ExportTemplateDetailDTO?>> CreateTemplateAsync(
            ExportTemplateCreateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ExportTemplateDetailDTO?>> UpdateTemplateAsync(
            int id,
            ExportTemplateUpdateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteTemplateAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<FilePayloadDTO?>> ExportMatrixExcelAsync(
            ExportByTemplateRequestDTO request,
            CancellationToken ct = default);
    }
}