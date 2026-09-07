using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedExportTemplateService
    {
        Task<ServiceResult<IReadOnlyList<ExportFieldListItemDTO>>> ListFieldsAsync(
            CancellationToken ct = default);

        Task<ServiceResult<IReadOnlyList<ExportTemplateListItemDTO>>> ListTemplatesAsync(
            CancellationToken ct = default);

        Task<ServiceResult<ExportTemplateDetailDTO>> GetTemplateAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<ExportTemplateDetailDTO>> CreateTemplateAsync(
            ExportTemplateCreateRequestDTO dto,
            CancellationToken ct = default);

        Task<ServiceResult<ExportTemplateDetailDTO>> UpdateTemplateAsync(
            int id,
            ExportTemplateUpdateRequestDTO dto,
            CancellationToken ct = default);

        Task<ServiceResult<NoContent>> DeleteTemplateAsync(int id, CancellationToken ct);

    }
}