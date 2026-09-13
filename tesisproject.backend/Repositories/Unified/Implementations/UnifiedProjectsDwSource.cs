using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedProjectsDwSource(UnifiedDideDbContext context) : IUnifiedProjectsDwSource
{
    public async Task<ProjectsDwDateBounds> GetDateBoundsAsync(CancellationToken ct = default)
    {
        var minProjectStart = await context.Projects.Select(p => p.StartDate).MinAsync(ct);
        var maxProjectStart = await context.Projects.Select(p => p.StartDate).MaxAsync(ct);
        var minProjectEnd = await context.Projects.Select(p => p.RealEndDate).MinAsync(ct);
        var maxProjectEnd = await context.Projects.Select(p => p.RealEndDate).MaxAsync(ct);
        var minProjectApproval = await context.Projects.Select(p => p.ApprovalDate).MinAsync(ct);
        var maxProjectApproval = await context.Projects.Select(p => p.ApprovalDate).MaxAsync(ct);
        var minBudgetApproved = await context.Budgets.Select(b => b.ApprovedAt).MinAsync(ct);
        var maxBudgetApproved = await context.Budgets.Select(b => b.ApprovedAt).MaxAsync(ct);

        var projectProducts = context.Products.Where(p => p.ProjectId != null);
        var minProductCreated = await projectProducts.Select(p => (DateTime?)p.CreatedAt).MinAsync(ct);
        var maxProductCreated = await projectProducts.Select(p => (DateTime?)p.CreatedAt).MaxAsync(ct);

        return new ProjectsDwDateBounds(
            minProjectStart, maxProjectStart,
            minProjectEnd, maxProjectEnd,
            minProjectApproval, maxProjectApproval,
            minBudgetApproved, maxBudgetApproved,
            minProductCreated, maxProductCreated);
    }

    public async Task<IReadOnlyList<ProjectsDwCatalogRow>> ListProjectStatesAsync(CancellationToken ct = default) =>
        await context.ProjectStates.AsNoTracking()
            .Select(x => new ProjectsDwCatalogRow(x.Id, x.Name, x.IsActive))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectsDwCatalogRow>> ListFundingTypesAsync(CancellationToken ct = default) =>
        await context.FundingTypes.AsNoTracking()
            .Select(x => new ProjectsDwCatalogRow(x.Id, x.Name, x.IsActive))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectsDwCatalogRow>> ListProductTypesAsync(CancellationToken ct = default) =>
        await context.ProductTypes.AsNoTracking()
            .Select(x => new ProjectsDwCatalogRow(x.Id, x.Name, x.IsActive))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectsDwFacultyRow>> ListUsedFacultiesAsync(CancellationToken ct = default)
    {
        var facultyIds = context.Projects.AsNoTracking().Select(p => p.FacultyId).Distinct();

        return await (
            from facultyId in facultyIds
            join faculty in context.Faculties.AsNoTracking()
                on facultyId equals faculty.FacultyId into matchingFaculties
            from faculty in matchingFaculties.DefaultIfEmpty()
            select new ProjectsDwFacultyRow(
                facultyId,
                faculty == null ? null : faculty.Acronym,
                faculty == null ? null : faculty.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProjectsDwResearchCategoryRow>> ListResearchCategoriesAsync(CancellationToken ct = default) =>
        await context.ResearchCategories.AsNoTracking()
            .Select(category => new ProjectsDwResearchCategoryRow(
                category.Id,
                category.Name,
                category.ResearchCategoryTypeId,
                category.ResearchCategoryType.Name,
                category.ResearchCategoryType.ResearchCategoryGroupId,
                category.ResearchCategoryType.ResearchCategoryGroup.Name,
                category.ParentCategoryId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ListProjectProductAttributeValuesAsync(
        BaseProductAttributeId attributeId,
        CancellationToken ct = default) =>
        await context.ProductValues.AsNoTracking()
            .Where(value =>
                value.Product != null &&
                value.Product.ProjectId != null &&
                value.AttributeDefinition != null &&
                value.AttributeDefinition.ProductTypeId == value.Product.ProductTypeId &&
                value.AttributeDefinition.ProductAttributeId == (int)attributeId &&
                value.Value != null && value.Value != "")
            .Select(value => value.Value!)
            .Distinct()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectsDwProjectResearchCategoryRow>> ListProjectResearchCategoriesAsync(CancellationToken ct = default) =>
        await context.ProjectResearchCategories.AsNoTracking()
            .Select(link => new ProjectsDwProjectResearchCategoryRow(link.ProjectId, link.ResearchCategoryId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectsDwProjectRow>> ListProjectsAsync(CancellationToken ct = default) =>
        await context.Projects.AsNoTracking()
            .Select(project => new ProjectsDwProjectRow(
                project.ProjectId,
                project.DurationInMonths,
                project.ExecutionPercentage,
                project.FacultyId,
                project.ProjectStateId,
                project.ApprovalDate,
                project.StartDate,
                project.RealEndDate))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectsDwBudgetRow>> ListBudgetsAsync(CancellationToken ct = default) =>
        await context.Budgets.AsNoTracking()
            .Select(budget => new ProjectsDwBudgetRow(
                budget.BudgetId,
                budget.ProjectId,
                budget.InitialAmount,
                budget.CertifiedAmount,
                budget.ExecutedAmount,
                budget.Project.FacultyId,
                budget.FundingTypeId,
                budget.ApprovedAt))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectsDwProductRow>> ListProjectProductsAsync(CancellationToken ct = default)
    {
        var journalId = (int)BaseProductAttributeId.Journal;
        var indexingDatabaseId = (int)BaseProductAttributeId.IndexingDatabase;
        var sjrId = (int)BaseProductAttributeId.Sjr;
        var quartileId = (int)BaseProductAttributeId.Quartile;
        var issnIsbnId = (int)BaseProductAttributeId.IssnIsbn;
        var doiId = (int)BaseProductAttributeId.Doi;
        var yearId = (int)BaseProductAttributeId.Year;
        var consultationUrlId = (int)BaseProductAttributeId.ConsultationUrl;

        return await context.Products.AsNoTracking()
            .Where(product => product.ProjectId != null)
            .Select(product => new ProjectsDwProductRow(
                product.Id,
                product.ProjectId!.Value,
                product.ProductTypeId,
                product.IsActive,
                product.CreatedAt,
                product.Project!.FacultyId,
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == indexingDatabaseId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == quartileId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Title,
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == journalId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == sjrId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == doiId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == yearId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == issnIsbnId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Values!
                    .Where(value =>
                        value.AttributeDefinition != null &&
                        value.AttributeDefinition.ProductTypeId == product.ProductTypeId &&
                        value.AttributeDefinition.ProductAttributeId == consultationUrlId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                product.Authors!.Count))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProjectsDwProductAuthorRow>> ListProjectProductAuthorsAsync(CancellationToken ct = default) =>
        await context.ProductAuthors.AsNoTracking()
            .Where(productAuthor => productAuthor.Product.ProjectId != null)
            .Select(productAuthor => new ProjectsDwProductAuthorRow(
                productAuthor.Id,
                productAuthor.ProductId,
                productAuthor.AuthorId,
                productAuthor.Author.AppUser == null
                    ? null
                    : productAuthor.Author.AppUser.IdUser,
                productAuthor.Author.AppUser == null
                    ? null
                    : productAuthor.Author.AppUser.IdAsp,
                productAuthor.Author.ExternalResearcherId,
                productAuthor.Author.ExternalResearcher == null
                    ? null
                    : productAuthor.Author.ExternalResearcher.FullName,
                productAuthor.Author.Orcid,
                productAuthor.AuthorOrder,
                productAuthor.IsPrimaryAuthor,
                productAuthor.Participation,
                productAuthor.NameSnapshot,
                productAuthor.Author.AppUserId != null))
            .ToListAsync(ct);
}
