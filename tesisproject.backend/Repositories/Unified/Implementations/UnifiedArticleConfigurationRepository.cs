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
    public Task<List<CatalogItemDto>> ReadCatalogItemsAsync(string key, int? parentId, CancellationToken ct) => key switch
    {
        "faculties" or "faculty" or "facultyid" => context.Set<Faculty>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.FacultyId, Name = x.Name }).ToListAsync(ct),
        "research-lines" or "researchlines" or "researchline" or "researchlineid" => context.Set<ResearchLine>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.ResearchLineId, Name = x.Name }).ToListAsync(ct),
        "indexing-sources" or "indexingsources" or "indexingsource" or "indexingsourceid" => context.Set<IndexingSource>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
        "publication-statuses" or "publicationstatuses" or "publicationstatus" or "publicationstatusid" => context.Set<PublicationStatus>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.PublicationStatusId, Name = x.Name }).ToListAsync(ct),
        "academic-terms" or "academicterms" or "academicterm" or "academictermid" => context.Set<AcademicTerm>().AsNoTracking().OrderByDescending(x => x.StartDate).ThenBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.AcademicTermId, Name = x.Name }).ToListAsync(ct),
        "broad-fields" or "broadfields" or "broadfield" or "broadfieldid" => context.Set<BroadField>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.BroadFieldId, Name = x.Name }).ToListAsync(ct),
        "specific-fields" or "specificfields" or "specificfield" or "specificfieldid" => context.Set<SpecificField>().AsNoTracking().Where(x => !parentId.HasValue || x.BroadFieldId == parentId.Value).OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.SpecificFieldId, Name = x.Name }).ToListAsync(ct),
        "detailed-fields" or "detailedfields" or "detailedfield" or "detailedfieldid" => context.Set<DetailedField>().AsNoTracking().Where(x => !parentId.HasValue || x.SpecificFieldId == parentId.Value).OrderBy(x => x.Name).Select(x => new CatalogItemDto { Id = x.DetailedFieldId, Name = x.Name }).ToListAsync(ct),
        _ => Task.FromResult(new List<CatalogItemDto>())
    };
    public Task<List<CatalogAdminItemDto>> ReadAdminCatalogAsync(string key, CancellationToken ct) => key switch
    {
        // Unified Faculty has Acronym; a numeric external identity is not a display code.
        "faculties" => context.Set<Faculty>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.FacultyId, Name = x.Name, Code = x.Acronym, ParentId = x.ParentFacultyId, ParentName = x.Parent != null ? x.Parent.Name : null }).ToListAsync(ct),
        "research-lines" or "researchlines" => context.Set<ResearchLine>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.ResearchLineId, Name = x.Name }).ToListAsync(ct),
        "indexing-sources" or "indexingsources" => context.Set<IndexingSource>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
        "publication-statuses" or "publicationstatuses" => context.Set<PublicationStatus>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.PublicationStatusId, Name = x.Name }).ToListAsync(ct),
        "academic-terms" or "academicterms" => context.Set<AcademicTerm>().AsNoTracking().OrderByDescending(x => x.StartDate).ThenBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.AcademicTermId, Name = x.Name }).ToListAsync(ct),
        "broad-fields" or "broadfields" => context.Set<BroadField>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.BroadFieldId, Name = x.Name }).ToListAsync(ct),
        "specific-fields" or "specificfields" => context.Set<SpecificField>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.SpecificFieldId, Name = x.Name, Code = x.Code, ParentId = x.BroadFieldId, ParentName = x.BroadField.Name }).ToListAsync(ct),
        "detailed-fields" or "detailedfields" => context.Set<DetailedField>().AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogAdminItemDto { Id = x.DetailedFieldId, Name = x.Name, Code = x.Code, ParentId = x.SpecificFieldId, ParentName = x.SpecificField.Name }).ToListAsync(ct),
        _ => Task.FromResult(new List<CatalogAdminItemDto>())
    };

    public async Task<CatalogAdminItemDto?> CreateAdminCatalogAsync(string key, UpsertCatalogItemRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        switch (key)
        {
            case "faculties":
                var faculty = new Faculty { Name = name, Acronym = NormalizeOptional(request.Code), ParentFacultyId = request.ParentId };
                context.Add(faculty);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = faculty.FacultyId, Name = faculty.Name, Code = faculty.Acronym, ParentId = faculty.ParentFacultyId };
            case "research-lines":
            case "researchlines":
                var researchLine = new ResearchLine { Name = name };
                context.Add(researchLine);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = researchLine.ResearchLineId, Name = researchLine.Name };
            case "indexing-sources":
            case "indexingsources":
                var indexingSource = new IndexingSource { Name = name, Abbreviation = NormalizeOptional(request.Code), ReferenceUrl = NormalizeOptional(request.JournalUrl), IsActive = true };
                context.Add(indexingSource);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = indexingSource.Id, Name = indexingSource.Name, Code = indexingSource.Abbreviation, JournalUrl = indexingSource.ReferenceUrl };
            case "publication-statuses":
            case "publicationstatuses":
                var nextStatusId = await NextPublicationStatusIdAsync(ct);
                var status = new PublicationStatus { PublicationStatusId = nextStatusId, Name = name };
                context.Add(status);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = status.PublicationStatusId, Name = status.Name };
            case "academic-terms":
            case "academicterms":
                var term = new AcademicTerm { Name = name };
                context.Add(term);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = term.AcademicTermId, Name = term.Name };
            case "broad-fields":
            case "broadfields":
                var broadField = new BroadField { Name = name };
                context.Add(broadField);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = broadField.BroadFieldId, Name = broadField.Name };
            case "specific-fields":
            case "specificfields":
                if (!request.ParentId.HasValue || !await context.Set<BroadField>().AnyAsync(x => x.BroadFieldId == request.ParentId.Value, ct)) return null;
                var specificField = new SpecificField { Name = name, Code = NormalizeOptional(request.Code), BroadFieldId = request.ParentId.Value };
                context.Add(specificField);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = specificField.SpecificFieldId, Name = specificField.Name, Code = specificField.Code, ParentId = specificField.BroadFieldId };
            case "detailed-fields":
            case "detailedfields":
                if (!request.ParentId.HasValue || !await context.Set<SpecificField>().AnyAsync(x => x.SpecificFieldId == request.ParentId.Value, ct)) return null;
                var detailedField = new DetailedField { Name = name, Code = NormalizeOptional(request.Code), SpecificFieldId = request.ParentId.Value };
                context.Add(detailedField);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = detailedField.DetailedFieldId, Name = detailedField.Name, Code = detailedField.Code, ParentId = detailedField.SpecificFieldId };
            default:
                return null;
        }
    }

    public async Task<CatalogAdminItemDto?> UpdateAdminCatalogAsync(string key, int id, UpsertCatalogItemRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        switch (key)
        {
            case "faculties":
                var faculty = await context.Set<Faculty>().FindAsync([id], ct);
                if (faculty is null) return null;
                faculty.Name = name; faculty.Acronym = NormalizeOptional(request.Code); faculty.ParentFacultyId = request.ParentId;
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = faculty.FacultyId, Name = faculty.Name, Code = faculty.Acronym, ParentId = faculty.ParentFacultyId };
            case "research-lines":
            case "researchlines":
                var researchLine = await context.Set<ResearchLine>().FindAsync([id], ct);
                if (researchLine is null) return null;
                researchLine.Name = name;
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = researchLine.ResearchLineId, Name = researchLine.Name };
            case "indexing-sources":
            case "indexingsources":
                var indexingSource = await context.Set<IndexingSource>().FindAsync([id], ct);
                if (indexingSource is null) return null;
                indexingSource.Name = name; indexingSource.Abbreviation = NormalizeOptional(request.Code); indexingSource.ReferenceUrl = NormalizeOptional(request.JournalUrl);
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = indexingSource.Id, Name = indexingSource.Name, Code = indexingSource.Abbreviation, JournalUrl = indexingSource.ReferenceUrl };
            case "publication-statuses":
            case "publicationstatuses":
                if (id is < byte.MinValue or > byte.MaxValue) return null;
                var status = await context.Set<PublicationStatus>().FindAsync([(byte)id], ct);
                if (status is null) return null;
                status.Name = name;
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = status.PublicationStatusId, Name = status.Name };
            case "academic-terms":
            case "academicterms":
                var term = await context.Set<AcademicTerm>().FindAsync([id], ct);
                if (term is null) return null;
                term.Name = name;
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = term.AcademicTermId, Name = term.Name };
            case "broad-fields":
            case "broadfields":
                var broadField = await context.Set<BroadField>().FindAsync([id], ct);
                if (broadField is null) return null;
                broadField.Name = name;
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = broadField.BroadFieldId, Name = broadField.Name };
            case "specific-fields":
            case "specificfields":
                if (!request.ParentId.HasValue || !await context.Set<BroadField>().AnyAsync(x => x.BroadFieldId == request.ParentId.Value, ct)) return null;
                var specificField = await context.Set<SpecificField>().FindAsync([id], ct);
                if (specificField is null) return null;
                specificField.Name = name; specificField.Code = NormalizeOptional(request.Code); specificField.BroadFieldId = request.ParentId.Value;
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = specificField.SpecificFieldId, Name = specificField.Name, Code = specificField.Code, ParentId = specificField.BroadFieldId };
            case "detailed-fields":
            case "detailedfields":
                if (!request.ParentId.HasValue || !await context.Set<SpecificField>().AnyAsync(x => x.SpecificFieldId == request.ParentId.Value, ct)) return null;
                var detailedField = await context.Set<DetailedField>().FindAsync([id], ct);
                if (detailedField is null) return null;
                detailedField.Name = name; detailedField.Code = NormalizeOptional(request.Code); detailedField.SpecificFieldId = request.ParentId.Value;
                await context.SaveChangesAsync(ct);
                return new CatalogAdminItemDto { Id = detailedField.DetailedFieldId, Name = detailedField.Name, Code = detailedField.Code, ParentId = detailedField.SpecificFieldId };
            default:
                return null;
        }
    }

    public async Task<bool> DeleteAdminCatalogAsync(string key, int id, CancellationToken ct)
    {
        object? entity = key switch
        {
            "faculties" => await context.Set<Faculty>().FindAsync([id], ct),
            "research-lines" or "researchlines" => await context.Set<ResearchLine>().FindAsync([id], ct),
            "indexing-sources" or "indexingsources" => await context.Set<IndexingSource>().FindAsync([id], ct),
            "publication-statuses" or "publicationstatuses" when id is >= byte.MinValue and <= byte.MaxValue => await context.Set<PublicationStatus>().FindAsync([(byte)id], ct),
            "academic-terms" or "academicterms" => await context.Set<AcademicTerm>().FindAsync([id], ct),
            "broad-fields" or "broadfields" => await context.Set<BroadField>().FindAsync([id], ct),
            "specific-fields" or "specificfields" => await context.Set<SpecificField>().FindAsync([id], ct),
            "detailed-fields" or "detailedfields" => await context.Set<DetailedField>().FindAsync([id], ct),
            _ => null
        };
        if (entity is null) return false;
        context.Remove(entity);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> AdminCatalogItemExistsAsync(string key, int id, CancellationToken ct) => key switch
    {
        "faculties" => context.Set<Faculty>().AnyAsync(x => x.FacultyId == id, ct),
        "research-lines" or "researchlines" => context.Set<ResearchLine>().AnyAsync(x => x.ResearchLineId == id, ct),
        "indexing-sources" or "indexingsources" => context.Set<IndexingSource>().AnyAsync(x => x.Id == id, ct),
        "publication-statuses" or "publicationstatuses" when id is >= byte.MinValue and <= byte.MaxValue => context.Set<PublicationStatus>().AnyAsync(x => x.PublicationStatusId == (byte)id, ct),
        "academic-terms" or "academicterms" => context.Set<AcademicTerm>().AnyAsync(x => x.AcademicTermId == id, ct),
        "broad-fields" or "broadfields" => context.Set<BroadField>().AnyAsync(x => x.BroadFieldId == id, ct),
        "specific-fields" or "specificfields" => context.Set<SpecificField>().AnyAsync(x => x.SpecificFieldId == id, ct),
        "detailed-fields" or "detailedfields" => context.Set<DetailedField>().AnyAsync(x => x.DetailedFieldId == id, ct),
        _ => Task.FromResult(false)
    };

    public async Task<bool> AdminCatalogItemInUseAsync(string key, int id, CancellationToken ct) => key switch
    {
        "faculties" => await context.Set<Article>().AnyAsync(x => x.FacultyId == id, ct) || await context.Set<Faculty>().AnyAsync(x => x.ParentFacultyId == id, ct),
        "research-lines" or "researchlines" => await context.Set<Article>().AnyAsync(x => x.ResearchLineId == id, ct),
        "indexing-sources" or "indexingsources" => await context.Set<ArticleIndexing>().AnyAsync(x => x.IndexingSourceId == id, ct),
        "publication-statuses" or "publicationstatuses" when id is >= byte.MinValue and <= byte.MaxValue => await context.Set<Article>().AnyAsync(x => x.PublicationStatusId == (byte)id, ct),
        "academic-terms" or "academicterms" => await context.Set<Article>().AnyAsync(x => x.AcademicTermId == id, ct),
        "broad-fields" or "broadfields" => await context.Set<Article>().AnyAsync(x => x.BroadFieldId == id, ct) || await context.Set<SpecificField>().AnyAsync(x => x.BroadFieldId == id, ct),
        "specific-fields" or "specificfields" => await context.Set<Article>().AnyAsync(x => x.SpecificFieldId == id, ct) || await context.Set<DetailedField>().AnyAsync(x => x.SpecificFieldId == id, ct),
        "detailed-fields" or "detailedfields" => await context.Set<Article>().AnyAsync(x => x.DetailedFieldId == id, ct),
        _ => true
    };

    private async Task<byte> NextPublicationStatusIdAsync(CancellationToken ct)
    {
        var maxId = await context.Set<PublicationStatus>().Select(x => (byte?)x.PublicationStatusId).MaxAsync(ct) ?? (byte)0;
        if (maxId == byte.MaxValue)
            throw new InvalidOperationException("No hay identificadores disponibles para estados de publicación.");
        return (byte)(maxId + 1);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
