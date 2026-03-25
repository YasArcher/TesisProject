using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalogs;
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
                .AsNoTracking();

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
            await EnsureVenueMetadataIfNeededAsync(normalizedEntityName, ct);

            return await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == normalizedEntityName)
                .Where(x => normalizedEntityName != "Article" || x.FieldKey != "VenueId")
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

        public async Task<List<FormFieldAdminDto>> GetFormFieldsAsync(int formId, CancellationToken ct = default)
        {
            await EnsureVenueMetadataIfNeededAsync("Article", ct);
            return await _db.FormFieldDefinitions
                .AsNoTracking()
                .Include(x => x.Field)
                .Where(x => x.FormId == formId)
                .Where(x => x.Field == null || x.Field.FieldKey != "VenueId")
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FormFieldId)
                .Select(x => new FormFieldAdminDto
                {
                    FormFieldId = x.FormFieldId,
                    FormId = x.FormId,
                    FieldId = x.FieldId,
                    FieldKey = x.Field != null ? x.Field.FieldKey : string.Empty,
                    FieldLabel = x.Field != null ? x.Field.FieldLabel : string.Empty,
                    EntityName = x.Field != null ? x.Field.EntityName : string.Empty,
                    DataType = x.Field != null ? x.Field.DataType : string.Empty,
                    IsDynamic = x.Field != null && x.Field.IsDynamic,
                    IsVisible = x.IsVisible,
                    IsRequired = x.IsRequired,
                    IsEditable = x.IsEditable,
                    DisplayOrder = x.DisplayOrder,
                    GroupName = x.GroupName,
                    ColumnSpan = x.ColumnSpan,
                    FieldIsActive = x.Field != null && x.Field.IsActive
                })
                .ToListAsync(ct);
        }

        public async Task<List<FieldCatalogItemDto>> GetDynamicFieldsByEntityAsync(string entityName, CancellationToken ct = default)
        {
            var normalizedEntityName = (entityName ?? string.Empty).Trim();
            await EnsureVenueMetadataIfNeededAsync(normalizedEntityName, ct);

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
                .Where(x => x.FieldId == fieldId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.DynamicFieldOptionId)
                .Select(x => new DynamicFieldOptionDto
                {
                    DynamicFieldOptionId = x.DynamicFieldOptionId,
                    FieldId = x.FieldId,
                    OptionValue = x.OptionValue,
                    OptionLabel = x.OptionLabel,
                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public async Task<DynamicFieldOptionDto> CreateFieldOptionAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default)
        {
            var field = await _db.FieldCatalogEntries.FirstOrDefaultAsync(x => x.FieldId == fieldId, ct);
            if (field is null)
            {
                throw new InvalidOperationException("El campo no existe.");
            }

            var optionValue = (request.OptionValue ?? string.Empty).Trim();
            var optionLabel = (request.OptionLabel ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(optionValue) || string.IsNullOrWhiteSpace(optionLabel))
            {
                throw new InvalidOperationException("OptionValue y OptionLabel son obligatorios.");
            }

            var option = new Data.Entities.DynamicFieldOption
            {
                FieldId = fieldId,
                OptionValue = optionValue,
                OptionLabel = optionLabel,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.DynamicFieldOptions.Add(option);
            await _db.SaveChangesAsync(ct);

            return new DynamicFieldOptionDto
            {
                DynamicFieldOptionId = option.DynamicFieldOptionId,
                FieldId = option.FieldId,
                OptionValue = option.OptionValue,
                OptionLabel = option.OptionLabel,
                DisplayOrder = option.DisplayOrder,
                IsActive = option.IsActive
            };
        }

        public async Task<DynamicFieldOptionDto?> UpdateFieldOptionAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default)
        {
            var option = await _db.DynamicFieldOptions.FirstOrDefaultAsync(x => x.FieldId == fieldId && x.DynamicFieldOptionId == optionId, ct);
            if (option is null)
            {
                return null;
            }

            option.OptionValue = string.IsNullOrWhiteSpace(request.OptionValue) ? option.OptionValue : request.OptionValue.Trim();
            option.OptionLabel = string.IsNullOrWhiteSpace(request.OptionLabel) ? option.OptionLabel : request.OptionLabel.Trim();
            option.DisplayOrder = request.DisplayOrder;
            option.IsActive = request.IsActive;
            option.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return new DynamicFieldOptionDto
            {
                DynamicFieldOptionId = option.DynamicFieldOptionId,
                FieldId = option.FieldId,
                OptionValue = option.OptionValue,
                OptionLabel = option.OptionLabel,
                DisplayOrder = option.DisplayOrder,
                IsActive = option.IsActive
            };
        }

        public async Task<List<CatalogItemDto>> GetCatalogItemsByFieldAsync(int fieldId, int? parentId = null, CancellationToken ct = default)
        {
            var field = await _db.FieldCatalogEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FieldId == fieldId && x.IsActive, ct);

            if (field is null)
            {
                return new List<CatalogItemDto>();
            }

            var referenceName = NormalizeReferenceName(field.ReferenceTableName);
            var fieldKey = (field.FieldKey ?? string.Empty).Trim();

            return referenceName switch
            {
                "AcademicTerms" => await _db.AcademicTerms
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.AcademicTermId, Name = x.Name })
                    .ToListAsync(ct),

                "PublicationStatuses" => await _db.PublicationStatuses
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.PublicationStatusId, Name = x.Name })
                    .ToListAsync(ct),

                "ResearchLines" => await _db.ResearchLines
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.ResearchLineId, Name = x.Name })
                    .ToListAsync(ct),

                "BroadFields" => await _db.BroadFields
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.BroadFieldId, Name = x.Name })
                    .ToListAsync(ct),

                "SpecificFields" => await _db.SpecificFields
                    .AsNoTracking()
                    .Where(x => !parentId.HasValue || x.BroadFieldId == parentId.Value)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.SpecificFieldId, Name = x.Name })
                    .ToListAsync(ct),

                "DetailedFields" => await _db.DetailedFields
                    .AsNoTracking()
                    .Where(x => !parentId.HasValue || x.SpecificFieldId == parentId.Value)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.DetailedFieldId, Name = x.Name })
                    .ToListAsync(ct),

                "Venues" => await _db.Venues
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.VenueId, Name = x.Name })
                    .ToListAsync(ct),

                "Projects" => await _db.Projects
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.Id, Name = x.Name })
                    .ToListAsync(ct),

                "IndexingSources" => await _db.IndexingSources
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.IndexingSourceId, Name = x.Name })
                    .ToListAsync(ct),

                _ when string.Equals(fieldKey, "SpecificFieldId", StringComparison.OrdinalIgnoreCase) => await _db.SpecificFields
                    .AsNoTracking()
                    .Where(x => !parentId.HasValue || x.BroadFieldId == parentId.Value)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.SpecificFieldId, Name = x.Name })
                    .ToListAsync(ct),

                _ when string.Equals(fieldKey, "DetailedFieldId", StringComparison.OrdinalIgnoreCase) => await _db.DetailedFields
                    .AsNoTracking()
                    .Where(x => !parentId.HasValue || x.SpecificFieldId == parentId.Value)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto { Id = x.DetailedFieldId, Name = x.Name })
                    .ToListAsync(ct),

                _ => new List<CatalogItemDto>()
            };
        }

        public async Task<FormDefinitionAdminDto> CreateFormAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
        {
            var entityName = (request.EntityName ?? string.Empty).Trim();
            var formKey = (request.FormKey ?? string.Empty).Trim();
            var formName = (request.FormName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(formKey) || string.IsNullOrWhiteSpace(formName))
            {
                throw new InvalidOperationException("EntityName, FormKey y FormName son obligatorios.");
            }

            var exists = await _db.FormDefinitions.AnyAsync(x => x.FormKey == formKey, ct);
            if (exists)
            {
                throw new InvalidOperationException("Ya existe un formulario con el mismo FormKey.");
            }

            var form = new Data.Entities.FormDefinition
            {
                EntityName = entityName,
                FormKey = formKey,
                FormName = formName,
                Description = NormalizeNullable(request.Description),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.FormDefinitions.Add(form);
            await _db.SaveChangesAsync(ct);

            return new FormDefinitionAdminDto
            {
                FormId = form.FormId,
                FormKey = form.FormKey,
                FormName = form.FormName,
                EntityName = form.EntityName,
                Description = form.Description,
                IsActive = form.IsActive
            };
        }

        public async Task<FormDefinitionAdminDto?> UpdateFormAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default)
        {
            var form = await _db.FormDefinitions.FirstOrDefaultAsync(x => x.FormId == formId, ct);
            if (form is null)
            {
                return null;
            }

            form.FormName = string.IsNullOrWhiteSpace(request.FormName) ? form.FormName : request.FormName.Trim();
            form.Description = NormalizeNullable(request.Description);
            form.IsActive = request.IsActive;
            form.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return new FormDefinitionAdminDto
            {
                FormId = form.FormId,
                FormKey = form.FormKey,
                FormName = form.FormName,
                EntityName = form.EntityName,
                Description = form.Description,
                IsActive = form.IsActive
            };
        }

        public async Task<bool> DeleteFormAsync(int formId, CancellationToken ct = default)
        {
            var form = await _db.FormDefinitions
                .Include(x => x.Fields)
                .FirstOrDefaultAsync(x => x.FormId == formId, ct);

            if (form is null)
            {
                return false;
            }

            if (form.Fields.Count > 0)
            {
                _db.FormFieldDefinitions.RemoveRange(form.Fields);
            }

            _db.FormDefinitions.Remove(form);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<FieldCatalogItemDto> CreateDynamicFieldAsync(CreateDynamicFieldRequest request, CancellationToken ct = default)
        {
            var entityName = (request.EntityName ?? string.Empty).Trim();
            var fieldKey = (request.FieldKey ?? string.Empty).Trim();
            var fieldLabel = (request.FieldLabel ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(fieldKey) || string.IsNullOrWhiteSpace(fieldLabel))
            {
                throw new InvalidOperationException("EntityName, FieldKey y FieldLabel son obligatorios.");
            }

            var exists = await _db.FieldCatalogEntries.AnyAsync(x => x.EntityName == entityName && x.FieldKey == fieldKey, ct);
            if (exists)
            {
                throw new InvalidOperationException("Ya existe un campo con el mismo FieldKey para esa entidad.");
            }

            var field = new Data.Entities.FieldCatalogEntry
            {
                EntityName = entityName,
                FieldKey = fieldKey,
                FieldLabel = fieldLabel,
                DataType = string.IsNullOrWhiteSpace(request.DataType) ? "string" : request.DataType.Trim(),
                SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Dynamic" : request.SourceType.Trim(),
                IsSystemField = false,
                IsDynamic = true,
                IsRequired = request.IsRequired,
                IsVisible = request.IsVisible,
                IsEditable = request.IsEditable,
                IsFilterable = request.IsFilterable,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                MaxLength = request.MaxLength,
                Placeholder = NormalizeNullable(request.Placeholder),
                HelpText = NormalizeNullable(request.HelpText),
                DefaultValue = NormalizeNullable(request.DefaultValue),
                ValidationRule = NormalizeNullable(request.ValidationRule),
                CreatedAt = DateTime.UtcNow
            };

            _db.FieldCatalogEntries.Add(field);
            await _db.SaveChangesAsync(ct);

            return MapField(field);
        }

        public async Task<FieldCatalogItemDto?> UpdateFieldAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default)
        {
            var field = await _db.FieldCatalogEntries.FirstOrDefaultAsync(x => x.FieldId == fieldId, ct);
            if (field is null)
            {
                return null;
            }

            field.FieldLabel = string.IsNullOrWhiteSpace(request.FieldLabel) ? field.FieldLabel : request.FieldLabel.Trim();
            field.IsRequired = request.IsRequired;
            field.IsVisible = request.IsVisible;
            field.IsEditable = request.IsEditable;
            field.IsFilterable = request.IsFilterable;
            field.IsActive = request.IsActive;
            field.DisplayOrder = request.DisplayOrder;
            field.MaxLength = request.MaxLength;
            field.Placeholder = NormalizeNullable(request.Placeholder);
            field.HelpText = NormalizeNullable(request.HelpText);
            field.DefaultValue = NormalizeNullable(request.DefaultValue);
            field.ValidationRule = NormalizeNullable(request.ValidationRule);
            field.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return MapField(field);
        }

        public async Task<FormFieldAdminDto> AddFieldToFormAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default)
        {
            var formExists = await _db.FormDefinitions.AnyAsync(x => x.FormId == formId, ct);
            if (!formExists)
            {
                throw new InvalidOperationException("El formulario no existe.");
            }

            var field = await _db.FieldCatalogEntries.FirstOrDefaultAsync(x => x.FieldId == request.FieldId, ct);
            if (field is null)
            {
                throw new InvalidOperationException("El campo no existe.");
            }

            var exists = await _db.FormFieldDefinitions.AnyAsync(x => x.FormId == formId && x.FieldId == request.FieldId, ct);
            if (exists)
            {
                throw new InvalidOperationException("El campo ya está asignado al formulario.");
            }

            var formField = new Data.Entities.FormFieldDefinition
            {
                FormId = formId,
                FieldId = request.FieldId,
                IsVisible = request.IsVisible,
                IsRequired = request.IsRequired,
                IsEditable = request.IsEditable,
                DisplayOrder = request.DisplayOrder,
                GroupName = NormalizeNullable(request.GroupName),
                ColumnSpan = request.ColumnSpan,
                CreatedAt = DateTime.UtcNow
            };

            _db.FormFieldDefinitions.Add(formField);
            await _db.SaveChangesAsync(ct);

            return new FormFieldAdminDto
            {
                FormFieldId = formField.FormFieldId,
                FormId = formField.FormId,
                FieldId = field.FieldId,
                FieldKey = field.FieldKey,
                FieldLabel = field.FieldLabel,
                EntityName = field.EntityName,
                DataType = field.DataType,
                IsDynamic = field.IsDynamic,
                IsVisible = formField.IsVisible,
                IsRequired = formField.IsRequired,
                IsEditable = formField.IsEditable,
                DisplayOrder = formField.DisplayOrder,
                GroupName = formField.GroupName,
                ColumnSpan = formField.ColumnSpan,
                FieldIsActive = field.IsActive
            };
        }

        public async Task<FormFieldAdminDto?> UpdateFormFieldAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default)
        {
            var formField = await _db.FormFieldDefinitions
                .Include(x => x.Field)
                .FirstOrDefaultAsync(x => x.FormId == formId && x.FormFieldId == formFieldId, ct);

            if (formField is null || formField.Field is null)
            {
                return null;
            }

            formField.IsVisible = request.IsVisible;
            formField.IsRequired = request.IsRequired;
            formField.IsEditable = request.IsEditable;
            formField.DisplayOrder = request.DisplayOrder;
            formField.GroupName = NormalizeNullable(request.GroupName);
            formField.ColumnSpan = request.ColumnSpan;
            formField.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return new FormFieldAdminDto
            {
                FormFieldId = formField.FormFieldId,
                FormId = formField.FormId,
                FieldId = formField.FieldId,
                FieldKey = formField.Field.FieldKey,
                FieldLabel = formField.Field.FieldLabel,
                EntityName = formField.Field.EntityName,
                DataType = formField.Field.DataType,
                IsDynamic = formField.Field.IsDynamic,
                IsVisible = formField.IsVisible,
                IsRequired = formField.IsRequired,
                IsEditable = formField.IsEditable,
                DisplayOrder = formField.DisplayOrder,
                GroupName = formField.GroupName,
                ColumnSpan = formField.ColumnSpan,
                FieldIsActive = formField.Field.IsActive
            };
        }

        public async Task<bool> RemoveFormFieldAsync(int formId, int formFieldId, CancellationToken ct = default)
        {
            var formField = await _db.FormFieldDefinitions
                .FirstOrDefaultAsync(x => x.FormId == formId && x.FormFieldId == formFieldId, ct);

            if (formField is null)
            {
                return false;
            }

            _db.FormFieldDefinitions.Remove(formField);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteFieldOptionAsync(int fieldId, int optionId, CancellationToken ct = default)
        {
            var option = await _db.DynamicFieldOptions
                .FirstOrDefaultAsync(x => x.FieldId == fieldId && x.DynamicFieldOptionId == optionId, ct);

            if (option is null)
            {
                return false;
            }

            _db.DynamicFieldOptions.Remove(option);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ResolvedFormDto?> GetResolvedFormAsync(string formKey, CancellationToken ct = default)
        {
            var normalizedFormKey = (formKey ?? string.Empty).Trim();
            if (string.Equals(normalizedFormKey, "ArticleManualForm", StringComparison.OrdinalIgnoreCase))
            {
                await EnsureVenueMetadataIfNeededAsync("Article", ct);
            }

            var form = await _db.FormDefinitions
                .AsNoTracking()
                .Include(x => x.Fields)
                    .ThenInclude(x => x.Field)
                        .ThenInclude(x => x!.Options)
                .FirstOrDefaultAsync(x => x.FormKey == normalizedFormKey && x.IsActive, ct);

            return form is null ? null : MapResolvedForm(form);
        }

        public async Task<ResolvedFormDto?> GetActiveResolvedFormAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default)
        {
            var normalizedEntityName = (entityName ?? string.Empty).Trim();
            var normalizedPreferredKey = (preferredFormKey ?? string.Empty).Trim();

            await EnsureVenueMetadataIfNeededAsync(normalizedEntityName, ct);

            var query = _db.FormDefinitions
                .AsNoTracking()
                .Include(x => x.Fields)
                    .ThenInclude(x => x.Field)
                        .ThenInclude(x => x!.Options)
                .Where(x => x.EntityName == normalizedEntityName && x.IsActive);

            Data.Entities.FormDefinition? form = null;

            if (!string.IsNullOrWhiteSpace(normalizedPreferredKey))
            {
                form = await query
                    .FirstOrDefaultAsync(x => x.FormKey == normalizedPreferredKey, ct);
            }

            form ??= await query
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.FormId)
                .FirstOrDefaultAsync(ct);

            return form is null ? null : MapResolvedForm(form);
        }

        private static ResolvedFormDto MapResolvedForm(Data.Entities.FormDefinition form)
        {
            var sections = form.Fields
                .Where(x => x.IsVisible && x.Field != null && x.Field.IsActive && x.Field.FieldKey != "VenueId")
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
                                        DisplayOrder = option.DisplayOrder,
                                        IsActive = option.IsActive
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

        private static string NormalizeReferenceName(string? referenceTableName)
        {
            if (string.IsNullOrWhiteSpace(referenceTableName))
            {
                return string.Empty;
            }

            var normalized = referenceTableName.Trim();
            const string dboPrefix = "dbo.";
            if (normalized.StartsWith(dboPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[dboPrefix.Length..];
            }

            return normalized;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task EnsureVenueMetadataIfNeededAsync(string entityName, CancellationToken ct)
        {
            if (string.Equals(entityName, "Article", StringComparison.OrdinalIgnoreCase))
            {
                await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            }
        }

        private static FieldCatalogItemDto MapField(Data.Entities.FieldCatalogEntry field)
        {
            return new FieldCatalogItemDto
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
                IsRequired = field.IsRequired,
                IsVisible = field.IsVisible,
                IsEditable = field.IsEditable,
                IsFilterable = field.IsFilterable,
                IsActive = field.IsActive,
                DisplayOrder = field.DisplayOrder,
                MaxLength = field.MaxLength,
                Placeholder = field.Placeholder,
                HelpText = field.HelpText,
                DefaultValue = field.DefaultValue,
                ValidationRule = field.ValidationRule
            };
        }
    }
}
