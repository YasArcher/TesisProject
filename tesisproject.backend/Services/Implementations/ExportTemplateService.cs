using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Entities.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExportTemplateService : IExportTemplateService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ExportTemplateService> _logger;

        private IExportFieldRepository Fields => _uow.ExportFields;
        private IExportTemplateRepository Templates => _uow.ExportTemplates;
        private IExportTemplateColumnRepository Columns => _uow.ExportTemplateColumns;

        public ExportTemplateService(IUnitOfWork uow, ILogger<ExportTemplateService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        // =====================================
        //   FIELDS
        // =====================================

        public async Task<ServiceResult<IReadOnlyList<ExportFieldListItemDTO>>> ListFieldsAsync(
            CancellationToken ct = default)
        {
            var list = await Fields.ListAsync(ct);

            var dto = list.Select(f => new ExportFieldListItemDTO
            {
                Id = f.Id,
                Key = f.Key,
                DisplayName = f.DisplayName,
                DefaultHeader = f.DefaultHeader ?? string.Empty,
                IsActive = f.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<ExportFieldListItemDTO>>.Ok(dto);
        }

        // =====================================
        //   TEMPLATES
        // =====================================

        public async Task<ServiceResult<IReadOnlyList<ExportTemplateListItemDTO>>> ListTemplatesAsync(
            CancellationToken ct = default)
        {
            var list = await Templates.ListAsync(ct);

            var dto = list.Select(t => new ExportTemplateListItemDTO
            {
                Id = t.Id,
                Key = t.Key,
                Name = t.Name,
                TargetSystem = t.TargetSystem,
                Version = t.Version,
                IsDefault = t.IsDefault,
                IsActive = t.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<ExportTemplateListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ExportTemplateDetailDTO>> GetTemplateAsync(int id, CancellationToken ct)
        {
            if (id <= 0)
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Id is required.", ErrorType.Validation);

            var entity = await Templates.GetDetailByIdAsync(id, ct);
            if (entity is null)
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Template not found.", ErrorType.NotFound);

            return ServiceResult<ExportTemplateDetailDTO>.Ok(MapToDetailDTO(entity));
        }

        public async Task<ServiceResult<ExportTemplateDetailDTO>> CreateTemplateAsync(
            ExportTemplateCreateRequestDTO dto, CancellationToken ct)
        {
            if (dto is null)
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Request is required.", ErrorType.Validation);

            var key = dto.Key?.Trim();
            var name = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(key))
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Key is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            // Validación unique key (normalizada)
            if (await Templates.GetByKeyAsync(key, ct) is not null)
                return ServiceResult<ExportTemplateDetailDTO>.Fail(
                    $"Template with key '{key}' already exists.",
                    ErrorType.Validation
                );

            var template = new ExportTemplate
            {
                Key = key,
                Name = name,
                TargetSystem = dto.TargetSystem,
                Version = dto.Version,
                IsDefault = dto.IsDefault,
                IsActive = dto.IsActive
            };

            await Templates.AddAsync(template, ct);

            // Guardamos INICIAL para obtener ID (necesario porque columnas usan TemplateId)
            await _uow.SaveChangesAsync(ct);

            // Construcción de columnas
            var columns = await BuildColumnsEntitiesAsync(template.Id, dto.Columns, ct);
            if (columns.Count > 0)
                await Columns.AddRangeAsync(columns, ct);

            // Guardamos una sola vez más
            await _uow.SaveChangesAsync(ct);

            var detail = await Templates.GetDetailByIdAsync(template.Id, ct) ?? template;

            return ServiceResult<ExportTemplateDetailDTO>.Ok(
                MapToDetailDTO(detail),
                "Template created."
            );
        }

        public async Task<ServiceResult<ExportTemplateDetailDTO>> UpdateTemplateAsync(
            int id, ExportTemplateUpdateRequestDTO dto, CancellationToken ct)
        {
            if (id <= 0)
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Id is required.", ErrorType.Validation);

            if (dto is null)
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Request is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var template = await Templates.GetDetailByIdAsync(id, ct);
            if (template is null)
                return ServiceResult<ExportTemplateDetailDTO>.Fail("Template not found.", ErrorType.NotFound);

            template.Name = dto.Name.Trim();
            template.TargetSystem = dto.TargetSystem;
            template.Version = dto.Version;
            template.IsDefault = dto.IsDefault;
            template.IsActive = dto.IsActive;

            Templates.Update(template);

            // Eliminar columnas previas
            var oldCols = await Columns.ListByTemplateIdAsync(template.Id, ct);
            if (oldCols.Count > 0)
                Columns.RemoveRange(oldCols);

            // Agregar nuevas columnas
            var newCols = await BuildColumnsEntitiesAsync(template.Id, dto.Columns, ct);
            if (newCols.Count > 0)
                await Columns.AddRangeAsync(newCols, ct);

            // ✅ Guardar una sola vez todo (template + columnas)
            await _uow.SaveChangesAsync(ct);

            var refreshed = await Templates.GetDetailByIdAsync(template.Id, ct) ?? template;

            return ServiceResult<ExportTemplateDetailDTO>.Ok(
                MapToDetailDTO(refreshed),
                "Template updated."
            );
        }

        public async Task<ServiceResult<NoContent>> DeleteTemplateAsync(int id, CancellationToken ct)
        {
            if (id <= 0)
                return ServiceResult<NoContent>.Fail("Id is required.", ErrorType.Validation);

            var template = await Templates.GetDetailByIdAsync(id, ct);
            if (template is null)
                return ServiceResult<NoContent>.Fail("Template not found.", ErrorType.NotFound);

            // Primero remover columnas
            if (template.Columns?.Count > 0)
                Columns.RemoveRange(template.Columns);

            // Luego remover template
            Templates.Remove(template);

            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent(), "Template deleted.");
        }

        // =====================================
        //   Helpers
        // =====================================

        private ExportTemplateDetailDTO MapToDetailDTO(ExportTemplate entity)
        {
            return new ExportTemplateDetailDTO
            {
                Id = entity.Id,
                Key = entity.Key,
                Name = entity.Name,
                TargetSystem = entity.TargetSystem,
                Version = entity.Version,
                IsDefault = entity.IsDefault,
                IsActive = entity.IsActive,

                Columns = entity.Columns?
                    .OrderBy(c => c.OrderIndex)
                    .Select(c => new ExportTemplateColumnDTO
                    {
                        Id = c.Id,
                        FieldId = c.ExportFieldId,
                        FieldKey = c.ExportField?.Key ?? "",
                        FieldDisplayName = c.ExportField?.DisplayName ?? "",
                        TargetHeader = c.TargetHeader,
                        OrderIndex = c.OrderIndex,
                        IsRequired = c.IsRequired,
                        Format = c.Format,
                        Separator = c.Separator
                    })
                    .ToList() ?? new()
            };
        }

        private async Task<List<ExportTemplateColumn>> BuildColumnsEntitiesAsync(
            int templateId, IEnumerable<ExportTemplateColumnUpsertDTO>? dtoColumns, CancellationToken ct)
        {
            var result = new List<ExportTemplateColumn>();

            if (dtoColumns is null)
                return result;

            var allFields = await Fields.ListAsync(ct);
            var fieldsById = allFields.ToDictionary(f => f.Id);

            foreach (var col in dtoColumns.OrderBy(c => c.OrderIndex))
            {
                if (!fieldsById.TryGetValue(col.FieldId, out var field))
                {
                    _logger.LogWarning("Skipping FieldId {FieldId} (not found).", col.FieldId);
                    continue;
                }

                var header = string.IsNullOrWhiteSpace(col.TargetHeader)
                    ? (field.DefaultHeader ?? field.Key)
                    : col.TargetHeader.Trim();

                result.Add(new ExportTemplateColumn
                {
                    TemplateId = templateId,
                    ExportFieldId = col.FieldId,
                    TargetHeader = header,
                    OrderIndex = col.OrderIndex,
                    IsRequired = col.IsRequired,
                    Format = string.IsNullOrWhiteSpace(col.Format) ? null : col.Format,
                    Separator = string.IsNullOrWhiteSpace(col.Separator) ? null : col.Separator
                });
            }

            return result;
        }
    }
}