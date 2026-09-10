using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

var assertions = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    assertions++;
}

foreach (var request in new[]
{
    new UnifiedAuthorWriteRequest(null, null), new UnifiedAuthorWriteRequest(41, 82),
    new UnifiedAuthorWriteRequest(0, null), new UnifiedAuthorWriteRequest(null, -1),
    new UnifiedAuthorWriteRequest(41, null, new string('x', 51))
})
{
    var f = new Fixture();
    var result = await f.Service.CreateAsync(request);
    Check(!result.Success && result.Error == ErrorType.Validation && result.ErrorCode is not null, "Invalid source/ORCID must fail validation.");
    Check(result.ValidationErrors?.Count > 0 && f.Saves == 0 && f.Authors.Count == 0, "Validation must not stage or commit an Author.");
}
{
    var f = new Fixture();
    var result = await f.Service.CreateAsync(null!);
    Check(result.ErrorCode == ErrorCodes.Common.RequestRequired && f.Saves == 0, "Null request.");
    result = await f.Service.CreateAsync(new(999, null));
    Check(result.ErrorCode == ErrorCodes.AppUser.NotFound && f.Saves == 0, "Missing AppUser must not be provisioned.");
    result = await f.Service.CreateAsync(new(null, 999));
    Check(result.ErrorCode == ErrorCodes.ExternalResearcher.NotFound && f.Saves == 0, "Missing researcher.");
}
{
    var f = new Fixture();
    using var cts = new CancellationTokenSource();
    f.ExpectedToken = cts.Token;
    var result = await f.Service.CreateAsync(new(41, null, "  0000-0001  "), cts.Token);
    Check(result.Success && result.Data is { AuthorId: 901, AppUserId: 41, ExternalResearcherId: null, Orcid: "0000-0001" }, "AuthorId must differ from institutional source ID.");
    Check(f.Saves == 1 && f.Authors.Single().ExternalAuthorId is null, "Create must save once and ignore legacy Articles identifiers.");
    var duplicate = await f.Service.CreateAsync(new(41, null), cts.Token);
    Check(duplicate.ErrorCode == ErrorCodes.Author.SourceAlreadyAssigned && f.Saves == 1, "Duplicate source.");
    var duplicateOrcid = await f.Service.CreateAsync(new(null, 82, "0000-0001"), cts.Token);
    Check(duplicateOrcid.ErrorCode == ErrorCodes.Author.OrcidAlreadyExists && f.Saves == 1, "Duplicate ORCID.");
    var lookup = await f.Service.GetByAppUserIdAsync(41, cts.Token);
    Check(lookup.Data?.AuthorId == 901 && f.Saves == 1, "Lookup resolves rather than casts source ID.");
    var list = await f.Service.GetAllAsync(cts.Token);
    Check(list.Data?.Count == 1 && f.Saves == 1, "Read operations never save.");
    f.Authors[0].ExternalAuthorId = "legacy-articles";
    var invalidUpdate = await f.Service.UpdateAsync(901, new(41, 82), cts.Token);
    Check(!invalidUpdate.Success && f.Authors[0].AppUserId == 41 && f.Saves == 1, "Invalid update leaves entity unchanged.");
    var update = await f.Service.UpdateAsync(901, new(null, 82, ""), cts.Token);
    Check(update.Success && update.Data is { AuthorId: 901, AppUserId: null, ExternalResearcherId: 82, Orcid: null } && f.Saves == 2, "Update validates the alternative source and saves once.");
    Check(f.Authors[0].ExternalAuthorId == "legacy-articles", "Update preserves legacy compatibility column.");
    lookup = await f.Service.GetByExternalResearcherIdAsync(82, cts.Token);
    Check(lookup.Data?.AuthorId == 901, "External lookup resolves AuthorId.");
    f.Linked = true;
    var delete = await f.Service.DeleteAsync(901, cts.Token);
    Check(delete.ErrorCode == ErrorCodes.Author.InUse && f.Saves == 2 && f.Authors.Count == 1, "Linked author deletion must fail without commit.");
    f.Linked = false;
    delete = await f.Service.DeleteAsync(901, cts.Token);
    Check(delete.Success && f.Saves == 3 && f.Authors.Count == 0, "Delete owns one commit.");
    Check((await f.Service.GetByIdAsync(901, cts.Token)).ErrorCode == ErrorCodes.Author.NotFound, "Deleted author is not found.");
}
{
    var f = new Fixture { SaveException = new DbUpdateException("unique race") };
    var result = await f.Service.CreateAsync(new(null, 82));
    Check(result.Error == ErrorType.Conflict && result.ErrorCode == ErrorCodes.Common.PersistenceConflict && f.Saves == 1, "Database races surface as persistence conflicts.");
}
{
    var f = new Fixture { SaveException = new OperationCanceledException() };
    var result = await f.Service.CreateAsync(new(41, null));
    Check(result.ErrorCode == ErrorCodes.Common.OperationCanceled, "Cancellation follows shared error contract.");
}

{
    var users = Stub.For<IUnifiedAppUserRepository>((method, args) =>
    {
        if (method.Name != "GetByLocalIdAsync" || (int)args[0]! != 19)
            throw new InvalidOperationException("Expected lookup by IdLocal, not IdUser.");
        return Task.FromResult<AppUser?>(new AppUser { IdLocal = 19, IdUser = 41, IdAsp = 77 });
    });
    var uow = Stub.For<IUnifiedUnitOfWork>((method, _) => method.Name == "get_AppUsers"
        ? users : throw new InvalidOperationException("A read must not commit or provision Identity."));
    var service = new UnifiedAppUserService(uow, AcademicFixture.NoProvisioning);
    var result = await service.GetAppUserIdByLocalIdAsync(19);
    Check(result.Success && result.Data == 41 && result.Message == "App user resolved.", "Preserve IdLocal -> IdUser lookup and legacy response.");
    Check((await service.GetAppUserIdByLocalIdAsync(0)).ErrorCode == ErrorCodes.AppUser.InvalidLocalUserId, "Reject invalid local ID before persistence access.");
}

// Inspect compiled type dependencies, including fields: no database connection or runtime DI changes.
var assembly = typeof(UnifiedAuthorService).Assembly;
var classes = assembly.GetTypes().Where(t => t.Namespace == "tesisproject.backend.Services.Unified.Implementations" && t.IsClass);
foreach (var type in classes)
{
    var dependencies = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(f => f.FieldType)
        .Concat(type.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType));
    foreach (var dependency in dependencies)
    {
        var signature = dependency.ToString();
        Check(!signature.Contains("AppDbContext") && !signature.Contains("ArticlesDbContext") &&
            !signature.Contains("UnitOfWork.Interfaces.IUnitOfWork") && !signature.Contains("Repositories.Interfaces"),
            $"Legacy persistence dependency: {type.Name} -> {signature}");
    }
}
Check(typeof(IUnifiedUnitOfWork).GetProperties().Length == 58, "Unified UoW contract includes 58 repositories with Articles.");
Check(typeof(IUnifiedProductService).GetMethod("CreateAsync") is not null && typeof(IUnifiedProductService).GetMethod("GetByIdAsync") is not null,
    "Product author-dependent methods are now operational.");
await AtomicityTests.RunAsync(Check);
await ProductTests.RunAsync(Check);
await AcademicReferenceTests.RunAsync(Check);
await IdentityProvisioningTests.RunAsync(Check);
await UnifiedControllerTests.RunAsync(Check);
await UnifiedRequestAdapterTests.RunAsync(Check);
await CatalogSynchronizationTests.RunAsync(Check);
await FacultyHierarchyTests.RunAsync(Check);
await LocalCatalogConsumerTests.RunAsync(Check);
Console.WriteLine($"PASS: {assertions} assertions; Author CRUD, distinct IDs, errors, commit boundaries, compiled dependency isolation. No database connection.");

public class Stub : DispatchProxy
{
    public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args ?? []);
    public static T For<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        var instance = Create<T, Stub>();
        ((Stub)(object)instance).Handler = handler;
        return instance;
    }
}

public sealed class Fixture
{
    public List<Author> Authors { get; } = [];
    public int Saves { get; private set; }
    public bool Linked { get; set; }
    public Exception? SaveException { get; set; }
    public CancellationToken ExpectedToken { get; set; }
    public UnifiedAuthorService Service { get; }

    public Fixture()
    {
        var authors = Stub.For<IUnifiedAuthorRepository>((method, args) =>
        {
            Token(args);
            switch (method.Name)
            {
                case "GetByIdAsync": return Task.FromResult(Authors.FirstOrDefault(x => x.AuthorId == (int)((object[])args[0]!)[0]));
                case "GetByAppUserIdAsync": return Task.FromResult(Authors.FirstOrDefault(x => x.AppUserId == (int)args[0]!));
                case "GetByExternalResearcherIdAsync": return Task.FromResult(Authors.FirstOrDefault(x => x.ExternalResearcherId == (int)args[0]!));
                case "GetAllAsync": return Task.FromResult(Authors.ToList());
                case "ExistsAsync": return Task.FromResult(Authors.Any(((Expression<Func<Author, bool>>)args[0]!).Compile()));
                case "AddAsync": Authors.Add((Author)args[0]!); return Task.CompletedTask;
                case "Update": return null;
                case "Remove": Authors.Remove((Author)args[0]!); return null;
                default: throw new InvalidOperationException(method.Name);
            }
        });
        var users = Stub.For<IUnifiedAppUserRepository>((method, args) =>
        {
            Token(args);
            if (method.Name != "GetByIdAsync") throw new InvalidOperationException(method.Name);
            return Task.FromResult((int)((object[])args[0]!)[0] == 41 ? new AppUser { IdUser = 41 } : null);
        });
        var researchers = Stub.For<IUnifiedExternalResearcherRepository>((method, args) =>
        {
            Token(args);
            if (method.Name != "GetByIdAsync") throw new InvalidOperationException(method.Name);
            return Task.FromResult((int)((object[])args[0]!)[0] == 82 ? new ExternalResearcher { ExternalResearcherId = 82 } : null);
        });
        var links = Stub.For<IUnifiedProductAuthorRepository>((method, args) =>
        {
            Token(args);
            if (method.Name != "ExistsAsync") throw new InvalidOperationException(method.Name);
            return Task.FromResult(Linked);
        });
        var uow = Stub.For<IUnifiedUnitOfWork>((method, args) =>
        {
            Token(args);
            switch (method.Name)
            {
                case "get_Authors": return authors;
                case "get_AppUsers": return users;
                case "get_ExternalResearchers": return researchers;
                case "get_ProductAuthors": return links;
                case "SaveChangesAsync":
                    Saves++;
                    if (SaveException is not null) throw SaveException;
                    foreach (var author in Authors.Where(x => x.AuthorId == 0)) author.AuthorId = 901;
                    return Task.FromResult(1);
                default: throw new InvalidOperationException("Unexpected UoW dependency: " + method.Name);
            }
        });
        Service = new(uow);
    }

    private void Token(object?[] args)
    {
        foreach (var token in args.OfType<CancellationToken>())
            if (token != ExpectedToken) throw new InvalidOperationException("CancellationToken was not forwarded.");
    }
}
