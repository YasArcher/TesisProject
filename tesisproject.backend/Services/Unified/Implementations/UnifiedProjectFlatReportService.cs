using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedProjectFlatReportService : IUnifiedProjectFlatReportService
    {
        private readonly IUnifiedUnitOfWork _uow;
        private readonly IExternalDirectoryClient _externalDirectory;
        private readonly ILogger<UnifiedProjectFlatReportService> _logger;

        public UnifiedProjectFlatReportService(
            IUnifiedUnitOfWork uow,
            IExternalDirectoryClient externalDirectory,
            ILogger<UnifiedProjectFlatReportService> logger)
        {
            _uow = uow;
            _externalDirectory = externalDirectory;
            _logger = logger;
        }

        public async Task<ServiceResult<IReadOnlyList<ProjectFlatReportDTO>>> GetFlatReportAsync(
            IEnumerable<int>? projectIds = null,
            CancellationToken ct = default)
        {
            // =========================
            //   LOCAL SYNCHRONIZED FACULTIES
            // =========================
            var facultyDict = (await _uow.Faculties.GetAllAsync(ct: ct)).ToDictionary(f => f.FacultyId);

            // =========================
            //      CATALOG TABLES
            // =========================
            var projectTypes = await _uow.ProjectTypes.Query().ToListAsync(ct);
            var projectStates = await _uow.ProjectStates.Query().ToListAsync(ct);
            var convocationList = await _uow.Convocations.Query().ToListAsync(ct);
            var groups = await _uow.Groups.Query().Include(g => g.GroupType).ToListAsync(ct);
            var groupTypes = groups.Select(g => g.GroupType).Where(t => t is not null).DistinctBy(t => t.Id).ToList();
            var fundingTypes = await _uow.FundingTypes.Query().ToListAsync(ct);
            var productTypes = await _uow.ProductTypes.Query().ToListAsync(ct);
            var objectiveTypes = await _uow.ObjectiveTypes.Query().ToListAsync(ct);
            var institutions = await _uow.Institutions.Query().ToListAsync(ct);
            var productAttributes = await _uow.ProductAttributes.Query().ToListAsync(ct);
            var attributeDefs = await _uow.ProductAttributeDefinitions.Query().ToListAsync(ct);

            // =========================
            //      OPERATIONAL TABLES
            // =========================
            var projectQuery = _uow.Projects.Query().AsQueryable();

            if (projectIds != null && projectIds.Any())
            {
                projectQuery = projectQuery.Where(p => projectIds.Contains(p.ProjectId));
            }

            _logger.LogInformation(
                "Generating flat report for {Count} projects.",
                projectIds?.Count() ?? await _uow.Projects.CountAsync(ct: ct));

            var projects = await projectQuery.ToListAsync(ct);

            var budgets = await _uow.Budgets.Query().ToListAsync(ct);
            var products = await _uow.Products.Query().ToListAsync(ct);
            var productValues = await _uow.ProductValues.Query().ToListAsync(ct);
            var projectObjectives = await _uow.ProjectObjectives.Query().ToListAsync(ct);
            var projectResearchCats = await _uow.ProjectResearchCategories.Query().ToListAsync(ct);
            var externalResearchers = await _uow.ExternalResearchers.Query().ToListAsync(ct);
            var externalResearcherProj = await _uow.ExternalResearcherProjects.Query().ToListAsync(ct);
            // Scopes y Visits NO se usan en el flat report actual

            // 🔹 NUEVO: integrantes de grupos y roles
            var groupMembers = await _uow.GroupMembers.Query().ToListAsync(ct);
            var memberRoleTypes = await _uow.MemberRoleTypes.Query().ToListAsync(ct);

            // 🔹 NUEVO: tabla puente de usuarios (IdUser -> IdAsp)
            var appUsers = await _uow.AppUsers.Query().ToListAsync(ct);

            // =========================
            //   LOOKUPS AUXILIARES
            // =========================
            var projectTypeById = ToFirstByKey(projectTypes, x => x.Id);
            var projectStateById = ToFirstByKey(projectStates, x => x.Id);
            var convocationById = ToFirstByKey(convocationList, x => x.Id);
            var groupById = ToFirstByKey(groups, x => x.GroupId);
            var groupTypeById = ToFirstByKey(groupTypes, x => x.Id);
            var fundingTypeById = ToFirstByKey(fundingTypes, x => x.Id);
            var productTypeById = ToFirstByKey(productTypes, x => x.Id);
            var objectiveTypeById = ToFirstByKey(objectiveTypes, x => x.Id);
            var institutionById = ToFirstByKey(institutions, x => x.Id);

            var productAttributeById = ToFirstByKey(productAttributes, x => x.Id);
            var attributeDefById = ToFirstByKey(attributeDefs, x => x.Id);

            // Diccionario rápido de roles (seguro ante duplicados)
            var memberRoleDict = ToFirstByKey(memberRoleTypes, r => r.Id);

            // Grupos tipo 2 = grupos de investigación (acreditados SENESCYT)
            var researchGroupIds = groups
                .Where(g => g.GroupTypeId == GroupTypeIds.Investigadores)
                .Select(g => g.GroupId)
                .ToHashSet();

            // Todos los IdUser que pertenecen a grupos tipo 2 (investigadores SENESCYT)
            var senescytUserIds = groupMembers
                .Where(m => researchGroupIds.Contains(m.GroupId))
                .Select(m => m.UserId)
                .Distinct()
                .ToHashSet();

            // Lookup de miembros por grupo
            var membersByGroupId = groupMembers
                .GroupBy(m => m.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Lookup de AppUser por IdUser (el que está en GroupMember.UserId)
            var appUserByIdUser = appUsers
                .GroupBy(a => a.IdUser)
                .ToDictionary(g => g.Key, g => g.First());

            // Agrupaciones por ProjectId/ProductId para evitar filtros repetidos
            var budgetsByProjectId = budgets
                .GroupBy(b => b.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var productsByProjectId = products
                .Where(p => p.ProjectId.HasValue)
                .GroupBy(p => p.ProjectId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var valuesByProductId = productValues
                .GroupBy(v => v.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var objectivesByProjectId = projectObjectives
                .GroupBy(o => o.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var researchCatsByProjectId = projectResearchCats
                .GroupBy(rc => rc.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var externalResearcherProjectsByProjectId = externalResearcherProj
                .GroupBy(er => er.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // =========================
            //   DIRECTORIO EXTERNO
            // =========================
            // Se indexa por ASP_ID (IdAsp)
            var profileByAspId = await LoadExternalProfilesAsync(ct);

            // =========================
            //          PROJECTION
            // =========================
            var report = projects
                .Select(p =>
                {
                    groupById.TryGetValue(p.ProjectGroupId, out var group);
                    groupTypeById.TryGetValue(group?.GroupTypeId ?? 0, out var groupType);

                    // Project.FacultyId and this dictionary both use local keys.
                    facultyDict.TryGetValue(p.FacultyId, out var facultyDto);
                    var facultyName = facultyDto?.Name;

                    // =========================
                    //      PROJECT MEMBERS
                    // =========================
                    membersByGroupId.TryGetValue(p.ProjectGroupId, out var rawMembers);
                    rawMembers ??= new List<tesisproject.backend.Data.UnifiedEntities.Core.GroupMember>();

                    var internalMembers = rawMembers
                        .Select(m =>
                        {
                            memberRoleDict.TryGetValue(m.MemberRoleId, out var role);

                            var userId = m.UserId;

                            ExternalUserProfileModel? profile = null;

                            if (appUserByIdUser.TryGetValue(userId, out var appUser) &&
                                appUser.IdAsp is int aspId &&
                                profileByAspId.TryGetValue(aspId, out var foundProfile))
                            {
                                profile = foundProfile;
                            }

                            return new ProjectMemberReportDTO
                            {
                                UserId = userId,
                                MemberRoleId = m.MemberRoleId,
                                MemberRoleName = role?.Name,
                                FullName = profile is null ? null : GetProfileName(profile),
                                Email = profile is null ? null : GetProfileEmail(profile),
                                PhoneNumber = profile is null ? null : GetProfilePhone(profile)
                            };
                        })
                        .ToList();

                    var coordinatorPrincipal = internalMembers
                        .FirstOrDefault(m => m.MemberRoleId == MemberRoleTypeIds.Coordinador);

                    var coordinatorSubrogant = internalMembers
                        .FirstOrDefault(m => m.MemberRoleId == MemberRoleTypeIds.Subrogante);

                    var senescytMembers = internalMembers
                        .Where(m => senescytUserIds.Contains(m.UserId))
                        .ToList();

                    // =========================
                    //        PROJECT DTO
                    // =========================
                    projectTypeById.TryGetValue(p.ProjectTypeId, out var projectType);
                    projectStateById.TryGetValue(p.ProjectStateId, out var projectState);
                    convocationById.TryGetValue(p.ConvocationId.GetValueOrDefault(), out var convocation);

                    var projectBudgets = budgetsByProjectId.TryGetValue(p.ProjectId, out var bList)
                        ? bList
                        : new List<tesisproject.backend.Data.UnifiedEntities.Core.Budget>();

                    var projectProducts = productsByProjectId.TryGetValue(p.ProjectId, out var prList)
                        ? prList
                        : new List<Product>();

                    var projectObjectivesList = objectivesByProjectId.TryGetValue(p.ProjectId, out var objList)
                        ? objList
                        : new List<tesisproject.backend.Data.UnifiedEntities.Core.ProjectObjective>();

                    var projectResearchCategories = researchCatsByProjectId.TryGetValue(p.ProjectId, out var rcList)
                        ? rcList
                        : new List<tesisproject.backend.Data.UnifiedEntities.Core.ProjectResearchCategory>();

                    var projectExternalResearchers = externalResearcherProjectsByProjectId.TryGetValue(p.ProjectId, out var erList)
                        ? erList
                        : new List<tesisproject.backend.Data.UnifiedEntities.Core.ExternalResearcherProject>();

                    return new ProjectFlatReportDTO
                    {
                        // =========================
                        //      PROJECT GRAIN
                        // =========================
                        ProjectId = p.ProjectId,
                        ProjectCode = p.ProjectCode,
                        ProjectName = p.ProjectName,
                        ProjectNumber = p.ProjectNumber,
                        ProjectTypeId = p.ProjectTypeId,
                        ProjectTypeName = projectType?.Name,
                        ProjectStateId = p.ProjectStateId,
                        ProjectStateName = projectState?.Name,
                        ProjectGroupId = p.ProjectGroupId,
                        GroupName = group?.Name,
                        GroupTypeName = groupType?.Name,
                        ConvocationId = p.ConvocationId.GetValueOrDefault(),
                        ConvocationName = convocation?.Name,
                        ApprovalDate = p.ApprovalDate,
                        StartDate = p.StartDate,
                        DurationInMonths = p.DurationInMonths,
                        TentativeEndDate = p.TentativeEndDate,
                        RealEndDate = p.RealEndDate,
                        ExecutionPercentage = p.ExecutionPercentage,
                        FacultyId = p.FacultyId,
                        FacultyName = facultyName,

                        // =========================
                        //          BUDGETS
                        // =========================
                        Budgets = projectBudgets
                            .Select(b =>
                            {
                                fundingTypeById.TryGetValue(b.FundingTypeId, out var fundingType);

                                return new ProjectBudgetReportDTO
                                {
                                    BudgetId = b.BudgetId,
                                    FundingTypeId = b.FundingTypeId,
                                    FundingTypeName = fundingType?.Name,
                                    InitialAmount = b.InitialAmount,
                                    CertifiedAmount = b.CertifiedAmount,
                                    ExecutedAmount = b.ExecutedAmount,
                                    ApprovedAt = b.ApprovedAt,
                                };
                            })
                            .ToList(),

                        // =========================
                        //          PRODUCTS
                        // =========================
                        Products = projectProducts
                            .Select(pr =>
                            {
                                productTypeById.TryGetValue(pr.ProductTypeId, out var prodType);

                                var valuesForProduct = valuesByProductId.TryGetValue(pr.Id, out var vList)
                                    ? vList
                                    : new List<ProductValue>();

                                return new ProjectProductReportDTO
                                {
                                    ProductId = pr.Id,
                                    Title = pr.Title,
                                    Description = pr.Description,
                                    ProductTypeId = pr.ProductTypeId,
                                    ProductTypeName = prodType?.Name,
                                    CreatedAt = pr.CreatedAt,
                                    UpdatedAt = pr.UpdatedAt,
                                    Attributes = valuesForProduct
                                        .Select(v =>
                                        {
                                            attributeDefById.TryGetValue(v.AttributeDefinitionId, out var def);

                                            var attr = def is null
                                                ? null
                                                : productAttributeById.TryGetValue(def.ProductAttributeId, out var foundAttr)
                                                    ? foundAttr
                                                    : null;

                                            return new ProductAttributeValueDTO
                                            {
                                                AttributeDefinitionId = v.AttributeDefinitionId,
                                                ProductAttributeId = def?.ProductAttributeId ?? 0,
                                                AttributeName = attr?.Name,
                                                Value = v.Value,
                                                Unit = attr?.Unit,
                                                DataType = attr?.DataType.ToString()
                                            };
                                        })
                                        .ToList()
                                };
                            })
                            .ToList(),

                        // =========================
                        //        OBJECTIVES
                        // =========================
                        Objectives = projectObjectivesList
                            .Select(o =>
                            {
                                objectiveTypeById.TryGetValue(o.ObjectiveTypeId, out var objType);

                                return new ProjectObjectiveReportDTO
                                {
                                    ObjectiveId = o.Id,
                                    ObjectiveTypeId = o.ObjectiveTypeId,
                                    ObjectiveTypeName = objType?.Name,
                                    Objective = o.Objective,
                                    Result = o.Result,
                                    WeightedPercentage = o.WeightedPercentage,
                                };
                            })
                            .ToList(),

                        // =========================
                        //    RESEARCH CATEGORIES
                        // =========================
                        ResearchCategories = projectResearchCategories
                            .Select(rc => new ProjectResearchCategoryReportDTO
                            {
                                ResearchCategoryId = rc.ResearchCategoryId
                            })
                            .ToList(),

                        // =========================
                        //    EXTERNAL RESEARCHERS
                        // =========================
                        ExternalResearchers = projectExternalResearchers
                            .Select(er =>
                            {
                                var person = externalResearchers
                                    .FirstOrDefault(x => x.ExternalResearcherId == er.ExternalResearcherId);

                                var inst = institutionById.TryGetValue(person?.InstitutionId ?? 0, out var foundInst)
                                    ? foundInst
                                    : null;

                                return new ProjectExternalResearcherReportDTO
                                {
                                    ExternalResearcherId = er.ExternalResearcherId,
                                    FullName = person?.FullName ?? string.Empty,
                                    Email = person?.Email ?? string.Empty,
                                    PhoneNumber = person?.PhoneNumber,
                                    InstitutionId = person?.InstitutionId,
                                    InstitutionName = inst?.Name,
                                    Role = er.Role,
                                    CreatedAtUtc = er.CreatedAtUtc,
                                    ExitDate = er.ExitDate
                                };
                            })
                            .ToList(),

                        // =========================
                        //      INTERNAL MEMBERS
                        // =========================
                        InternalMembers = internalMembers,
                        SenescytMembers = senescytMembers,
                        CoordinatorName = coordinatorPrincipal?.FullName,
                        CoordinatorEmail = coordinatorPrincipal?.Email,
                        CoordinatorPhone = coordinatorPrincipal?.PhoneNumber,

                        SubrogantName = coordinatorSubrogant?.FullName,
                        SubrogantEmail = coordinatorSubrogant?.Email,
                        SubrogantPhone = coordinatorSubrogant?.PhoneNumber
                    };
                })
                .ToList();

            return ServiceResult<IReadOnlyList<ProjectFlatReportDTO>>.Ok(report);
        }

        // ======================================================
        //      Cargas externas (mantener logging EXACTO)
        // ======================================================



        private async Task<Dictionary<int, ExternalUserProfileModel>> LoadExternalProfilesAsync(CancellationToken ct)
        {
            var profileByAspId = new Dictionary<int, ExternalUserProfileModel>();

            try
            {
                var profilesRes = await _externalDirectory.GetAllAsync(ct);
                if (profilesRes.Success && profilesRes.Data is not null)
                {
                    profileByAspId = profilesRes.Data
                        .Where(p => p.AspId.HasValue)
                        .GroupBy(p => GetProfileKey(p))
                        .ToDictionary(g => g.Key, g => g.First());

                    _logger.LogInformation(
                        "Loaded {Count} external profiles from directory.",
                        profileByAspId.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not load external profiles from directory. Members will not be enriched.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading external profiles from directory.");
            }

            return profileByAspId;
        }

        // ======================================================
        //      Mapeos auxiliares para ExternalProfileDTO
        // ======================================================

        /// <summary>
        /// Devuelve la clave (ASP_ID) del perfil externo.
        /// </summary>
        private static int GetProfileKey(ExternalUserProfileModel profile)
        {
            // ASP_ID viene como long? / int? en tu DTO
            return (int)profile.AspId!;
        }

        private static string? GetProfileName(ExternalUserProfileModel profile)
            => profile.FullName;

        private static string? GetProfileEmail(ExternalUserProfileModel profile)
            => profile.Email;

        private static string? GetProfilePhone(ExternalUserProfileModel profile)
            => profile.Phone;

        // ======================================================
        //      Helpers de diccionarios (preservan "First")
        // ======================================================

        private static Dictionary<TKey, TSource> ToFirstByKey<TSource, TKey>(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            where TKey : notnull
        {
            return source
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.First());
        }
    }
}
