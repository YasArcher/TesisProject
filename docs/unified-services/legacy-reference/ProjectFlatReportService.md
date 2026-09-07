# Referencia legacy — ProjectFlatReportService

## TODO UNIFIED: GetFlatReportAsync

Une FacultyId persistido con claves de API externa; resolver Facultad local/externa. Acceso AppDbContext/GroupTypes debe pasar por UoW cuando se acuerde la consulta.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectFlatReportService.cs:37-420`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<IReadOnlyList<ProjectFlatReportDTO>>> GetFlatReportAsync(
            IEnumerable<int>? projectIds = null,
            CancellationToken ct = default)
        {
            // =========================
            //   EXTERNAL FACULTIES
            // =========================
            var facultyDict = await LoadExternalFacultiesAsync(ct);

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

            _logger.LogInformation(
                "Generating flat report for {Count} projects.",
                projectIds?.Count() ?? await _db.Projects.CountAsync(ct));

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
                .GroupBy(p => p.ProjectId)
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

                    // Resolver facultad (externa)
                    facultyDict.TryGetValue(p.FacultyId, out var facultyDto);
                    var facultyName = facultyDto?.Name;

                    // =========================
                    //      PROJECT MEMBERS
                    // =========================
                    membersByGroupId.TryGetValue(p.ProjectGroupId, out var rawMembers);
                    rawMembers ??= new List<shared.Entities.Core.GroupMember>();

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
                        : new List<shared.Entities.Core.Budget>();

                    var projectProducts = productsByProjectId.TryGetValue(p.ProjectId, out var prList)
                        ? prList
                        : new List<Product>();

                    var projectObjectivesList = objectivesByProjectId.TryGetValue(p.ProjectId, out var objList)
                        ? objList
                        : new List<shared.Entities.Core.ProjectObjective>();

                    var projectResearchCategories = researchCatsByProjectId.TryGetValue(p.ProjectId, out var rcList)
                        ? rcList
                        : new List<shared.Entities.Core.ProjectResearchCategory>();

                    var projectExternalResearchers = externalResearcherProjectsByProjectId.TryGetValue(p.ProjectId, out var erList)
                        ? erList
                        : new List<shared.Entities.Core.ExternalResearcherProject>();

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
                        ConvocationId = (int)p.ConvocationId,
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
```
