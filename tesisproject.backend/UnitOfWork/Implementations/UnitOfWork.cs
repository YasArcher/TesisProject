using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.UnitOfWork.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _ctx;
        public IProjectRepository Projects { get; }
        public IGroupRepository Groups { get; }
        public IGroupMemberRepository GroupMembers { get; }
        public IBudgetRepository Budgets { get; }
        public IVisitRepository Visits { get; }
        public IProjectExtensionRepository ProjectExtensions { get; set; }
        public IAspNetUserRepository AspNetUsers { get; set; }
        public IVisitIssueRepository VisitIssues { get; set; }
        public IConvocationRepository Convocations { get; set; }
        public IProductRepository Products { get; set; }
        public IProductTypeRepository ProductTypes { get; set; }
        public IProductAttributeDefinitionRepository ProductAttributeDefinitions { get; set; }
        public IProductValueRepository ProductValues { get; set; }
        public IProductAuthorRepository ProductAuthors { get; set; }
        public ICatalogRepository<ObjectiveType> ObjectiveTypes { get; set; }
        public IProjectObjectiveRepository ProjectObjectives { get; set; }
        public IObjectiveActivityRepository ObjectiveActivities { get; set; }
        public IObjectiveActivityUserRepository ObjectiveActivityUsers { get; set; }
        public IDocumentRepository Documents { get; }
        public ICatalogRepository<MemberRoleType> MemberRoleTypeRepository { get; }
        public ICatalogRepository<ProjectType> ProjectTypeRepository { get; set; }
        public ICatalogRepository<DocumentType> DocumentTypes { get; set; }
        public IProjectResearchCategoryRepository ProjectResearchCategories { get; }
        public IResearchCategoryRepository ResearchCategories { get; }
        public ICatalogRepository<ResearchCategoryType> ResearchCategoryTypes { get; }
        public ICatalogRepository<FundingType> FundingTypes { get; }
        public ICatalogRepository<ResearchCategoryGroup> ResearchCategoryGroups { get; set; }
        public IAppUserRepository AppUsers { get; }
        public ICatalogRepository<TransactionType> TransactionTypes { get; }
        public IExternalResearcherRepository ExternalResearchers { get; }
        public ICatalogRepository<Country> Countries { get; }
        public ICatalogRepository<Institution> Institutions { get; }
        public IExternalResearcherProjectRepository ExternalResearcherProjects { get; }
        public UnitOfWork(
            AppDbContext ctx,
            IProjectRepository projectRepository,
            IGroupRepository groupRepository,
            IGroupMemberRepository groupMemberRepository,
            IBudgetRepository budgets,
            IVisitRepository visitRepository,
            IProjectExtensionRepository projectExtensions,
            IAspNetUserRepository aspNetUserRepository,
            IVisitIssueRepository visitIssues,
            IConvocationRepository convocations,
            IProductRepository products,
            IProductTypeRepository productTypes,
            IProductAttributeDefinitionRepository productAttributeDefinitions,
            IProductValueRepository productValues,
            IProductAuthorRepository productAuthors,
            ICatalogRepository<ObjectiveType> objectiveTypes,
            IProjectObjectiveRepository projectObjectives,
            IObjectiveActivityRepository objectiveActivities,
            IObjectiveActivityUserRepository objectiveActivityUsers,
            IDocumentRepository documents,
            ICatalogRepository<MemberRoleType> memberRoleTypeRepository,
            ICatalogRepository<ProjectType> projectTypeRepository,
            ICatalogRepository<DocumentType> documentTypes,
            IProjectResearchCategoryRepository projectResearchCategories,
            IResearchCategoryRepository researchCategories,
            ICatalogRepository<ResearchCategoryType> researchCategoryTypes,
            ICatalogRepository<FundingType> fundingTypes,
            ICatalogRepository<ResearchCategoryGroup> researchCategoryGroups,
            IAppUserRepository appUsers,
            ICatalogRepository<TransactionType> transactionTypes,
            IExternalResearcherRepository externalResearchers,
            ICatalogRepository<Country> countries,
            ICatalogRepository<Institution> institutions,
            IExternalResearcherProjectRepository externalResearcherProjects)
        {
            _ctx = ctx;
            Projects = projectRepository;
            Groups = groupRepository;
            GroupMembers = groupMemberRepository;
            Budgets = budgets;
            Visits = visitRepository;
            ProjectExtensions = projectExtensions;
            AspNetUsers = aspNetUserRepository;
            VisitIssues = visitIssues;
            Convocations = convocations;
            Products = products;
            ProductTypes = productTypes;
            ProductAttributeDefinitions = productAttributeDefinitions;
            ProductValues = productValues;
            ProductAuthors = productAuthors;
            ObjectiveTypes = objectiveTypes;
            ProjectObjectives = projectObjectives;
            ObjectiveActivities = objectiveActivities;
            ObjectiveActivityUsers = objectiveActivityUsers;
            Documents = documents;
            MemberRoleTypeRepository = memberRoleTypeRepository;
            ProjectTypeRepository = projectTypeRepository;
            DocumentTypes = documentTypes;
            ProjectResearchCategories = projectResearchCategories;
            ResearchCategories = researchCategories;
            ResearchCategoryTypes = researchCategoryTypes;
            FundingTypes = fundingTypes;
            ResearchCategoryGroups = researchCategoryGroups;
            AppUsers = appUsers;
            TransactionTypes = transactionTypes;
            ExternalResearchers = externalResearchers;
            Countries = countries;
            Institutions = institutions;
            ExternalResearcherProjects = externalResearcherProjects;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _ctx.SaveChangesAsync(ct);

        public ValueTask DisposeAsync() => _ctx.DisposeAsync();
    }
}
