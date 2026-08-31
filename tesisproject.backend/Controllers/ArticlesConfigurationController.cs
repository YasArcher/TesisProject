// [ARTICLES-MIGRATION] Endpoints de solo lectura para validar configuracion de articulos dentro del backend base de proyectos.
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
            query = query.OrderByDescending(form => form.FormKey == preferredFormKey).ThenBy(form => form.FormName);
        else
            query = query.OrderBy(form => form.FormName);

        var form = await query.FirstOrDefaultAsync(ct);
        if (form is null)
            return ServiceResult<ResolvedFormDto>.Fail("Formulario no encontrado.", ErrorType.NotFound, "CONFIG_FORM_NOT_FOUND").ToActionResult();

        var resolved = await BuildResolvedFormAsync(form.FormId, ct);
        return ServiceResult<ResolvedFormDto>.Ok(resolved, "Formulario resuelto obtenido.").ToActionResult();
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




