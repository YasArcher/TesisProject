using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Articles;
using tesisproject.backend.Data.Articles.Entities;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers;

[ApiController]
[Route("api/config")]
[Route("api/scientific-production/config")]
[Authorize]
public sealed class ArticlesConfigurationController : ControllerBase
{
    private readonly ArticlesDbContext _ctx;

    public ArticlesConfigurationController(ArticlesDbContext ctx)
    {
        _ctx = ctx;
    }

    [HttpGet("forms")]
    public async Task<ActionResult<ServiceResult<List<FormSummaryDto>>>> GetForms(
        [FromQuery] string? entityName,
        CancellationToken ct)
    {
        var query = _ctx.FormDefinitions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(form => form.EntityName == entityName);

        var forms = await query
            .OrderByDescending(form => form.IsActive)
            .ThenBy(form => form.EntityName)
            .ThenBy(form => form.FormName)
            .Select(form => new FormSummaryDto
            {
                FormId = form.FormId,
                FormKey = form.FormKey,
                FormName = form.FormName,
                EntityName = form.EntityName,
                Description = form.Description,
                IsActive = form.IsActive
            })
            .ToListAsync(ct);

        return ServiceResult<List<FormSummaryDto>>.Ok(forms, "Formularios obtenidos.").ToActionResult();
    }

    [HttpGet("forms/{formId:int}/fields")]
    public async Task<ActionResult<ServiceResult<List<FormFieldAdminDto>>>> GetFormFields(int formId, CancellationToken ct)
    {
        var fields = await _ctx.FormFieldDefinitions
            .AsNoTracking()
            .Where(formField => formField.FormId == formId)
            .Include(formField => formField.Field)
            .OrderBy(formField => formField.DisplayOrder)
            .ThenBy(formField => formField.Field!.FieldLabel)
            .Select(formField => new FormFieldAdminDto
            {
                FormFieldId = formField.FormFieldId,
                FormId = formField.FormId,
                FieldId = formField.FieldId,
                FieldKey = formField.Field!.FieldKey,
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
            })
            .ToListAsync(ct);

        return ServiceResult<List<FormFieldAdminDto>>.Ok(fields, "Campos del formulario obtenidos.").ToActionResult();
    }

    [HttpGet("fields")]
    public async Task<ActionResult<ServiceResult<List<FieldCatalogItemDto>>>> GetFields(
        [FromQuery] string entityName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return ServiceResult<List<FieldCatalogItemDto>>.Fail("entityName es obligatorio.", ErrorType.Validation, "CONFIG_ENTITY_REQUIRED").ToActionResult();

        var fields = await GetFieldsQuery(entityName).ToListAsync(ct);
        return ServiceResult<List<FieldCatalogItemDto>>.Ok(fields, "Campos obtenidos.").ToActionResult();
    }
    [HttpGet("fields/dynamic")]
    public async Task<ActionResult<ServiceResult<List<FieldCatalogItemDto>>>> GetDynamicFields(
        [FromQuery] string entityName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return ServiceResult<List<FieldCatalogItemDto>>.Fail("entityName es obligatorio.", ErrorType.Validation, "CONFIG_ENTITY_REQUIRED").ToActionResult();

        var fields = await GetFieldsQuery(entityName)
            .Where(field => field.IsDynamic)
            .ToListAsync(ct);

        return ServiceResult<List<FieldCatalogItemDto>>.Ok(fields, "Campos dinamicos obtenidos.").ToActionResult();
    }
    [HttpGet("fields/{fieldId:int}/options")]
    public async Task<ActionResult<ServiceResult<List<DynamicFieldOptionDto>>>> GetFieldOptions(int fieldId, CancellationToken ct)
    {
        var options = await _ctx.DynamicFieldOptions
            .AsNoTracking()
            .Where(option => option.FieldId == fieldId)
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.OptionLabel)
            .Select(option => new DynamicFieldOptionDto
            {
                DynamicFieldOptionId = option.DynamicFieldOptionId,
                FieldId = option.FieldId,
                OptionValue = option.OptionValue,
                OptionLabel = option.OptionLabel,
                DisplayOrder = option.DisplayOrder,
                IsActive = option.IsActive
            })
            .ToListAsync(ct);

        return ServiceResult<List<DynamicFieldOptionDto>>.Ok(options, "Opciones obtenidas.").ToActionResult();
    }

    [HttpGet("fields/{fieldId:int}/catalog-items")]
    public async Task<ActionResult<ServiceResult<List<CatalogItemDto>>>> GetCatalogItems(int fieldId, [FromQuery] int? parentId, CancellationToken ct)
    {
        var field = await _ctx.FieldCatalogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.FieldId == fieldId, ct);

        if (field is null)
            return ServiceResult<List<CatalogItemDto>>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND").ToActionResult();

        var key = field.ReferenceTableName ?? field.FieldKey;
        var items = await ReadCatalogItemsAsync(key, ct);
        return ServiceResult<List<CatalogItemDto>>.Ok(items, "Catalogo obtenido.").ToActionResult();
    }
    [HttpGet("forms/{formKey}/resolved")]
    public async Task<ActionResult<ServiceResult<ResolvedFormDto>>> GetResolvedForm(string formKey, CancellationToken ct)
    {
        var form = await _ctx.FormDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.FormKey == formKey, ct);

        if (form is null)
            return ServiceResult<ResolvedFormDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND").ToActionResult();

        var resolved = await BuildResolvedFormAsync(form.FormId, ct);
        return ServiceResult<ResolvedFormDto>.Ok(resolved, "Formulario resuelto obtenido.").ToActionResult();
    }
    [HttpGet("forms/resolved-active")]
    public async Task<ActionResult<ServiceResult<ResolvedFormDto>>> GetResolvedActiveForm(
        [FromQuery] string entityName,
        [FromQuery] string? preferredFormKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return ServiceResult<ResolvedFormDto>.Fail("entityName es obligatorio.", ErrorType.Validation, "CONFIG_ENTITY_REQUIRED").ToActionResult();

        var query = _ctx.FormDefinitions.AsNoTracking()
            .Where(form => form.EntityName == entityName && form.IsActive);

        if (!string.IsNullOrWhiteSpace(preferredFormKey))
        {
            var preferredKey = preferredFormKey.Trim();
            var normalizedPreferredKey = NormalizeKey(preferredKey);
            query = query
                .OrderByDescending(form => form.FormKey == preferredKey || form.FormKey == normalizedPreferredKey)
                .ThenBy(form => form.FormName);
        }
        else
            query = query.OrderBy(form => form.FormName);

        var form = await query.FirstOrDefaultAsync(ct);
        if (form is null)
            return ServiceResult<ResolvedFormDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND").ToActionResult();

        var resolved = await BuildResolvedFormAsync(form.FormId, ct);
        return ServiceResult<ResolvedFormDto>.Ok(resolved, "Formulario resuelto obtenido.").ToActionResult();
    }
    [HttpPost("forms")]
    public async Task<ActionResult<ServiceResult<FormDefinitionAdminDto>>> CreateForm(
        [FromBody] CreateFormDefinitionRequest request,
        CancellationToken ct)
    {
        var validation = ValidateFormRequest(request.FormKey, request.FormName, request.EntityName);
        if (validation is not null)
            return ServiceResult<FormDefinitionAdminDto>.Fail(validation, ErrorType.Validation, "CONFIG_FORM_INVALID").ToActionResult();

        var key = request.FormKey.Trim();
        var normalizedKey = NormalizeKey(key);
        var exists = await _ctx.FormDefinitions.AnyAsync(form => form.FormKey == key || form.FormKey == normalizedKey, ct);
        if (exists)
            return ServiceResult<FormDefinitionAdminDto>.Fail("Ya existe un formulario con esa clave.", ErrorType.Conflict, "CONFIG_FORM_KEY_DUPLICATED").ToActionResult();

        if (request.IsActive)
        {
            await _ctx.FormDefinitions
                .Where(form => form.EntityName == request.EntityName.Trim() && form.IsActive)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(form => form.IsActive, false)
                    .SetProperty(form => form.UpdatedAt, DateTime.UtcNow), ct);
        }

        var formDefinition = new FormDefinition
        {
            FormKey = key,
            FormName = request.FormName.Trim(),
            EntityName = request.EntityName.Trim(),
            Description = NormalizeOptional(request.Description),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _ctx.FormDefinitions.Add(formDefinition);
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<FormDefinitionAdminDto>.Ok(MapForm(formDefinition), "Formulario creado.").ToActionResult();
    }

    [HttpPut("forms/{formId:int}")]
    public async Task<ActionResult<ServiceResult<FormDefinitionAdminDto>>> UpdateForm(
        int formId,
        [FromBody] UpdateFormDefinitionRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FormName))
            return ServiceResult<FormDefinitionAdminDto>.Fail("El nombre del formulario es obligatorio.", ErrorType.Validation, "CONFIG_FORM_NAME_REQUIRED").ToActionResult();

        var formDefinition = await _ctx.FormDefinitions.FirstOrDefaultAsync(form => form.FormId == formId, ct);
        if (formDefinition is null)
            return ServiceResult<FormDefinitionAdminDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND").ToActionResult();

        if (request.IsActive && !formDefinition.IsActive)
        {
            await _ctx.FormDefinitions
                .Where(form => form.EntityName == formDefinition.EntityName && form.FormId != formId && form.IsActive)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(form => form.IsActive, false)
                    .SetProperty(form => form.UpdatedAt, DateTime.UtcNow), ct);
        }

        formDefinition.FormName = request.FormName.Trim();
        formDefinition.Description = NormalizeOptional(request.Description);
        formDefinition.IsActive = request.IsActive;
        formDefinition.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<FormDefinitionAdminDto>.Ok(MapForm(formDefinition), "Formulario actualizado.").ToActionResult();
    }

    [HttpDelete("forms/{formId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteForm(int formId, CancellationToken ct)
    {
        var formDefinition = await _ctx.FormDefinitions.FirstOrDefaultAsync(form => form.FormId == formId, ct);
        if (formDefinition is null)
            return ServiceResult<NoContent>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND").ToActionResult();

        _ctx.FormDefinitions.Remove(formDefinition);
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Formulario eliminado.").ToActionResult();
    }

    [HttpPost("fields/dynamic")]
    public async Task<ActionResult<ServiceResult<FieldCatalogItemDto>>> CreateDynamicField(
        [FromBody] CreateDynamicFieldRequest request,
        CancellationToken ct)
    {
        var validation = ValidateFieldRequest(request.EntityName, request.FieldKey, request.FieldLabel, request.DataType);
        if (validation is not null)
            return ServiceResult<FieldCatalogItemDto>.Fail(validation, ErrorType.Validation, "CONFIG_FIELD_INVALID").ToActionResult();

        var entityName = request.EntityName.Trim();
        var fieldKey = NormalizeKey(request.FieldKey);
        var exists = await _ctx.FieldCatalogEntries.AnyAsync(field => field.EntityName == entityName && field.FieldKey == fieldKey, ct);
        if (exists)
            return ServiceResult<FieldCatalogItemDto>.Fail("Ya existe un campo con esa clave para la entidad seleccionada.", ErrorType.Conflict, "CONFIG_FIELD_KEY_DUPLICATED").ToActionResult();

        var fieldEntry = new FieldCatalogEntry
        {
            EntityName = entityName,
            FieldKey = fieldKey,
            FieldLabel = request.FieldLabel.Trim(),
            DataType = request.DataType.Trim(),
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
            Placeholder = NormalizeOptional(request.Placeholder),
            HelpText = NormalizeOptional(request.HelpText),
            DefaultValue = NormalizeOptional(request.DefaultValue),
            ValidationRule = NormalizeOptional(request.ValidationRule),
            CreatedAt = DateTime.UtcNow
        };

        _ctx.FieldCatalogEntries.Add(fieldEntry);
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<FieldCatalogItemDto>.Ok(MapField(fieldEntry), "Campo dinamico creado.").ToActionResult();
    }

    [HttpPut("fields/{fieldId:int}")]
    public async Task<ActionResult<ServiceResult<FieldCatalogItemDto>>> UpdateField(
        int fieldId,
        [FromBody] UpdateFieldCatalogRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FieldLabel))
            return ServiceResult<FieldCatalogItemDto>.Fail("La etiqueta del campo es obligatoria.", ErrorType.Validation, "CONFIG_FIELD_LABEL_REQUIRED").ToActionResult();

        var fieldEntry = await _ctx.FieldCatalogEntries.FirstOrDefaultAsync(field => field.FieldId == fieldId, ct);
        if (fieldEntry is null)
            return ServiceResult<FieldCatalogItemDto>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND").ToActionResult();

        fieldEntry.FieldLabel = request.FieldLabel.Trim();
        fieldEntry.IsRequired = request.IsRequired;
        fieldEntry.IsVisible = request.IsVisible;
        fieldEntry.IsEditable = request.IsEditable;
        fieldEntry.IsFilterable = request.IsFilterable;
        fieldEntry.IsActive = request.IsActive;
        fieldEntry.DisplayOrder = request.DisplayOrder;
        fieldEntry.MaxLength = request.MaxLength;
        fieldEntry.Placeholder = NormalizeOptional(request.Placeholder);
        fieldEntry.HelpText = NormalizeOptional(request.HelpText);
        fieldEntry.DefaultValue = NormalizeOptional(request.DefaultValue);
        fieldEntry.ValidationRule = NormalizeOptional(request.ValidationRule);
        fieldEntry.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<FieldCatalogItemDto>.Ok(MapField(fieldEntry), "Campo actualizado.").ToActionResult();
    }

    [HttpDelete("fields/{fieldId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteField(int fieldId, CancellationToken ct)
    {
        var fieldEntry = await _ctx.FieldCatalogEntries.FirstOrDefaultAsync(field => field.FieldId == fieldId, ct);
        if (fieldEntry is null)
            return ServiceResult<NoContent>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND").ToActionResult();

        if (fieldEntry.IsSystemField)
            return ServiceResult<NoContent>.Fail("No se puede eliminar un campo del sistema.", ErrorType.Conflict, "CONFIG_SYSTEM_FIELD_DELETE_BLOCKED").ToActionResult();

        var hasValues = await _ctx.DynamicFieldValues.AnyAsync(value => value.FieldId == fieldId, ct)
            || await _ctx.ArticleParticipantDynamicFieldValues.AnyAsync(value => value.FieldId == fieldId, ct);
        if (hasValues)
            return ServiceResult<NoContent>.Fail("No se puede eliminar un campo que ya tiene valores registrados.", ErrorType.Conflict, "CONFIG_FIELD_HAS_VALUES").ToActionResult();

        var isAssignedToForms = await _ctx.FormFieldDefinitions.AnyAsync(formField => formField.FieldId == fieldId, ct);
        if (isAssignedToForms)
            return ServiceResult<NoContent>.Fail("Remueve el campo de los formularios antes de eliminarlo.", ErrorType.Conflict, "CONFIG_FIELD_ASSIGNED_TO_FORM").ToActionResult();

        _ctx.FieldCatalogEntries.Remove(fieldEntry);
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Campo eliminado.").ToActionResult();
    }

    [HttpPost("forms/{formId:int}/fields")]
    public async Task<ActionResult<ServiceResult<FormFieldAdminDto>>> AddFieldToForm(
        int formId,
        [FromBody] AddFieldToFormRequest request,
        CancellationToken ct)
    {
        var formDefinition = await _ctx.FormDefinitions.FirstOrDefaultAsync(form => form.FormId == formId, ct);
        if (formDefinition is null)
            return ServiceResult<FormFieldAdminDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND").ToActionResult();

        var fieldEntry = await _ctx.FieldCatalogEntries.FirstOrDefaultAsync(field => field.FieldId == request.FieldId, ct);
        if (fieldEntry is null)
            return ServiceResult<FormFieldAdminDto>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND").ToActionResult();

        if (!string.Equals(fieldEntry.EntityName, formDefinition.EntityName, StringComparison.OrdinalIgnoreCase))
            return ServiceResult<FormFieldAdminDto>.Fail("El campo no pertenece a la entidad del formulario.", ErrorType.Validation, "CONFIG_FORM_FIELD_ENTITY_MISMATCH").ToActionResult();

        var duplicated = await _ctx.FormFieldDefinitions.AnyAsync(formField => formField.FormId == formId && formField.FieldId == request.FieldId, ct);
        if (duplicated)
            return ServiceResult<FormFieldAdminDto>.Fail("El campo ya pertenece a este formulario.", ErrorType.Conflict, "CONFIG_FORM_FIELD_DUPLICATED").ToActionResult();

        var formFieldDefinition = new FormFieldDefinition
        {
            FormId = formId,
            FieldId = request.FieldId,
            IsVisible = request.IsVisible && fieldEntry.IsVisible && fieldEntry.IsActive,
            IsRequired = request.IsRequired && fieldEntry.IsActive,
            IsEditable = request.IsEditable && fieldEntry.IsEditable && fieldEntry.IsActive,
            DisplayOrder = request.DisplayOrder,
            GroupName = NormalizeOptional(request.GroupName),
            ColumnSpan = NormalizeColumnSpan(request.ColumnSpan),
            CreatedAt = DateTime.UtcNow
        };

        _ctx.FormFieldDefinitions.Add(formFieldDefinition);
        await _ctx.SaveChangesAsync(ct);
        formFieldDefinition.Field = fieldEntry;

        return ServiceResult<FormFieldAdminDto>.Ok(MapFormField(formFieldDefinition), "Campo agregado al formulario.").ToActionResult();
    }

    [HttpPut("forms/{formId:int}/fields/{formFieldId:int}")]
    public async Task<ActionResult<ServiceResult<FormFieldAdminDto>>> UpdateFormField(
        int formId,
        int formFieldId,
        [FromBody] UpdateFormFieldRequest request,
        CancellationToken ct)
    {
        var formFieldDefinition = await _ctx.FormFieldDefinitions
            .Include(formField => formField.Field)
            .FirstOrDefaultAsync(formField => formField.FormId == formId && formField.FormFieldId == formFieldId, ct);

        if (formFieldDefinition is null)
            return ServiceResult<FormFieldAdminDto>.Fail("Campo de formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_FIELD_NOT_FOUND").ToActionResult();

        var fieldEntry = formFieldDefinition.Field!;
        formFieldDefinition.IsVisible = request.IsVisible && fieldEntry.IsVisible && fieldEntry.IsActive;
        formFieldDefinition.IsRequired = request.IsRequired && fieldEntry.IsActive;
        formFieldDefinition.IsEditable = request.IsEditable && fieldEntry.IsEditable && fieldEntry.IsActive;
        formFieldDefinition.DisplayOrder = request.DisplayOrder;
        formFieldDefinition.GroupName = NormalizeOptional(request.GroupName);
        formFieldDefinition.ColumnSpan = NormalizeColumnSpan(request.ColumnSpan);
        formFieldDefinition.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<FormFieldAdminDto>.Ok(MapFormField(formFieldDefinition), "Campo de formulario actualizado.").ToActionResult();
    }

    [HttpDelete("forms/{formId:int}/fields/{formFieldId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteFormField(int formId, int formFieldId, CancellationToken ct)
    {
        var formFieldDefinition = await _ctx.FormFieldDefinitions.FirstOrDefaultAsync(formField => formField.FormId == formId && formField.FormFieldId == formFieldId, ct);
        if (formFieldDefinition is null)
            return ServiceResult<NoContent>.Fail("Campo de formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_FIELD_NOT_FOUND").ToActionResult();

        _ctx.FormFieldDefinitions.Remove(formFieldDefinition);
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Campo removido del formulario.").ToActionResult();
    }

    [HttpPost("fields/{fieldId:int}/options")]
    public async Task<ActionResult<ServiceResult<DynamicFieldOptionDto>>> CreateFieldOption(
        int fieldId,
        [FromBody] CreateDynamicFieldOptionRequest request,
        CancellationToken ct)
    {
        var fieldExists = await _ctx.FieldCatalogEntries.AnyAsync(field => field.FieldId == fieldId, ct);
        if (!fieldExists)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND").ToActionResult();

        var validation = ValidateOptionRequest(request.OptionValue, request.OptionLabel);
        if (validation is not null)
            return ServiceResult<DynamicFieldOptionDto>.Fail(validation, ErrorType.Validation, "CONFIG_OPTION_INVALID").ToActionResult();

        var optionValue = NormalizeKey(request.OptionValue);
        var duplicated = await _ctx.DynamicFieldOptions.AnyAsync(option => option.FieldId == fieldId && option.OptionValue == optionValue, ct);
        if (duplicated)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Ya existe una opción con ese valor para el campo.", ErrorType.Conflict, "CONFIG_OPTION_DUPLICATED").ToActionResult();

        var optionEntry = new DynamicFieldOption
        {
            FieldId = fieldId,
            OptionValue = optionValue,
            OptionLabel = request.OptionLabel.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _ctx.DynamicFieldOptions.Add(optionEntry);
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<DynamicFieldOptionDto>.Ok(MapOption(optionEntry), "Opción creada.").ToActionResult();
    }

    [HttpPut("fields/{fieldId:int}/options/{optionId:int}")]
    public async Task<ActionResult<ServiceResult<DynamicFieldOptionDto>>> UpdateFieldOption(
        int fieldId,
        int optionId,
        [FromBody] UpdateDynamicFieldOptionRequest request,
        CancellationToken ct)
    {
        var validation = ValidateOptionRequest(request.OptionValue, request.OptionLabel);
        if (validation is not null)
            return ServiceResult<DynamicFieldOptionDto>.Fail(validation, ErrorType.Validation, "CONFIG_OPTION_INVALID").ToActionResult();

        var optionEntry = await _ctx.DynamicFieldOptions.FirstOrDefaultAsync(option => option.FieldId == fieldId && option.DynamicFieldOptionId == optionId, ct);
        if (optionEntry is null)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Opción no encontrada.", ErrorType.NotFound, "CONFIG_OPTION_NOT_FOUND").ToActionResult();

        var optionValue = NormalizeKey(request.OptionValue);
        var duplicated = await _ctx.DynamicFieldOptions.AnyAsync(option => option.FieldId == fieldId && option.DynamicFieldOptionId != optionId && option.OptionValue == optionValue, ct);
        if (duplicated)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Ya existe una opción con ese valor para el campo.", ErrorType.Conflict, "CONFIG_OPTION_DUPLICATED").ToActionResult();

        optionEntry.OptionValue = optionValue;
        optionEntry.OptionLabel = request.OptionLabel.Trim();
        optionEntry.DisplayOrder = request.DisplayOrder;
        optionEntry.IsActive = request.IsActive;
        optionEntry.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<DynamicFieldOptionDto>.Ok(MapOption(optionEntry), "Opción actualizada.").ToActionResult();
    }

    [HttpDelete("fields/{fieldId:int}/options/{optionId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteFieldOption(int fieldId, int optionId, CancellationToken ct)
    {
        var optionEntry = await _ctx.DynamicFieldOptions.FirstOrDefaultAsync(option => option.FieldId == fieldId && option.DynamicFieldOptionId == optionId, ct);
        if (optionEntry is null)
            return ServiceResult<NoContent>.Fail("Opción no encontrada.", ErrorType.NotFound, "CONFIG_OPTION_NOT_FOUND").ToActionResult();

        _ctx.DynamicFieldOptions.Remove(optionEntry);
        await _ctx.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Opción eliminada.").ToActionResult();
    }
    private IQueryable<FieldCatalogItemDto> GetFieldsQuery(string entityName)
    {
        return _ctx.FieldCatalogEntries
            .AsNoTracking()
            .Where(field => field.EntityName == entityName)
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.FieldLabel)
            .Select(field => new FieldCatalogItemDto
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
            });
    }


    private static FormDefinitionAdminDto MapForm(FormDefinition form)
        => new()
        {
            FormId = form.FormId,
            FormKey = form.FormKey,
            FormName = form.FormName,
            EntityName = form.EntityName,
            Description = form.Description,
            IsActive = form.IsActive
        };

    private static FieldCatalogItemDto MapField(FieldCatalogEntry field)
        => new()
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

    private static FormFieldAdminDto MapFormField(FormFieldDefinition formField)
    {
        var field = formField.Field!;
        return new FormFieldAdminDto
        {
            FormFieldId = formField.FormFieldId,
            FormId = formField.FormId,
            FieldId = formField.FieldId,
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

    private static DynamicFieldOptionDto MapOption(DynamicFieldOption option)
        => new()
        {
            DynamicFieldOptionId = option.DynamicFieldOptionId,
            FieldId = option.FieldId,
            OptionValue = option.OptionValue,
            OptionLabel = option.OptionLabel,
            DisplayOrder = option.DisplayOrder,
            IsActive = option.IsActive
        };

    private static string NormalizeKey(string value)
        => value.Trim().Replace(" ", "_").ToLowerInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? NormalizeColumnSpan(int? value)
        => value is null ? null : Math.Clamp(value.Value, 1, 2);

    private static string? ValidateFormRequest(string formKey, string formName, string entityName)
    {
        if (string.IsNullOrWhiteSpace(formKey)) return "La clave del formulario es obligatoria.";
        if (string.IsNullOrWhiteSpace(formName)) return "El nombre del formulario es obligatorio.";
        if (string.IsNullOrWhiteSpace(entityName)) return "La entidad del formulario es obligatoria.";
        return null;
    }

    private static string? ValidateFieldRequest(string entityName, string fieldKey, string fieldLabel, string dataType)
    {
        if (string.IsNullOrWhiteSpace(entityName)) return "La entidad del campo es obligatoria.";
        if (string.IsNullOrWhiteSpace(fieldKey)) return "La clave del campo es obligatoria.";
        if (string.IsNullOrWhiteSpace(fieldLabel)) return "La etiqueta del campo es obligatoria.";
        if (string.IsNullOrWhiteSpace(dataType)) return "El tipo de dato del campo es obligatorio.";
        return null;
    }

    private static string? ValidateOptionRequest(string optionValue, string optionLabel)
    {
        if (string.IsNullOrWhiteSpace(optionValue)) return "El valor de la opción es obligatorio.";
        if (string.IsNullOrWhiteSpace(optionLabel)) return "La etiqueta de la opción es obligatoria.";
        return null;
    }
    private async Task<ResolvedFormDto> BuildResolvedFormAsync(int formId, CancellationToken ct)
    {
        var form = await _ctx.FormDefinitions
            .AsNoTracking()
            .FirstAsync(item => item.FormId == formId, ct);

        var fields = await _ctx.FormFieldDefinitions
            .AsNoTracking()
            .Where(formField => formField.FormId == formId && formField.Field != null)
            .Include(formField => formField.Field)
            .ThenInclude(field => field!.Options)
            .OrderBy(formField => formField.DisplayOrder)
            .ThenBy(formField => formField.Field!.FieldLabel)
            .ToListAsync(ct);

        var sections = fields
            .GroupBy(formField => string.IsNullOrWhiteSpace(formField.GroupName) ? "General" : formField.GroupName!)
            .Select(group => new ResolvedFormSectionDto
            {
                GroupName = group.Key,
                DisplayOrder = group.Min(item => item.DisplayOrder),
                Fields = group.Select(MapResolvedField).ToList()
            })
            .OrderBy(section => section.DisplayOrder)
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

    private static ResolvedFormFieldDto MapResolvedField(tesisproject.backend.Data.Articles.Entities.FormFieldDefinition formField)
    {
        var field = formField.Field!;
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
            IsRequired = formField.IsRequired,
            IsVisible = formField.IsVisible,
            IsEditable = formField.IsEditable,
            IsFilterable = field.IsFilterable,
            IsActive = field.IsActive,
            DisplayOrder = formField.DisplayOrder,
            ColumnSpan = formField.ColumnSpan,
            GroupName = formField.GroupName,
            MaxLength = field.MaxLength,
            Placeholder = field.Placeholder,
            HelpText = field.HelpText,
            DefaultValue = field.DefaultValue,
            ValidationRule = field.ValidationRule,
            Options = field.Options
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.OptionLabel)
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
    }

    private async Task<List<CatalogItemDto>> ReadCatalogItemsAsync(string key, CancellationToken ct)
    {
        return NormalizeCatalogKey(key) switch
        {
            "faculties" or "faculty" or "facultyid" => await _ctx.Faculties.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.FacultyId, Name = x.Name }).ToListAsync(ct),
            "research-lines" or "researchline" or "researchlineid" => await _ctx.ResearchLines.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.ResearchLineId, Name = x.Name }).ToListAsync(ct),
            "indexing-sources" or "indexingsource" or "indexingsourceid" => await _ctx.IndexingSources.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.IndexingSourceId, Name = x.Name }).ToListAsync(ct),
            "publication-statuses" or "publicationstatus" or "publicationstatusid" => await _ctx.PublicationStatuses.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.PublicationStatusId, Name = x.Name }).ToListAsync(ct),
            _ => []
        };
    }

    private static string NormalizeCatalogKey(string? key)
        => (key ?? string.Empty).Trim().Replace("_", "-").ToLowerInvariant();
}

[ApiController]
[Route("api/catalogs")]
[Route("api/scientific-production/catalogs")]
[Authorize]
public sealed class ArticlesCatalogsController : ControllerBase
{
    private readonly ArticlesDbContext _ctx;

    public ArticlesCatalogsController(ArticlesDbContext ctx)
    {
        _ctx = ctx;
    }

    [HttpGet("admin/{catalogKey}")]
    public async Task<ActionResult<ServiceResult<List<CatalogAdminItemDto>>>> GetAdminCatalog(string catalogKey, CancellationToken ct)
    {
        var items = NormalizeCatalogKey(catalogKey) switch
        {
            "faculties" => await _ctx.Faculties.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.FacultyId, Name = x.Name, Code = x.Code }).ToListAsync(ct),
            "research-lines" => await _ctx.ResearchLines.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.ResearchLineId, Name = x.Name }).ToListAsync(ct),
            "indexing-sources" => await _ctx.IndexingSources.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.IndexingSourceId, Name = x.Name }).ToListAsync(ct),
            "publication-statuses" => await _ctx.PublicationStatuses.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.PublicationStatusId, Name = x.Name }).ToListAsync(ct),
            _ => []
        };

        return ServiceResult<List<CatalogAdminItemDto>>.Ok(items, "Catalogo administrativo obtenido.").ToActionResult();
    }

    private static string NormalizeCatalogKey(string? key)
        => (key ?? string.Empty).Trim().Replace("_", "-").ToLowerInvariant();
}
