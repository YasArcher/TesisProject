using System.Linq.Expressions;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedArticleConfigurationRepository
{
    IQueryable<FormDefinition> FormDefinitions { get; }
    IQueryable<FieldCatalogEntry> FieldCatalogEntries { get; }
    IQueryable<FormFieldDefinition> FormFieldDefinitions { get; }
    IQueryable<DynamicFieldOption> DynamicFieldOptions { get; }
    IQueryable<DynamicFieldValue> DynamicFieldValues { get; }
    IQueryable<ProductAuthorDynamicFieldValue> ProductAuthorDynamicFieldValues { get; }
    void AddForm(FormDefinition form);
    Task RemoveFormAsync(FormDefinition form, CancellationToken ct);
    void AddField(FieldCatalogEntry field);
    Task RemoveFieldAsync(FieldCatalogEntry field, CancellationToken ct);
    void AddAssignment(FormFieldDefinition assignment);
    void RemoveAssignment(FormFieldDefinition assignment);
    void AddOption(DynamicFieldOption option);
    void RemoveOption(DynamicFieldOption option);
    Task DeactivateFormsAsync(string entityName, int? exceptFormId, CancellationToken ct);
    Task<bool> HasCanonicalAttributeAsync(int attributeId, CancellationToken ct);
    Task<List<CatalogItemDto>> ReadCatalogItemsAsync(string normalizedKey, CancellationToken ct);
    Task<List<CatalogAdminItemDto>> ReadAdminCatalogAsync(string normalizedKey, CancellationToken ct);

    Task<List<FormSummaryDto>> ListFormsAsync(string? entityName, CancellationToken ct);
    Task<List<FormFieldAdminDto>> ListFormFieldsAsync(int formId, CancellationToken ct);
    Task<List<DynamicFieldOptionDto>> ListOptionsAsync(int fieldId, CancellationToken ct);
    Task<List<FieldCatalogItemDto>> ListFieldsAsync(string entityName, bool dynamicOnly, CancellationToken ct);
    Task<List<FormFieldDefinition>> ListResolvedFieldsAsync(int formId, CancellationToken ct);
    Task<bool> ExistsFormAsync(Expression<Func<FormDefinition, bool>> predicate, CancellationToken ct);
    Task<FormDefinition?> FindFormAsync(Expression<Func<FormDefinition, bool>> predicate, CancellationToken ct, bool asNoTracking = false);
    Task<bool> ExistsFieldAsync(Expression<Func<FieldCatalogEntry, bool>> predicate, CancellationToken ct);
    Task<FieldCatalogEntry?> FindFieldAsync(Expression<Func<FieldCatalogEntry, bool>> predicate, CancellationToken ct, bool asNoTracking = false);
    Task<bool> ExistsAssignmentAsync(Expression<Func<FormFieldDefinition, bool>> predicate, CancellationToken ct);
    Task<FormFieldDefinition?> FindAssignmentAsync(Expression<Func<FormFieldDefinition, bool>> predicate, CancellationToken ct, bool asNoTracking = false);
    Task<bool> ExistsOptionAsync(Expression<Func<DynamicFieldOption, bool>> predicate, CancellationToken ct);
    Task<DynamicFieldOption?> FindOptionAsync(Expression<Func<DynamicFieldOption, bool>> predicate, CancellationToken ct, bool asNoTracking = false);
    Task<bool> ExistsArticleValueAsync(Expression<Func<DynamicFieldValue, bool>> predicate, CancellationToken ct);
    Task<bool> ExistsParticipantValueAsync(Expression<Func<ProductAuthorDynamicFieldValue, bool>> predicate, CancellationToken ct);
    Task<FormDefinition> GetFormAsync(int formId, CancellationToken ct);
}
