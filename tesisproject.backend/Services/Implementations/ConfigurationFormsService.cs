using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.backend.Services.Implementations
{
    public class ConfigurationFormsService : IConfigurationFormsService
    {
        private readonly AppDbContext _db;

        public ConfigurationFormsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<FormSummaryDto>> GetFormsAsync(string? entityName = null, CancellationToken ct = default)
        {
            var normalizedEntityName = (entityName ?? string.Empty).Trim();

            var query = _db.FormDefinitions
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(normalizedEntityName))
            {
                query = query.Where(x => x.EntityName == normalizedEntityName);
            }

            return await query
                .OrderBy(x => x.EntityName)
                .ThenBy(x => x.FormName)
                .Select(x => new FormSummaryDto
                {
                    FormId = x.FormId,
                    FormKey = x.FormKey,
                    FormName = x.FormName,
                    EntityName = x.EntityName,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public async Task<List<FieldCatalogItemDto>> GetFieldsByEntityAsync(string entityName, CancellationToken ct = default)
        {
            var normalizedEntityName = (entityName ?? string.Empty).Trim();

            return await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == normalizedEntityName)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .Select(x => new FieldCatalogItemDto
                {
                    FieldId = x.FieldId,
                    EntityName = x.EntityName,
                    FieldKey = x.FieldKey,
                    FieldLabel = x.FieldLabel,
                    DataType = x.DataType,
                    SourceType = x.SourceType,
                    PhysicalTableName = x.PhysicalTableName,
                    PhysicalColumnName = x.PhysicalColumnName,
                    ReferenceTableName = x.ReferenceTableName,
                    IsSystemField = x.IsSystemField,
                    IsDynamic = x.IsDynamic,
                    IsRequired = x.IsRequired,
                    IsVisible = x.IsVisible,
                    IsEditable = x.IsEditable,
                    IsFilterable = x.IsFilterable,
                    IsActive = x.IsActive,
                    DisplayOrder = x.DisplayOrder,
                    MaxLength = x.MaxLength,
                    Placeholder = x.Placeholder,
                    HelpText = x.HelpText,
                    DefaultValue = x.DefaultValue,
                    ValidationRule = x.ValidationRule
                })
                .ToListAsync(ct);
        }

        public async Task<List<FieldCatalogItemDto>> GetDynamicFieldsByEntityAsync(string entityName, CancellationToken ct = default)
        {
            var normalizedEntityName = (entityName ?? string.Empty).Trim();

            return await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == normalizedEntityName && x.IsDynamic && x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .Select(x => new FieldCatalogItemDto
                {
                    FieldId = x.FieldId,
                    EntityName = x.EntityName,
                    FieldKey = x.FieldKey,
                    FieldLabel = x.FieldLabel,
                    DataType = x.DataType,
                    SourceType = x.SourceType,
                    PhysicalTableName = x.PhysicalTableName,
                    PhysicalColumnName = x.PhysicalColumnName,
                    ReferenceTableName = x.ReferenceTableName,
                    IsSystemField = x.IsSystemField,
                    IsDynamic = x.IsDynamic,
                    IsRequired = x.IsRequired,
                    IsVisible = x.IsVisible,
                    IsEditable = x.IsEditable,
                    IsFilterable = x.IsFilterable,
                    IsActive = x.IsActive,
                    DisplayOrder = x.DisplayOrder,
                    MaxLength = x.MaxLength,
                    Placeholder = x.Placeholder,
                    HelpText = x.HelpText,
                    DefaultValue = x.DefaultValue,
                    ValidationRule = x.ValidationRule
                })
                .ToListAsync(ct);
        }

        public async Task<List<DynamicFieldOptionDto>> GetFieldOptionsAsync(int fieldId, CancellationToken ct = default)
        {
            return await _db.DynamicFieldOptions
                .AsNoTracking()
                .Where(x => x.FieldId == fieldId && x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.DynamicFieldOptionId)
                .Select(x => new DynamicFieldOptionDto
                {
                    DynamicFieldOptionId = x.DynamicFieldOptionId,
                    FieldId = x.FieldId,
                    OptionValue = x.OptionValue,
                    OptionLabel = x.OptionLabel,
                    DisplayOrder = x.DisplayOrder
                })
                .ToListAsync(ct);
        }

        public async Task<ResolvedFormDto?> GetResolvedFormAsync(string formKey, CancellationToken ct = default)
        {
            var normalizedFormKey = (formKey ?? string.Empty).Trim();

            var form = await _db.FormDefinitions
                .AsNoTracking()
                .Include(x => x.Fields)
                    .ThenInclude(x => x.Field)
                        .ThenInclude(x => x!.Options)
                .FirstOrDefaultAsync(x => x.FormKey == normalizedFormKey && x.IsActive, ct);

            if (form is null)
            {
                return null;
            }

            var sections = form.Fields
                .Where(x => x.IsVisible && x.Field != null && x.Field.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .GroupBy(x => string.IsNullOrWhiteSpace(x.GroupName) ? "General" : x.GroupName!)
                .Select(group => new ResolvedFormSectionDto
                {
                    GroupName = group.Key,
                    DisplayOrder = group.Min(x => x.DisplayOrder),
                    Fields = group
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.FieldId)
                        .Select(x =>
                        {
                            var field = x.Field!;

                            return new ResolvedFormFieldDto
                            {
                                FieldId = field.FieldId,
                                EntityName = field.EntityName,
                                FieldKey = field.FieldKey,
                                FieldLabel = field.FieldLabel,
                                DataType = field.DataType,
                                SourceType = field.SourceType,
                                PhysicalTableName = field.PhysicalTableName,
                                PhysicalColumnName = field.PhysicalColumnName,
                                ReferenceTableName = field.ReferenceTableName,
                                IsSystemField = field.IsSystemField,
                                IsDynamic = field.IsDynamic,
                                IsRequired = x.IsRequired,
                                IsVisible = x.IsVisible,
                                IsEditable = x.IsEditable,
                                IsFilterable = field.IsFilterable,
                                IsActive = field.IsActive,
                                DisplayOrder = x.DisplayOrder,
                                ColumnSpan = x.ColumnSpan,
                                GroupName = x.GroupName,
                                MaxLength = field.MaxLength,
                                Placeholder = field.Placeholder,
                                HelpText = field.HelpText,
                                DefaultValue = field.DefaultValue,
                                ValidationRule = field.ValidationRule,
                                Options = field.Options
                                    .Where(option => option.IsActive)
                                    .OrderBy(option => option.DisplayOrder)
                                    .ThenBy(option => option.DynamicFieldOptionId)
                                    .Select(option => new DynamicFieldOptionDto
                                    {
                                        DynamicFieldOptionId = option.DynamicFieldOptionId,
                                        FieldId = option.FieldId,
                                        OptionValue = option.OptionValue,
                                        OptionLabel = option.OptionLabel,
                                        DisplayOrder = option.DisplayOrder
                                    })
                                    .ToList()
                            };
                        })
                        .ToList()
                })
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            return new ResolvedFormDto
            {
                FormId = form.FormId,
                FormKey = form.FormKey,
                FormName = form.FormName,
                EntityName = form.EntityName,
                Description = form.Description,
                Sections = sections
            };
        }
    }
}
