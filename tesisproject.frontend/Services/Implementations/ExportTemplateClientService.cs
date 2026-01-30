using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExportTemplateClientService : IExportTemplateClientService
    {
        private readonly IApiClient _api;

        private readonly string _baseUrl = "exporttemplates";

        public ExportTemplateClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //          FIELDS
        // =========================

        public Task<HttpResponseWrapper<List<ExportFieldListItemDTO>?>> ListFieldsAsync(
            CancellationToken ct = default)
        {
            // GET: api/ExportTemplates/fields
            return _api.GetAsync<List<ExportFieldListItemDTO>>(
                $"{_baseUrl}/fields",
                ct
            );
        }

        // =========================
        //        TEMPLATES LIST
        // =========================

        public Task<HttpResponseWrapper<List<ExportTemplateListItemDTO>?>> ListTemplatesAsync(CancellationToken ct = default)
        {
            return _api.GetAsync<List<ExportTemplateListItemDTO>>(
    _baseUrl,
    ct
);
        }

        // =========================
        //         SINGLE GET
        // =========================

        public Task<HttpResponseWrapper<ExportTemplateDetailDTO?>> GetTemplateAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // GET: api/ExportTemplates/{id}
            return _api.GetAsync<ExportTemplateDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<ExportTemplateDetailDTO?>> CreateTemplateAsync(
            ExportTemplateCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/ExportTemplates
            return _api.PostAsync<ExportTemplateCreateRequestDTO, ExportTemplateDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<ExportTemplateDetailDTO?>> UpdateTemplateAsync(
            int id,
            ExportTemplateUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // PUT: api/ExportTemplates/{id}
            return _api.PutAsync<ExportTemplateUpdateRequestDTO, ExportTemplateDetailDTO>(
                $"{_baseUrl}/{id}",
                request,
                ct
            );
        }

        // =========================
        //          DELETE
        // =========================

        public Task<HttpResponseWrapper<NoContent?>> DeleteTemplateAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // DELETE: api/ExportTemplates/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //   EXPORT MATRIX EXCEL
        // =========================
        public Task<HttpResponseMessage> ExportMatrixExcelAsync(
            ExportRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            return _api.PostRawAsync(
                $"{_baseUrl}/matrix-excel",
                request,
                ct
            );
        }
    }
}