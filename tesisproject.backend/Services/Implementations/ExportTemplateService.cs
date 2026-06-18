using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Entities.Export;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExportTemplateService : IExportTemplateService
    {
        private const string MsgTemplateCreated = "Template created.";
        private const string MsgTemplateUpdated = "Template updated.";
        private const string MsgTemplateDeleted = "Template deleted.";

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

        public async Task<ServiceResult<ExportTemplateDetailDTO>> GetTemplateAsync(
            int id,
            CancellationToken ct)
        {
            if (id <= 0)
                return InvalidId<ExportTemplateDetailDTO>();

            var entity = await Templates.GetDetailByIdAsync(id, ct);
            if (entity is null)
                return TemplateNotFound<ExportTemplateDetailDTO>();

            return ServiceResult<ExportTemplateDetailDTO>.Ok(MapToDetailDTO(entity));
        }

        public async Task<ServiceResult<ExportTemplateDetailDTO>> CreateTemplateAsync(
            ExportTemplateCreateRequestDTO dto,
            CancellationToken ct)
        {
            if (dto is null)
                return RequestRequired<ExportTemplateDetailDTO>();

            var key = dto.Key?.Trim();
            var name = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(key))
                return KeyRequired<ExportTemplateDetailDTO>();

            if (string.IsNullOrWhiteSpace(name))
                return NameRequired<ExportTemplateDetailDTO>();

            if (await Templates.GetByKeyAsync(key, ct) is not null)
                return TemplateKeyAlreadyExists<ExportTemplateDetailDTO>();

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

            // Necesario para obtener TemplateId antes de crear columnas.
            await _uow.SaveChangesAsync(ct);

            var columns = await BuildColumnsEntitiesAsync(template.Id, dto.Columns, ct);
            if (columns.Count > 0)
                await Columns.AddRangeAsync(columns, ct);

            await _uow.SaveChangesAsync(ct);

            var detail = await Templates.GetDetailByIdAsync(template.Id, ct) ?? template;

            return ServiceResult<ExportTemplateDetailDTO>.Ok(
                MapToDetailDTO(detail),
                MsgTemplateCreated);
        }

        public async Task<ServiceResult<ExportTemplateDetailDTO>> UpdateTemplateAsync(
            int id,
            ExportTemplateUpdateRequestDTO dto,
            CancellationToken ct)
        {
            if (id <= 0)
                return InvalidId<ExportTemplateDetailDTO>();

            if (dto is null)
                return RequestRequired<ExportTemplateDetailDTO>();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return NameRequired<ExportTemplateDetailDTO>();

            var template = await Templates.GetDetailByIdAsync(id, ct);
            if (template is null)
                return TemplateNotFound<ExportTemplateDetailDTO>();

            template.Name = dto.Name.Trim();
            template.TargetSystem = dto.TargetSystem;
            template.Version = dto.Version;
            template.IsDefault = dto.IsDefault;
            template.IsActive = dto.IsActive;

            Templates.Update(template);

            var oldCols = await Columns.ListByTemplateIdAsync(template.Id, ct);
            if (oldCols.Count > 0)
                Columns.RemoveRange(oldCols);

            var newCols = await BuildColumnsEntitiesAsync(template.Id, dto.Columns, ct);
            if (newCols.Count > 0)
                await Columns.AddRangeAsync(newCols, ct);

            await _uow.SaveChangesAsync(ct);

            var refreshed = await Templates.GetDetailByIdAsync(template.Id, ct) ?? template;

            return ServiceResult<ExportTemplateDetailDTO>.Ok(
                MapToDetailDTO(refreshed),
                MsgTemplateUpdated);
        }

        public async Task<ServiceResult<NoContent>> DeleteTemplateAsync(
            int id,
            CancellationToken ct)
        {
            if (id <= 0)
                return InvalidId<NoContent>();

            var template = await Templates.GetDetailByIdAsync(id, ct);
            if (template is null)
                return TemplateNotFound<NoContent>();

            if (template.Columns?.Count > 0)
                Columns.RemoveRange(template.Columns);

            Templates.Remove(template);

            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent(), MsgTemplateDeleted);
        }

        // =====================================
        //   Helpers
        // =====================================

        private static ServiceResult<T> InvalidId<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.InvalidId,
                ErrorType.Validation,
                ErrorCodes.Common.InvalidId);

        private static ServiceResult<T> RequestRequired<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.RequestRequired,
                ErrorType.Validation,
                ErrorCodes.Common.RequestRequired);

        private static ServiceResult<T> NameRequired<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.NameRequired,
                ErrorType.Validation,
                ErrorCodes.Common.NameRequired);

        private static ServiceResult<T> KeyRequired<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExportTemplate.KeyRequired,
                ErrorType.Validation,
                ErrorCodes.ExportTemplate.KeyRequired);

        private static ServiceResult<T> TemplateNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExportTemplate.NotFound,
                ErrorType.NotFound,
                ErrorCodes.ExportTemplate.NotFound);

        private static ServiceResult<T> TemplateKeyAlreadyExists<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExportTemplate.KeyAlreadyExists,
                ErrorType.Conflict,
                ErrorCodes.ExportTemplate.KeyAlreadyExists);

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
                        FieldKey = c.ExportField?.Key ?? string.Empty,
                        FieldDisplayName = c.ExportField?.DisplayName ?? string.Empty,
                        TargetHeader = c.TargetHeader,
                        OrderIndex = c.OrderIndex,
                        IsRequired = c.IsRequired,
                        Format = c.Format,
                        Separator = c.Separator
                    })
                    .ToList() ?? []
            };
        }

        private async Task<List<ExportTemplateColumn>> BuildColumnsEntitiesAsync(
            int templateId,
            IEnumerable<ExportTemplateColumnUpsertDTO>? dtoColumns,
            CancellationToken ct)
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
                    Format = string.IsNullOrWhiteSpace(col.Format) ? null : col.Format.Trim(),
                    Separator = string.IsNullOrWhiteSpace(col.Separator) ? null : col.Separator.Trim()
                });
            }

            return result;
        }
    }
}