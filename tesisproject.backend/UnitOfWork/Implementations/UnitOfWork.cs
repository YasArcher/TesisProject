using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;

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
        public IObjectiveTypeRepository ObjectiveTypes { get; set; }
        public IProjectObjectiveRepository ProjectObjectives { get; set; }
        public IObjectiveActivityRepository ObjectiveActivities { get; set; }
        public IObjectiveActivityUserRepository ObjectiveActivityUsers { get; set; }
        public IDocumentRepository Documents { get; }


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
            IObjectiveTypeRepository objectiveTypes,
            IProjectObjectiveRepository projectObjectives,
            IObjectiveActivityRepository objectiveActivities,
            IObjectiveActivityUserRepository objectiveActivityUsers,
            IDocumentRepository documents)
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
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _ctx.SaveChangesAsync(ct);

        public ValueTask DisposeAsync() => _ctx.DisposeAsync();
    }
}
