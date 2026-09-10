using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedArticleConfigurationRepository(UnifiedDideDbContext context) : IUnifiedArticleConfigurationRepository
{
    public IQueryable<FormDefinition> FormDefinitions => context.Set<FormDefinition>();
    public IQueryable<FieldCatalogEntry> FieldCatalogEntries => context.Set<FieldCatalogEntry>();
    public IQueryable<FormFieldDefinition> FormFieldDefinitions => context.Set<FormFieldDefinition>();
    public IQueryable<DynamicFieldOption> DynamicFieldOptions => context.Set<DynamicFieldOption>();
    public IQueryable<DynamicFieldValue> DynamicFieldValues => context.Set<DynamicFieldValue>();
    public IQueryable<ProductAuthorDynamicFieldValue> ProductAuthorDynamicFieldValues => context.Set<ProductAuthorDynamicFieldValue>();
    public void AddForm(FormDefinition form) => context.Add(form);
    public async Task RemoveFormAsync(FormDefinition form, CancellationToken ct)
    {
        context.RemoveRange(await FormFieldDefinitions.Where(f => f.FormId == form.FormId).ToListAsync(ct));
        context.Remove(form);
    }
    public void AddField(FieldCatalogEntry field) => context.Add(field);
    public async Task RemoveFieldAsync(FieldCatalogEntry field, CancellationToken ct)
    {
        context.RemoveRange(await DynamicFieldOptions.Where(o => o.FieldId == field.FieldId).ToListAsync(ct));
        context.Remove(field);
    }
    public void AddAssignment(FormFieldDefinition assignment) => context.Add(assignment);
    public void RemoveAssignment(FormFieldDefinition assignment) => context.Remove(assignment);
    public void AddOption(DynamicFieldOption option) => context.Add(option);
    public void RemoveOption(DynamicFieldOption option) => context.Remove(option);
    public async Task DeactivateFormsAsync(string entityName, int? exceptFormId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await FormDefinitions.Where(f => f.EntityName == entityName && f.IsActive && (!exceptFormId.HasValue || f.FormId != exceptFormId))
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsActive, false).SetProperty(f => f.UpdatedAt, now), ct);
        // Bulk SQL bypasses tracking. Keep previously loaded forms consistent in this scoped UoW.
        foreach (var entry in context.ChangeTracker.Entries<FormDefinition>().Where(e =>
            e.Entity.EntityName == entityName && e.Entity.FormId != exceptFormId && e.Entity.IsActive))
        {
            entry.Entity.IsActive = false; entry.Entity.UpdatedAt = now;
            entry.Property(f => f.IsActive).OriginalValue = false;
            entry.Property(f => f.UpdatedAt).OriginalValue = now;
        }
    }
    public async Task<bool> HasCanonicalAttributeAsync(int attributeId, CancellationToken ct)
    {
        if (!await context.Set<ProductAttribute>().AnyAsync(a => a.Id == attributeId, ct)) return false;
        return await context.Set<ProductAttributeDefinition>().Where(d => d.ProductAttributeId == attributeId &&
            (d.ProductTypeId == (int)BaseProductTypeId.ScientificProduction || d.ProductTypeId == (int)BaseProductTypeId.RegionalProduction))
            .Select(d => d.ProductTypeId).Distinct().CountAsync(ct) == 2;
    }
    public Task<List<CatalogItemDto>> ReadCatalogItemsAsync(string key, CancellationToken ct) => key switch
    {
        "faculties" or "faculty" or "facultyid" => context.Set<Faculty>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.FacultyId, Name = x.Name }).ToListAsync(ct),
        "research-lines" or "researchline" or "researchlineid" => context.Set<ResearchLine>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.ResearchLineId, Name = x.Name }).ToListAsync(ct),
        "indexing-sources" or "indexingsource" or "indexingsourceid" => context.Set<IndexingSource>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
        "publication-statuses" or "publicationstatus" or "publicationstatusid" => context.Set<PublicationStatus>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.PublicationStatusId, Name = x.Name }).ToListAsync(ct),
        _ => Task.FromResult(new List<CatalogItemDto>())
    };
    public Task<List<CatalogAdminItemDto>> ReadAdminCatalogAsync(string key, CancellationToken ct) => key switch
    {
        // Unified Faculty has Acronym; a numeric external identity is not a display code.
        "faculties" => context.Set<Faculty>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.FacultyId, Name = x.Name, Code = x.Acronym }).ToListAsync(ct),
        "research-lines" => context.Set<ResearchLine>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.ResearchLineId, Name = x.Name }).ToListAsync(ct),
        "indexing-sources" => context.Set<IndexingSource>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
        "publication-statuses" => context.Set<PublicationStatus>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.PublicationStatusId, Name = x.Name }).ToListAsync(ct),
        _ => Task.FromResult(new List<CatalogAdminItemDto>())
    };

    public Task<List<FormSummaryDto>> ListFormsAsync(string? entityName, CancellationToken ct)
    {
        var query = FormDefinitions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(form => form.EntityName == entityName);

        return query
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
    }

    public Task<List<FormFieldAdminDto>> ListFormFieldsAsync(int formId, CancellationToken ct)
    {
        return FormFieldDefinitions
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
    }

    public Task<List<DynamicFieldOptionDto>> ListOptionsAsync(int fieldId, CancellationToken ct)
    {
        return DynamicFieldOptions
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
    }

    private IQueryable<FieldCatalogItemDto> GetFieldsQuery(string entityName)
    {
        return FieldCatalogEntries
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

    public Task<List<FieldCatalogItemDto>> ListFieldsAsync(string entityName, bool dynamicOnly, CancellationToken ct)
    {
        var query = GetFieldsQuery(entityName);
        if (dynamicOnly) query = query.Where(field => field.IsDynamic);
        return query.ToListAsync(ct);
    }

    public Task<List<FormFieldDefinition>> ListResolvedFieldsAsync(int formId, CancellationToken ct)
    {
        return FormFieldDefinitions
            .AsNoTracking()
            .Where(formField => formField.FormId == formId && formField.Field != null)
            .Include(formField => formField.Field)
            .ThenInclude(field => field!.Options)
            .OrderBy(formField => formField.DisplayOrder)
            .ThenBy(formField => formField.Field!.FieldLabel)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsFormAsync(Expression<Func<FormDefinition, bool>> predicate, CancellationToken ct)
    {
        return FormDefinitions.AnyAsync(predicate, ct);
    }

    public Task<FormDefinition?> FindFormAsync(Expression<Func<FormDefinition, bool>> predicate, CancellationToken ct, bool asNoTracking = false)
    {
        var query = FormDefinitions;
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(predicate, ct);
    }

    public Task<bool> ExistsFieldAsync(Expression<Func<FieldCatalogEntry, bool>> predicate, CancellationToken ct)
    {
        return FieldCatalogEntries.AnyAsync(predicate, ct);
    }

    public Task<FieldCatalogEntry?> FindFieldAsync(Expression<Func<FieldCatalogEntry, bool>> predicate, CancellationToken ct, bool asNoTracking = false)
    {
        var query = FieldCatalogEntries;
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(predicate, ct);
    }

    public Task<bool> ExistsAssignmentAsync(Expression<Func<FormFieldDefinition, bool>> predicate, CancellationToken ct)
    {
        return FormFieldDefinitions.AnyAsync(predicate, ct);
    }

    public Task<FormFieldDefinition?> FindAssignmentAsync(Expression<Func<FormFieldDefinition, bool>> predicate, CancellationToken ct, bool asNoTracking = false)
    {
        var query = FormFieldDefinitions.Include(x => x.Field);
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(predicate, ct);
    }

    public Task<bool> ExistsOptionAsync(Expression<Func<DynamicFieldOption, bool>> predicate, CancellationToken ct)
    {
        return DynamicFieldOptions.AnyAsync(predicate, ct);
    }

    public Task<DynamicFieldOption?> FindOptionAsync(Expression<Func<DynamicFieldOption, bool>> predicate, CancellationToken ct, bool asNoTracking = false)
    {
        var query = DynamicFieldOptions;
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(predicate, ct);
    }

    public Task<bool> ExistsArticleValueAsync(Expression<Func<DynamicFieldValue, bool>> predicate, CancellationToken ct)
    {
        return DynamicFieldValues.AnyAsync(predicate, ct);
    }

    public Task<bool> ExistsParticipantValueAsync(Expression<Func<ProductAuthorDynamicFieldValue, bool>> predicate, CancellationToken ct)
    {
        return ProductAuthorDynamicFieldValues.AnyAsync(predicate, ct);
    }

    public Task<FormDefinition> GetFormAsync(int formId, CancellationToken ct)
    {
        return FormDefinitions.AsNoTracking().FirstAsync(item => item.FormId == formId, ct);
    }
}
