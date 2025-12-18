using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectFlatReportService : IProjectFlatReportService
    {
        private readonly AppDbContext _db;
        private readonly IExternalAcademicsService _externalAcademics;
        private readonly IExternalDirectoryClient _externalDirectory;
        private readonly ILogger<ProjectFlatReportService> _logger;

        public ProjectFlatReportService(
            AppDbContext db,
            IExternalAcademicsService externalAcademics,
            IExternalDirectoryClient externalDirectory,
            ILogger<ProjectFlatReportService> logger)
        {
            _db = db;
            _externalAcademics = externalAcademics;
            _externalDirectory = externalDirectory;
            _logger = logger;
        }

        public async Task<ServiceResult<IReadOnlyList<ProjectFlatReportDTO>>> GetFlatReportAsync(
            IEnumerable<int>? projectIds = null,
            CancellationToken ct = default)
        {
            // =========================
            //   EXTERNAL FACULTIES
            // =========================
            var facultyDict = new Dictionary<int, ExternalFacultyDTO>();

            try
            {
                var facultiesRes = await _externalAcademics.GetFacultiesAsync(ct);
                if (facultiesRes.Success && facultiesRes.Data is not null)
                {
                    facultyDict = facultiesRes.Data
                        .GroupBy(f => f.FacultyId) // por si acaso
                        .ToDictionary(g => g.Key, g => g.First());

                    _logger.LogInformation(
                        "Loaded {Count} faculties from external API for flat report.",
                        facultyDict.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not load faculties from external API. Report will not include FacultyName.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error loading faculties from external API. FacultyName will be empty.");
            }

            // =========================
            //      CATALOG TABLES
            // =========================
            var projectTypes = await _db.ProjectTypes.AsNoTracking().ToListAsync(ct);
            var projectStates = await _db.ProjectStates.AsNoTracking().ToListAsync(ct);
            var convocationList = await _db.Convocations.AsNoTracking().ToListAsync(ct);
            var groups = await _db.Groups.AsNoTracking().ToListAsync(ct);
            var groupTypes = await _db.GroupTypes.AsNoTracking().ToListAsync(ct);
            var fundingTypes = await _db.FundingTypes.AsNoTracking().ToListAsync(ct);
            var productTypes = await _db.ProductTypes.AsNoTracking().ToListAsync(ct);
            var objectiveTypes = await _db.ObjectiveTypes.AsNoTracking().ToListAsync(ct);
            var institutions = await _db.Institutions.AsNoTracking().ToListAsync(ct);
            var productAttributes = await _db.ProductAttributes.AsNoTracking().ToListAsync(ct);
            var attributeDefs = await _db.ProductAttributeDefinitions.AsNoTracking().ToListAsync(ct);

            // =========================
            //      OPERATIONAL TABLES
            // =========================

            var projectQuery = _db.Projects.AsNoTracking().AsQueryable();

            if (projectIds != null && projectIds.Any())
            {
                projectQuery = projectQuery.Where(p => projectIds.Contains(p.ProjectId));
            }
            _logger.LogInformation("Generating flat report for {Count} projects.",projectIds?.Count() ?? await _db.Projects.CountAsync(ct));
            var projects = await projectQuery.ToListAsync(ct);
            var budgets = await _db.Budgets.AsNoTracking().ToListAsync(ct);
            var products = await _db.Products.AsNoTracking().ToListAsync(ct);
            var productValues = await _db.ProductValues.AsNoTracking().ToListAsync(ct);
            var projectObjectives = await _db.ProjectObjectives.AsNoTracking().ToListAsync(ct);
            var projectResearchCats = await _db.ProjectResearchCategories.AsNoTracking().ToListAsync(ct);
            var externalResearchers = await _db.ExternalResearchers.AsNoTracking().ToListAsync(ct);
            var externalResearcherProj = await _db.ExternalResearcherProjects.AsNoTracking().ToListAsync(ct);
            // Scopes y Visits NO se usan en el flat report actual

            // 🔹 NUEVO: integrantes de grupos y roles
            var groupMembers = await _db.GroupMembers.AsNoTracking().ToListAsync(ct);
            var memberRoleTypes = await _db.MemberRoleTypes.AsNoTracking().ToListAsync(ct);

            // 🔹 NUEVO: tabla puente de usuarios (IdUser -> IdAsp)
            var appUsers = await _db.AppUsers.AsNoTracking().ToListAsync(ct);

            // =========================
            //   LOOKUPS AUXILIARES
            // =========================

            // Diccionario rápido de roles
            var memberRoleDict = memberRoleTypes.ToDictionary(r => r.Id, r => r);

            // Grupos tipo 1 = integrantes de proyecto
            const int PROJECT_MEMBER_GROUP_TYPE_ID = 1;

            // Grupos tipo 2 = grupos de investigación (acreditados SENESCYT)
            const int RESEARCH_GROUP_GROUP_TYPE_ID = 2;

            var researchGroupIds = groups
                .Where(g => g.GroupTypeId == RESEARCH_GROUP_GROUP_TYPE_ID)
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

            // =========================
            //   DIRECTORIO EXTERNO
            // =========================
            // Se indexa por ASP_ID (IdAsp)
            var profileByAspId = new Dictionary<int, ExternalProfileDTO>();

            try
            {
                var profilesRes = await _externalDirectory.GetAllAsync(ct);
                if (profilesRes.Success && profilesRes.Data is not null)
                {
                    profileByAspId = profilesRes.Data
                        .Where(p => p.ASP_ID.HasValue)
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

            // =========================
            //          PROJECTION
            // =========================
            var report = projects
                .Select(p =>
                {
                    var group = groups.FirstOrDefault(g => g.GroupId == p.ProjectGroupId);
                    var groupType = groupTypes.FirstOrDefault(gt => gt.Id == (group?.GroupTypeId ?? 0));

                    // Resolver facultad (externa)
                    facultyDict.TryGetValue(p.FacultyId, out var facultyDto);
                    var facultyName = facultyDto?.Name;

                    // =========================
                    //      PROJECT MEMBERS
                    // =========================

                    // Miembros del grupo asociado al proyecto (ProjectGroupId)
                    membersByGroupId.TryGetValue(p.ProjectGroupId, out var rawMembers);
                    rawMembers ??= new List<shared.Entities.Core.GroupMember>();

                    // Mapear a DTO enriquecido con directorio externo
                    var internalMembers = rawMembers
                        .Select(m =>
                        {
                            memberRoleDict.TryGetValue(m.MemberRoleId, out var role);

                            // IdUser (interno) que viene en GroupMember
                            var userId = m.UserId;

                            ExternalProfileDTO? profile = null;

                            // Buscar AppUser para obtener IdAsp
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

                    // Coordinador y Subrogante separados (sin fallback)
                    const int ROLE_COORDINADOR_PRINCIPAL_ID = 1;
                    const int ROLE_COORDINADOR_SUBROGANTE_ID = 2;

                    var coordinatorPrincipal = internalMembers
                        .FirstOrDefault(m => m.MemberRoleId == ROLE_COORDINADOR_PRINCIPAL_ID);

                    var coordinatorSubrogant = internalMembers
                        .FirstOrDefault(m => m.MemberRoleId == ROLE_COORDINADOR_SUBROGANTE_ID);


                    // Investigadores SENESCYT = integrantes del proyecto cuyo IdUser
                    // pertenece a algún grupo tipo 2 (researchGroupIds)
                    var senescytMembers = internalMembers
                        .Where(m => senescytUserIds.Contains(m.UserId))
                        .ToList();

                    // =========================
                    //        PROJECT DTO
                    // =========================
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
                        ProjectTypeName = projectTypes.FirstOrDefault(t => t.Id == p.ProjectTypeId)?.Name,
                        ProjectStateId = p.ProjectStateId,
                        ProjectStateName = projectStates.FirstOrDefault(s => s.Id == p.ProjectStateId)?.Name,
                        ProjectGroupId = p.ProjectGroupId,
                        GroupName = group?.Name,
                        GroupTypeName = groupType?.Name,
                        ConvocationId = (int)p.ConvocationId,
                        ConvocationName = convocationList.FirstOrDefault(c => c.Id == p.ConvocationId)?.Name,
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
                        Budgets = budgets
                            .Where(b => b.ProjectId == p.ProjectId)
                            .Select(b => new ProjectBudgetReportDTO
                            {
                                BudgetId = b.BudgetId,
                                FundingTypeId = b.FundingTypeId,
                                FundingTypeName = fundingTypes.FirstOrDefault(ft => ft.Id == b.FundingTypeId)?.Name,
                                InitialAmount = b.InitialAmount,
                                CertifiedAmount = b.CertifiedAmount,
                                ExecutedAmount = b.ExecutedAmount,
                                ApprovedAt = b.ApprovedAt,
                                // Transactions NO se proyectan
                            })
                            .ToList(),

                        // =========================
                        //          PRODUCTS
                        // =========================
                        Products = products
                            .Where(pr => pr.ProjectId == p.ProjectId)
                            .Select(pr => new ProjectProductReportDTO
                            {
                                ProductId = pr.Id,
                                Title = pr.Title,
                                Description = pr.Description,
                                ProductTypeId = pr.ProductTypeId,
                                ProductTypeName = productTypes.FirstOrDefault(pt => pt.Id == pr.ProductTypeId)?.Name,
                                CreatedAt = pr.CreatedAt,
                                UpdatedAt = pr.UpdatedAt,
                                Attributes = productValues
                                    .Where(v => v.ProductId == pr.Id)
                                    .Select(v =>
                                    {
                                        var def = attributeDefs.FirstOrDefault(d => d.Id == v.AttributeDefinitionId);
                                        var attr = def is null
                                            ? null
                                            : productAttributes.FirstOrDefault(a => a.Id == def.ProductAttributeId);

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
                            })
                            .ToList(),

                        // =========================
                        //        OBJECTIVES
                        // =========================
                        Objectives = projectObjectives
                            .Where(o => o.ProjectId == p.ProjectId)
                            .Select(o => new ProjectObjectiveReportDTO
                            {
                                ObjectiveId = o.Id,
                                ObjectiveTypeId = o.ObjectiveTypeId,
                                ObjectiveTypeName = objectiveTypes.FirstOrDefault(ot => ot.Id == o.ObjectiveTypeId)?.Name,
                                Objective = o.Objetive,
                                Result = o.Result,
                                WeightedPercentage = o.WeightedPercentage,
                                // Activities NO se proyectan
                            })
                            .ToList(),

                        // =========================
                        //    RESEARCH CATEGORIES
                        // =========================
                        ResearchCategories = projectResearchCats
                            .Where(rc => rc.ProjectId == p.ProjectId)
                            .Select(rc => new ProjectResearchCategoryReportDTO
                            {
                                ResearchCategoryId = rc.ResearchCategoryId
                            })
                            .ToList(),

                        // =========================
                        //    EXTERNAL RESEARCHERS
                        // =========================
                        ExternalResearchers = externalResearcherProj
                            .Where(er => er.ProjectId == p.ProjectId)
                            .Select(er =>
                            {
                                var person = externalResearchers
                                    .FirstOrDefault(x => x.ExternalResearcherId == er.ExternalResearcherId);
                                var inst = institutions
                                    .FirstOrDefault(i => i.Id == (person?.InstitutionId ?? 0));

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
        //      Mapeos auxiliares para ExternalProfileDTO
        // ======================================================

        /// <summary>
        /// Devuelve la clave (ASP_ID) del perfil externo.
        /// </summary>
        private static int GetProfileKey(ExternalProfileDTO profile)
        {
            // ASP_ID viene como long? / int? en tu DTO
            return (int)profile.ASP_ID!;
        }

        private static string? GetProfileName(ExternalProfileDTO profile)
            => profile.FullName;

        private static string? GetProfileEmail(ExternalProfileDTO profile)
            => profile.Email;

        private static string? GetProfilePhone(ExternalProfileDTO profile)
            => profile.Phone;
    }
}
