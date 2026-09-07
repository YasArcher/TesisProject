using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations;

// Closed catalog set owned by the UoW; no service locator or separate repository scope.
internal static class UnifiedCatalogAccess
{
    internal static IUnifiedCatalogRepository<T> Get<T>(IUnifiedUnitOfWork uow) where T : CatalogEntityBase
    {
        if (uow.Countries is IUnifiedCatalogRepository<T> countries) return countries;
        if (uow.DocumentTypes is IUnifiedCatalogRepository<T> documenttypes) return documenttypes;
        if (uow.FundingTypes is IUnifiedCatalogRepository<T> fundingtypes) return fundingtypes;
        if (uow.IndexingSources is IUnifiedCatalogRepository<T> indexingsources) return indexingsources;
        if (uow.Institutions is IUnifiedCatalogRepository<T> institutions) return institutions;
        if (uow.MemberRoleTypes is IUnifiedCatalogRepository<T> memberroletypes) return memberroletypes;
        if (uow.ObjectiveTypes is IUnifiedCatalogRepository<T> objectivetypes) return objectivetypes;
        if (uow.ProductAttributes is IUnifiedCatalogRepository<T> productattributes) return productattributes;
        if (uow.ProductTypes is IUnifiedCatalogRepository<T> producttypes) return producttypes;
        if (uow.ProjectExtensionTypes is IUnifiedCatalogRepository<T> projectextensiontypes) return projectextensiontypes;
        if (uow.ProjectOriginTypes is IUnifiedCatalogRepository<T> projectorigintypes) return projectorigintypes;
        if (uow.ProjectStates is IUnifiedCatalogRepository<T> projectstates) return projectstates;
        if (uow.ProjectTypes is IUnifiedCatalogRepository<T> projecttypes) return projecttypes;
        if (uow.ResearchCategoryGroups is IUnifiedCatalogRepository<T> researchcategorygroups) return researchcategorygroups;
        if (uow.ResearchCategoryTypes is IUnifiedCatalogRepository<T> researchcategorytypes) return researchcategorytypes;
        if (uow.TransactionTypes is IUnifiedCatalogRepository<T> transactiontypes) return transactiontypes;
        if (uow.VisitStates is IUnifiedCatalogRepository<T> visitstates) return visitstates;
        throw new NotSupportedException(ErrorMessages.Common.InvalidRequest);
    }
}
