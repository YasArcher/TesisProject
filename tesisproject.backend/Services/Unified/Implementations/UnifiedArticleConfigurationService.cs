using tesisproject.backend.Repositories.Unified;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;
using tesisproject.shared.Responses;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedArticleConfigurationService(IUnifiedArticleConfigurationRepository repository, IUnifiedUnitOfWork uow, IUnifiedArticleFormSelector selector) : IUnifiedArticleConfigurationService
{
    public Task<ServiceResult<FormDefinitionAdminDto>> CreateForm(CreateFormDefinitionRequest request,
        CancellationToken ct)
        => MutateAsync(() => CreateFormCore(request, ct), ct);

    public Task<ServiceResult<FormDefinitionAdminDto>> UpdateForm(int formId,
        UpdateFormDefinitionRequest request,
        CancellationToken ct)
        => MutateAsync(() => UpdateFormCore(formId, request, ct), ct);

    public Task<ServiceResult<NoContent>> DeleteForm(int formId, CancellationToken ct)
        => MutateAsync(() => DeleteFormCore(formId, ct), ct);

    public Task<ServiceResult<FieldCatalogItemDto>> CreateDynamicField(CreateDynamicFieldRequest request,
        CancellationToken ct)
        => MutateAsync(() => CreateDynamicFieldCore(request, ct), ct);

    public Task<ServiceResult<FieldCatalogItemDto>> UpdateField(int fieldId,
        UpdateFieldCatalogRequest request,
        CancellationToken ct)
        => MutateAsync(() => UpdateFieldCore(fieldId, request, ct), ct);

    public Task<ServiceResult<NoContent>> DeleteField(int fieldId, CancellationToken ct)
        => MutateAsync(() => DeleteFieldCore(fieldId, ct), ct);

    public Task<ServiceResult<FormFieldAdminDto>> AddFieldToForm(int formId,
        AddFieldToFormRequest request,
        CancellationToken ct)
        => MutateAsync(() => AddFieldToFormCore(formId, request, ct), ct);

    public Task<ServiceResult<FormFieldAdminDto>> UpdateFormField(int formId,
        int formFieldId,
        UpdateFormFieldRequest request,
        CancellationToken ct)
        => MutateAsync(() => UpdateFormFieldCore(formId, formFieldId, request, ct), ct);

    public Task<ServiceResult<NoContent>> DeleteFormField(int formId, int formFieldId, CancellationToken ct)
        => MutateAsync(() => DeleteFormFieldCore(formId, formFieldId, ct), ct);

    public Task<ServiceResult<DynamicFieldOptionDto>> CreateFieldOption(int fieldId,
        CreateDynamicFieldOptionRequest request,
        CancellationToken ct)
        => MutateAsync(() => CreateFieldOptionCore(fieldId, request, ct), ct);

    public Task<ServiceResult<DynamicFieldOptionDto>> UpdateFieldOption(int fieldId,
        int optionId,
        UpdateDynamicFieldOptionRequest request,
        CancellationToken ct)
        => MutateAsync(() => UpdateFieldOptionCore(fieldId, optionId, request, ct), ct);

    public Task<ServiceResult<NoContent>> DeleteFieldOption(int fieldId, int optionId, CancellationToken ct)
        => MutateAsync(() => DeleteFieldOptionCore(fieldId, optionId, ct), ct);

    public async Task<ServiceResult<List<FormSummaryDto>>> GetForms(
        string? entityName,
        CancellationToken ct)
    {
        var forms = await repository.ListFormsAsync(entityName, ct);

        return ServiceResult<List<FormSummaryDto>>.Ok(forms, "Formularios obtenidos.");
    }

    public async Task<ServiceResult<List<FormFieldAdminDto>>> GetFormFields(int formId, CancellationToken ct)
    {
        var fields = await repository.ListFormFieldsAsync(formId, ct);

        return ServiceResult<List<FormFieldAdminDto>>.Ok(fields, "Campos del formulario obtenidos.");
    }

    public async Task<ServiceResult<List<FieldCatalogItemDto>>> GetFields(
        string entityName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return ServiceResult<List<FieldCatalogItemDto>>.Fail("entityName es obligatorio.", ErrorType.Validation, "CONFIG_ENTITY_REQUIRED");

        var fields = await repository.ListFieldsAsync(entityName, false, ct);
        return ServiceResult<List<FieldCatalogItemDto>>.Ok(fields, "Campos obtenidos.");
    }
    public async Task<ServiceResult<List<FieldCatalogItemDto>>> GetDynamicFields(
        string entityName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return ServiceResult<List<FieldCatalogItemDto>>.Fail("entityName es obligatorio.", ErrorType.Validation, "CONFIG_ENTITY_REQUIRED");

        var fields = await repository.ListFieldsAsync(entityName, true, ct);

        return ServiceResult<List<FieldCatalogItemDto>>.Ok(fields, "Campos dinamicos obtenidos.");
    }
    public async Task<ServiceResult<List<DynamicFieldOptionDto>>> GetFieldOptions(int fieldId, CancellationToken ct)
    {
        var options = await repository.ListOptionsAsync(fieldId, ct);

        return ServiceResult<List<DynamicFieldOptionDto>>.Ok(options, "Opciones obtenidas.");
    }

    public async Task<ServiceResult<List<CatalogItemDto>>> GetCatalogItems(int fieldId, int? parentId, CancellationToken ct)
    {
        var field = await repository.FindFieldAsync(item => item.FieldId == fieldId, ct, asNoTracking: true);

        if (field is null)
            return ServiceResult<List<CatalogItemDto>>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND");

        var key = field.ReferenceTableName ?? field.FieldKey;
        var items = await ReadCatalogItemsAsync(key, ct);
        return ServiceResult<List<CatalogItemDto>>.Ok(items, "Catalogo obtenido.");
    }
    public async Task<ServiceResult<ResolvedFormDto>> GetResolvedForm(string formKey, CancellationToken ct)
    {
        var form = await repository.FindFormAsync(item => item.FormKey == formKey, ct, asNoTracking: true);

        if (form is null)
            return ServiceResult<ResolvedFormDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND");

        return await BuildResolvedFormAsync(form.FormId, ct);
    }
    public async Task<ServiceResult<ResolvedFormDto>> GetResolvedActiveForm(
        string entityName,
        string? preferredFormKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return ServiceResult<ResolvedFormDto>.Fail("entityName es obligatorio.", ErrorType.Validation, "CONFIG_ENTITY_REQUIRED");

        var form = await selector.SelectAsync(entityName, preferredFormKey, ct);
        if (form is null)
            return ServiceResult<ResolvedFormDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND");

        return await BuildResolvedFormAsync(form.FormId, ct);
    }
    private async Task<ServiceResult<FormDefinitionAdminDto>> CreateFormCore(
        CreateFormDefinitionRequest request,
        CancellationToken ct)
    {
        var validation = ValidateFormRequest(request.FormKey, request.FormName, request.EntityName);
        if (validation is not null)
            return ServiceResult<FormDefinitionAdminDto>.Fail(validation, ErrorType.Validation, "CONFIG_FORM_INVALID");

        var key = request.FormKey.Trim();
        var normalizedKey = NormalizeKey(key);
        var exists = await repository.ExistsFormAsync(form => form.FormKey == key || form.FormKey == normalizedKey, ct);
        if (exists)
            return ServiceResult<FormDefinitionAdminDto>.Fail("Ya existe un formulario con esa clave.", ErrorType.Conflict, "CONFIG_FORM_KEY_DUPLICATED");

        if (request.IsActive)
        {
            await repository.DeactivateFormsAsync(request.EntityName.Trim(), null, ct);
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

        repository.AddForm(formDefinition);
        await uow.SaveChangesAsync(ct);

        return ServiceResult<FormDefinitionAdminDto>.Ok(MapForm(formDefinition), "Formulario creado.");
    }

    private async Task<ServiceResult<FormDefinitionAdminDto>> UpdateFormCore(
        int formId,
        UpdateFormDefinitionRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FormName))
            return ServiceResult<FormDefinitionAdminDto>.Fail("El nombre del formulario es obligatorio.", ErrorType.Validation, "CONFIG_FORM_NAME_REQUIRED");

        var formDefinition = await repository.FindFormAsync(form => form.FormId == formId, ct, asNoTracking: false);
        if (formDefinition is null)
            return ServiceResult<FormDefinitionAdminDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND");

        if (request.IsActive && !formDefinition.IsActive)
        {
            await repository.DeactivateFormsAsync(formDefinition.EntityName, formId, ct);
        }

        formDefinition.FormName = request.FormName.Trim();
        formDefinition.Description = NormalizeOptional(request.Description);
        formDefinition.IsActive = request.IsActive;
        formDefinition.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);

        return ServiceResult<FormDefinitionAdminDto>.Ok(MapForm(formDefinition), "Formulario actualizado.");
    }

    private async Task<ServiceResult<NoContent>> DeleteFormCore(int formId, CancellationToken ct)
    {
        var formDefinition = await repository.FindFormAsync(form => form.FormId == formId, ct, asNoTracking: false);
        if (formDefinition is null)
            return ServiceResult<NoContent>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND");

        await repository.RemoveFormAsync(formDefinition, ct);
        await uow.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Formulario eliminado.");
    }

    private async Task<ServiceResult<FieldCatalogItemDto>> CreateDynamicFieldCore(
        CreateDynamicFieldRequest request,
        CancellationToken ct)
    {
        var validation = ValidateFieldRequest(request.EntityName, request.FieldKey, request.FieldLabel, request.DataType);
        if (validation is not null)
            return ServiceResult<FieldCatalogItemDto>.Fail(validation, ErrorType.Validation, "CONFIG_FIELD_INVALID");

        var entityName = request.EntityName.Trim();
        var fieldKey = NormalizeKey(request.FieldKey);
        if (UnifiedArticleConfigurationCanonicalFields.Attribute(entityName, fieldKey, request.FieldLabel).HasValue)
            return ServiceResult<FieldCatalogItemDto>.Fail("El atributo base pertenece a Product; no puede crearse como campo dinámico de Article.", ErrorType.Conflict, "CONFIG_BASE_ATTRIBUTE_DUPLICATED");
        var exists = await repository.ExistsFieldAsync(field => field.EntityName == entityName && field.FieldKey == fieldKey, ct);
        if (exists)
            return ServiceResult<FieldCatalogItemDto>.Fail("Ya existe un campo con esa clave para la entidad seleccionada.", ErrorType.Conflict, "CONFIG_FIELD_KEY_DUPLICATED");

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

        repository.AddField(fieldEntry);
        await uow.SaveChangesAsync(ct);

        return ServiceResult<FieldCatalogItemDto>.Ok(MapField(fieldEntry), "Campo dinamico creado.");
    }

    private async Task<ServiceResult<FieldCatalogItemDto>> UpdateFieldCore(
        int fieldId,
        UpdateFieldCatalogRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FieldLabel))
            return ServiceResult<FieldCatalogItemDto>.Fail("La etiqueta del campo es obligatoria.", ErrorType.Validation, "CONFIG_FIELD_LABEL_REQUIRED");

        var fieldEntry = await repository.FindFieldAsync(field => field.FieldId == fieldId, ct, asNoTracking: false);
        if (fieldEntry is null)
            return ServiceResult<FieldCatalogItemDto>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND");

        var canonicalError = await ValidateCanonicalFieldAsync(fieldEntry, request.FieldLabel, ct);
        if (canonicalError is not null)
            return ServiceResult<FieldCatalogItemDto>.Fail(canonicalError, ErrorType.Conflict, "CONFIG_CANONICAL_FIELD_INVALID");

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
        await uow.SaveChangesAsync(ct);

        return ServiceResult<FieldCatalogItemDto>.Ok(MapField(fieldEntry), "Campo actualizado.");
    }

    private async Task<ServiceResult<NoContent>> DeleteFieldCore(int fieldId, CancellationToken ct)
    {
        var fieldEntry = await repository.FindFieldAsync(field => field.FieldId == fieldId, ct, asNoTracking: false);
        if (fieldEntry is null)
            return ServiceResult<NoContent>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND");

        if (fieldEntry.IsSystemField)
            return ServiceResult<NoContent>.Fail("No se puede eliminar un campo del sistema.", ErrorType.Conflict, "CONFIG_SYSTEM_FIELD_DELETE_BLOCKED");

        var hasValues = await repository.ExistsArticleValueAsync(value => value.FieldId == fieldId, ct)
            || await repository.ExistsParticipantValueAsync(value => value.FieldId == fieldId, ct);
        if (hasValues)
            return ServiceResult<NoContent>.Fail("No se puede eliminar un campo que ya tiene valores registrados.", ErrorType.Conflict, "CONFIG_FIELD_HAS_VALUES");

        var isAssignedToForms = await repository.ExistsAssignmentAsync(formField => formField.FieldId == fieldId, ct);
        if (isAssignedToForms)
            return ServiceResult<NoContent>.Fail("Remueve el campo de los formularios antes de eliminarlo.", ErrorType.Conflict, "CONFIG_FIELD_ASSIGNED_TO_FORM");

        await repository.RemoveFieldAsync(fieldEntry, ct);
        await uow.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Campo eliminado.");
    }

    private async Task<ServiceResult<FormFieldAdminDto>> AddFieldToFormCore(
        int formId,
        AddFieldToFormRequest request,
        CancellationToken ct)
    {
        var formDefinition = await repository.FindFormAsync(form => form.FormId == formId, ct, asNoTracking: false);
        if (formDefinition is null)
            return ServiceResult<FormFieldAdminDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND");

        var fieldEntry = await repository.FindFieldAsync(field => field.FieldId == request.FieldId, ct, asNoTracking: false);
        if (fieldEntry is null)
            return ServiceResult<FormFieldAdminDto>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND");

        var canonicalError = await ValidateCanonicalFieldAsync(fieldEntry, fieldEntry.FieldLabel, ct);
        if (canonicalError is not null)
            return ServiceResult<FormFieldAdminDto>.Fail(canonicalError, ErrorType.Conflict, "CONFIG_CANONICAL_FIELD_INVALID");

        if (!string.Equals(fieldEntry.EntityName, formDefinition.EntityName, StringComparison.OrdinalIgnoreCase))
            return ServiceResult<FormFieldAdminDto>.Fail("El campo no pertenece a la entidad del formulario.", ErrorType.Validation, "CONFIG_FORM_FIELD_ENTITY_MISMATCH");

        var duplicated = await repository.ExistsAssignmentAsync(formField => formField.FormId == formId && formField.FieldId == request.FieldId, ct);
        if (duplicated)
            return ServiceResult<FormFieldAdminDto>.Fail("El campo ya pertenece a este formulario.", ErrorType.Conflict, "CONFIG_FORM_FIELD_DUPLICATED");

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

        repository.AddAssignment(formFieldDefinition);
        await uow.SaveChangesAsync(ct);
        formFieldDefinition.Field = fieldEntry;

        return ServiceResult<FormFieldAdminDto>.Ok(MapFormField(formFieldDefinition), "Campo agregado al formulario.");
    }

    private async Task<ServiceResult<FormFieldAdminDto>> UpdateFormFieldCore(
        int formId,
        int formFieldId,
        UpdateFormFieldRequest request,
        CancellationToken ct)
    {
        var formFieldDefinition = await repository.FindAssignmentAsync(formField => formField.FormId == formId && formField.FormFieldId == formFieldId, ct, asNoTracking: false);

        if (formFieldDefinition is null)
            return ServiceResult<FormFieldAdminDto>.Fail("Campo de formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_FIELD_NOT_FOUND");

        var fieldEntry = formFieldDefinition.Field!;
        var canonicalError = await ValidateCanonicalFieldAsync(fieldEntry, fieldEntry.FieldLabel, ct);
        if (canonicalError is not null)
            return ServiceResult<FormFieldAdminDto>.Fail(canonicalError, ErrorType.Conflict, "CONFIG_CANONICAL_FIELD_INVALID");
        formFieldDefinition.IsVisible = request.IsVisible && fieldEntry.IsVisible && fieldEntry.IsActive;
        formFieldDefinition.IsRequired = request.IsRequired && fieldEntry.IsActive;
        formFieldDefinition.IsEditable = request.IsEditable && fieldEntry.IsEditable && fieldEntry.IsActive;
        formFieldDefinition.DisplayOrder = request.DisplayOrder;
        formFieldDefinition.GroupName = NormalizeOptional(request.GroupName);
        formFieldDefinition.ColumnSpan = NormalizeColumnSpan(request.ColumnSpan);
        formFieldDefinition.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);

        return ServiceResult<FormFieldAdminDto>.Ok(MapFormField(formFieldDefinition), "Campo de formulario actualizado.");
    }

    private async Task<ServiceResult<NoContent>> DeleteFormFieldCore(int formId, int formFieldId, CancellationToken ct)
    {
        var formFieldDefinition = await repository.FindAssignmentAsync(formField => formField.FormId == formId && formField.FormFieldId == formFieldId, ct, asNoTracking: false);
        if (formFieldDefinition is null)
            return ServiceResult<NoContent>.Fail("Campo de formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_FIELD_NOT_FOUND");

        repository.RemoveAssignment(formFieldDefinition);
        await uow.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Campo removido del formulario.");
    }

    private async Task<ServiceResult<DynamicFieldOptionDto>> CreateFieldOptionCore(
        int fieldId,
        CreateDynamicFieldOptionRequest request,
        CancellationToken ct)
    {
        var fieldExists = await repository.ExistsFieldAsync(field => field.FieldId == fieldId, ct);
        if (!fieldExists)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Campo no encontrado.", ErrorType.NotFound, "CONFIG_FIELD_NOT_FOUND");

        var validation = ValidateOptionRequest(request.OptionValue, request.OptionLabel);
        if (validation is not null)
            return ServiceResult<DynamicFieldOptionDto>.Fail(validation, ErrorType.Validation, "CONFIG_OPTION_INVALID");

        var optionValue = NormalizeKey(request.OptionValue);
        var duplicated = await repository.ExistsOptionAsync(option => option.FieldId == fieldId && option.OptionValue == optionValue, ct);
        if (duplicated)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Ya existe una opción con ese valor para el campo.", ErrorType.Conflict, "CONFIG_OPTION_DUPLICATED");

        var optionEntry = new DynamicFieldOption
        {
            FieldId = fieldId,
            OptionValue = optionValue,
            OptionLabel = request.OptionLabel.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        repository.AddOption(optionEntry);
        await uow.SaveChangesAsync(ct);

        return ServiceResult<DynamicFieldOptionDto>.Ok(MapOption(optionEntry), "Opción creada.");
    }

    private async Task<ServiceResult<DynamicFieldOptionDto>> UpdateFieldOptionCore(
        int fieldId,
        int optionId,
        UpdateDynamicFieldOptionRequest request,
        CancellationToken ct)
    {
        var validation = ValidateOptionRequest(request.OptionValue, request.OptionLabel);
        if (validation is not null)
            return ServiceResult<DynamicFieldOptionDto>.Fail(validation, ErrorType.Validation, "CONFIG_OPTION_INVALID");

        var optionEntry = await repository.FindOptionAsync(option => option.FieldId == fieldId && option.DynamicFieldOptionId == optionId, ct, asNoTracking: false);
        if (optionEntry is null)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Opción no encontrada.", ErrorType.NotFound, "CONFIG_OPTION_NOT_FOUND");

        var optionValue = NormalizeKey(request.OptionValue);
        var duplicated = await repository.ExistsOptionAsync(option => option.FieldId == fieldId && option.DynamicFieldOptionId != optionId && option.OptionValue == optionValue, ct);
        if (duplicated)
            return ServiceResult<DynamicFieldOptionDto>.Fail("Ya existe una opción con ese valor para el campo.", ErrorType.Conflict, "CONFIG_OPTION_DUPLICATED");

        optionEntry.OptionValue = optionValue;
        optionEntry.OptionLabel = request.OptionLabel.Trim();
        optionEntry.DisplayOrder = request.DisplayOrder;
        optionEntry.IsActive = request.IsActive;
        optionEntry.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);

        return ServiceResult<DynamicFieldOptionDto>.Ok(MapOption(optionEntry), "Opción actualizada.");
    }

    private async Task<ServiceResult<NoContent>> DeleteFieldOptionCore(int fieldId, int optionId, CancellationToken ct)
    {
        var optionEntry = await repository.FindOptionAsync(option => option.FieldId == fieldId && option.DynamicFieldOptionId == optionId, ct, asNoTracking: false);
        if (optionEntry is null)
            return ServiceResult<NoContent>.Fail("Opción no encontrada.", ErrorType.NotFound, "CONFIG_OPTION_NOT_FOUND");

        repository.RemoveOption(optionEntry);
        await uow.SaveChangesAsync(ct);

        return ServiceResult<NoContent>.Ok(default, "Opción eliminada.");
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
    private async Task<ServiceResult<ResolvedFormDto>> BuildResolvedFormAsync(int formId, CancellationToken ct)
    {
        var form = await repository.GetFormAsync(formId, ct);

        var fields = await repository.ListResolvedFieldsAsync(formId, ct);

        foreach (var assignment in fields)
        {
            var error = await ValidateCanonicalFieldAsync(assignment.Field!, assignment.Field!.FieldLabel, ct);
            if (error is not null)
                return ServiceResult<ResolvedFormDto>.Fail(error, ErrorType.Conflict, "CONFIG_CANONICAL_FIELD_INVALID");
        }
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

        return ServiceResult<ResolvedFormDto>.Ok(new ResolvedFormDto
        {
            FormId = form.FormId,
            FormKey = form.FormKey,
            FormName = form.FormName,
            EntityName = form.EntityName,
            Description = form.Description,
            Sections = sections
        }, "Formulario resuelto obtenido.");
    }

    private static ResolvedFormFieldDto MapResolvedField(tesisproject.backend.Data.UnifiedEntities.Articles.FormFieldDefinition formField)
    {
        var field = formField.Field!;
        var dto = new ResolvedFormFieldDto
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
            IsRequired = formField.IsRequired && field.IsActive,
            IsVisible = formField.IsVisible && field.IsVisible && field.IsActive,
            IsEditable = formField.IsEditable && field.IsEditable && field.IsActive,
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
        var attribute = CanonicalAttribute(field, field.FieldLabel);
        if (attribute.HasValue)
        {
            dto.IsDynamic = false;
            dto.SourceType = attribute == BaseProductAttributeId.Title ? "Product" : attribute == BaseProductAttributeId.Authors ? "ProductAuthors" : "ProductValue";
            dto.PhysicalTableName = attribute == BaseProductAttributeId.Title ? "dbo.Products" : attribute == BaseProductAttributeId.Authors ? "dbo.ProductAuthors" : "dbo.ArticleReadView";
            dto.PhysicalColumnName = attribute == BaseProductAttributeId.Authors ? null : UnifiedArticleConfigurationCanonicalFields.Column(attribute.Value);
            dto.DefaultValue = null; // Defaults for canonical values are not a second field-value source.
        }
        return dto;
    }

    private static BaseProductAttributeId? CanonicalAttribute(FieldCatalogEntry field, string label) =>
        UnifiedArticleConfigurationCanonicalFields.Attribute(field.EntityName, field.FieldKey, field.PhysicalColumnName, label,
            UnifiedArticleFormSelector.Normalize(field.PhysicalTableName) is "venues" or "dbovenues" && field.PhysicalColumnName == "Name" ? "Journal" : null);

    private async Task<string?> ValidateCanonicalFieldAsync(FieldCatalogEntry field, string label, CancellationToken ct)
    {
        var attribute = CanonicalAttribute(field, label);
        if (!attribute.HasValue) return null;
        if (field.IsDynamic) return "El atributo base pertenece a Product; no se permite una segunda fuente dinámica en Article.";
        if ((int)attribute >= (int)BaseProductAttributeId.Journal && !await repository.HasCanonicalAttributeAsync((int)attribute, ct))
            return $"Falta el atributo canónico {attribute} o sus definiciones para los tipos Article (1 y 2).";
        return null;
    }

    private Task<List<CatalogItemDto>> ReadCatalogItemsAsync(string key, CancellationToken ct)
        => repository.ReadCatalogItemsAsync(NormalizeCatalogKey(key), ct);

    private static string NormalizeCatalogKey(string? key)
        => (key ?? string.Empty).Trim().Replace("_", "-").ToLowerInvariant();
    public async Task<ServiceResult<List<CatalogAdminItemDto>>> GetAdminCatalog(string catalogKey, CancellationToken ct)
    {
        var items = await repository.ReadAdminCatalogAsync(NormalizeCatalogKey(catalogKey), ct);

        return ServiceResult<List<CatalogAdminItemDto>>.Ok(items, "Catalogo administrativo obtenido.");
    }


    private async Task<ServiceResult<T>> MutateAsync<T>(Func<Task<ServiceResult<T>>> operation, CancellationToken ct)
    {
        try { return await uow.ExecuteInTransactionAsync(_ => operation(), ct); }
        catch (Exception ex) when (UnifiedPersistenceErrors.Classify(ex) is PersistenceFailure.Duplicate or PersistenceFailure.DuplicateArticleDoi or PersistenceFailure.ReferenceConstraint)
        { return ServiceResult<T>.Fail("La configuración está duplicada o tiene referencias en uso.", ErrorType.Conflict, "CONFIG_PERSISTENCE_CONFLICT"); }
    }
}
