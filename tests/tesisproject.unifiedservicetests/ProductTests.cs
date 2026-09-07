using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Products.Product.Request;
using tesisproject.shared.DTOs.Products.Product.Response;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;
using System.Text.Json;

internal static class ProductTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        foreach (var projectId in new int?[] { 73, null })
        foreach (var ids in new List<int>?[] { [41], [42], [41, 42, 43], [41, 41, 42, 42, 0, -1], [], null })
        {
            using var f = new ProductFixture();
            var result = await f.Service.CreateAsync(new() { ProjectId = projectId, ProductTypeId = 17, Title = " Title ", AuthorUserIds = ids });
            var expected = (ids ?? []).Where(x => x > 0).Distinct().ToList();
            check(result.Success && f.Saves == 1, "Product create succeeds with exactly one save: " + result.Message);
            check(result.Data!.ProjectId == projectId && result.Data.Title == "Title", "Create preserves nullable project and normalizes title.");
            check(f.ProjectReads == (projectId.HasValue ? 1 : 0), "Only supplied projects are validated.");
            check(result.Data.Authors.Count == expected.Count, "Distinct positive institutional users only.");
            check(f.NewAuthors == expected.Count(x => x != 41), "Create only missing institutional Authors.");
            foreach (var author in result.Data.Authors)
                check(author.AuthorId > 900 && author.AuthorId != author.AppUserId && author.UserId == author.AppUserId &&
                    author.AuthorType == ProductAuthorType.Institutional && author.ExternalResearcherId is null,
                    "Source and author identity remain distinct.");
            check(f.InvalidAddedPrincipals == 0 && f.RelationshipsValid, "Real EF tracker propagates temporary Product/Author keys without reinserting principals.");
            check(expected.Where(x => x != 41).All(x => f.AuthorReads[x] == 2), "Missing Author is rechecked before being created.");
        }
        {
            using var f = new ProductFixture { AuthorAppearsOnRecheck = true };
            var result = await f.Service.CreateAsync(Request([42]));
            check(result.Success && result.Data!.Authors.Single().AuthorId == 904 && f.NewAuthors == 0 && f.Saves == 1,
                "Recheck reuses an Author that appeared after the first lookup.");
        }
        {
            using var f = new ProductFixture();
            var p = f.SeedProduct();
            var old = p.Authors!.Single();
            old.AuthorOrder = 3; old.Participation = "Research"; old.NameSnapshot = "Historic name"; old.IsPrimaryAuthor = true;
            f.Context.ChangeTracker.AcceptAllChanges();
            var result = await f.Service.UpdateAsync(new() { Id = p.Id, Title = " Updated ", AuthorUserIds = [41, 42, 42], Values = [new() { AttributeDefinitionId = 61, Value = " new " }] });
            check(result.Success && f.Saves == 1 && f.NewAuthors == 1, "Update adds missing Author in one save.");
            check(ReferenceEquals(old, p.Authors!.Single(x => x.AuthorId == 901)) && old.AuthorOrder == 3 && old.IsPrimaryAuthor && old.NameSnapshot == "Historic name",
                "Retained authors preserve link identity, order, primary flag and snapshots.");
            check(p.Title == "Updated" && p.Values!.Single().Value == "new", "Update normalizes values and title.");
            result = await f.Service.UpdateAsync(new() { Id = p.Id, Title = "Remove", AuthorUserIds = [42] });
            check(result.Success && f.Saves == 2 && p.Authors!.Count == 1 && p.Authors.Single().Author.AppUserId == 42,
                "Update removes only absent links, retains existing author.");
            check(f.Authors.Any(x => x.AuthorId == 901), "Removing a link does not remove the Author.");
            result = await f.Service.UpdateAsync(new() { Id = p.Id, Title = "No author change" });
            check(result.Success && f.Saves == 3 && p.Authors!.Count == 1 && p.Values!.Single().Value == "new", "Null authors/values leave relationships intact.");
            result = await f.Service.UpdateAsync(new() { Id = p.Id, Title = "Clear", AuthorUserIds = [], Values = [] });
            check(result.Success && f.Saves == 4 && p.Authors!.Count == 0 && p.Values!.Count == 0, "Empty authors/values clear optional collections.");
        }
        {
            using var f = new ProductFixture();
            var p = f.SeedProduct();
            var external = f.AddExternalLink(p);
            var result = await f.Service.GetByIdAsync(p.Id);
            var institution = result.Data!.Authors.Single(x => x.AuthorType == ProductAuthorType.Institutional);
            var other = result.Data.Authors.Single(x => x.AuthorType == ProductAuthorType.External);
            check(result.Success && f.Saves == 0 && institution is { AuthorId: 901, AppUserId: 41, UserId: 41, ExternalResearcherId: null }, "Institutional detail projection.");
            check(other is { AuthorId: 905, AppUserId: null, UserId: null, ExternalResearcherId: 82 }, "External detail never masquerades as an AppUser.");
            check(result.Data.Authors[0] == other && other.AuthorOrder == 1 && other.IsPrimaryAuthor && other.Participation == "External contribution" &&
                other.NameSnapshot == "Submitted name" && other.EmailSnapshot == "submitted@example.test" && other.IdentificationSnapshot == "historical-id" &&
                other.AffiliationSnapshot == "Old institution" && other.ParticipantTypeSnapshot == "External" && other.CreatedAt == external.CreatedAt && other.UpdatedAt == external.UpdatedAt,
                "Detail preserves every authorship snapshot and sorts by order.");
            var copy = JsonSerializer.Deserialize<ProductAuthorResponseDTO>(JsonSerializer.Serialize(other));
            check(copy is { AuthorId: 905, AppUserId: null, UserId: null, ExternalResearcherId: 82, AuthorType: ProductAuthorType.External }, "External JSON round trip.");
            var legacy = JsonSerializer.Deserialize<ProductAuthorResponseDTO>("{\"UserId\":41}");
            check(legacy is { AuthorId: null, AppUserId: 41, AuthorType: ProductAuthorType.Institutional }, "Legacy JSON keeps institutional identity without inventing AuthorId.");
            var update = await f.Service.UpdateAsync(new() { Id = p.Id, Title = "Keep external" });
            check(update.Success && p.Authors!.Contains(external), "Null author selection preserves external authors.");
            update = await f.Service.UpdateAsync(new() { Id = p.Id, Title = "Replace", AuthorUserIds = [] });
            check(update.Success && p.Authors!.Count == 0, "Explicit empty selection replaces all authors, including external ones.");
        }
        {
            using var f = new ProductFixture();
            f.SeedProduct(73);
            var independent = f.SeedProduct(null, 502);
            var list = await f.Service.ListAsync();
            check(list.Success && list.Data!.Any(x => x.ProjectId is null) && list.Data!.Any(x => x.ProjectId == 73) && f.Saves == 0, "List supports project results and independent production.");
            check((await f.Service.GetByIdAsync(independent.Id)).Data!.ProjectId is null, "Independent detail keeps null.");
            var byProject = await f.Service.ListByProjectAsync(73);
            check(byProject.Success && byProject.Data!.Count == 1 && byProject.Data[0].ProjectId == 73, "By-project listing remains filtered.");
            check((await f.Service.GetByIdAsync(999)).ErrorCode == ErrorCodes.Product.NotFound, "Missing product read.");
            check((await f.Service.DeleteAsync(independent.Id)).Success && f.Saves == 1, "Delete dependent method operates on the tracked aggregate.");
        }
        {
            using var f = new ProductFixture();
            var request = Request([42, 43]);
            request.Values = [new() { AttributeDefinitionId = 61, Value = " first " }, new() { AttributeDefinitionId = 61, Value = " last " }];
            var result = await f.Service.CreateAsync(request);
            check(result.Success && result.Data!.Values.Single().Value == "last" && f.Saves == 1 && f.RelationshipsValid,
                "Create builds ProductValues with navigation in the same save; duplicate definitions keep the last value.");
        }
        {
            using var f = new ProductFixture { FailAddProduct = true };
            var result = await f.Service.CreateAsync(Request([42]));
            check(!result.Success && f.Saves == 0 && f.Context.ChangeTracker.HasChanges(),
                "Apply failure after staging Author never commits; failed scope must be discarded.");
        }
        foreach (var update in new[] { false, true })
        foreach (var failure in new[] { "user", "values", "query", "source" })
        {
            using var f = new ProductFixture();
            var p = f.SeedProduct();
            f.FailAuthorRead = failure == "query";
            f.ConflictSource = failure == "source";
            var users = failure == "user" ? new List<int> { 42, 999 } : new List<int> { 42 };
            List<ProductValueUpsertDTO>? values = failure == "values" ? [new() { AttributeDefinitionId = 999, Value = "bad" }] : null;
            var result = update
                ? await f.Service.UpdateAsync(new() { Id = p.Id, Title = "Changed", AuthorUserIds = users, Values = values })
                : await f.Service.CreateAsync(new() { ProjectId = 73, ProductTypeId = 17, Title = "Changed", AuthorUserIds = users, Values = values });
            check(!result.Success && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges() && p.Title == "Original", "Failure before apply leaves real tracker unchanged: " + failure);
            if (failure == "user") check(result.ErrorCode == ErrorCodes.AppUser.NotFound, "Reuse AppUser error contract.");
            if (failure == "source") check(result.ErrorCode == ErrorCodes.Author.ExactlyOneSource && result.ValidationErrors?.Count > 0, "Reuse Author source validation contract.");
        }
        foreach (var id in new[] { 0, -1, 999 })
        {
            using var f = new ProductFixture();
            var request = Request([41]); request.ProjectId = id;
            var result = await f.Service.CreateAsync(request);
            check(!result.Success && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "Invalid/missing supplied project fails before changes.");
        }
        foreach (var update in new[] { false, true })
        {
            using var f = new ProductFixture { FailSave = true };
            var p = f.SeedProduct();
            var result = update ? await f.Service.UpdateAsync(new() { Id = p.Id, Title = "Changed", AuthorUserIds = [42] }) : await f.Service.CreateAsync(Request([42]));
            check(result.Error == ErrorType.Conflict && result.ErrorCode == ErrorCodes.Common.PersistenceConflict && f.Saves == 1, "Final constraint conflict has one save attempt and shared error.");
        }
        {
            using var f = new ProductFixture();
            using var cts = new CancellationTokenSource(); cts.Cancel();
            var result = await f.Service.CreateAsync(Request([42]), cts.Token);
            check(!result.Success && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "Cancellation before commit makes no changes.");
            var sql = new UnifiedProductRepository(f.Context).QueryWithRefs().AsSingleQuery().ToQueryString();
            check(sql.Contains("ExternalResearchers") && sql.Contains("Authors") && sql.Contains("AppUsers"), "Real repository query includes both person sources without connecting to SQL Server.");
        }
    }

    private static ProductCreateRequestDTO Request(List<int> ids) => new() { ProjectId = 73, ProductTypeId = 17, Title = "Product", AuthorUserIds = ids };
}

// Reads are deterministic doubles; mutations use the real Unified EF model/tracker.
// Save emulates generated IDs and AcceptAllChanges; no relational commit/rollback is claimed.
internal sealed class ProductFixture : IDisposable
{
    internal UnifiedDideDbContext Context { get; } = new(new DbContextOptionsBuilder<UnifiedDideDbContext>()
        .UseSqlServer("Server=(local);Database=ProductTests;Trusted_Connection=True;TrustServerCertificate=True").Options);
    internal List<Author> Authors { get; } = [];
    internal List<Product> Products { get; } = [];
    internal Dictionary<int, int> AuthorReads { get; } = [];
    internal int Saves, NewAuthors, InvalidAddedPrincipals, ProjectReads;
    internal bool FailSave, FailAuthorRead, ConflictSource, AuthorAppearsOnRecheck, FailAddProduct;
    internal bool RelationshipsValid = true;
    internal UnifiedProductService Service { get; }
    private int generatedId = 1000;

    internal ProductFixture()
    {
        var users = new[] { new AppUser { IdUser = 41 }, new AppUser { IdUser = 42 }, new AppUser { IdUser = 43 } };
        var project = new Project { ProjectId = 73 };
        var type = new ProductType { Id = 17, Name = "Report" };
        var definition = new ProductAttributeDefinition { Id = 61, ProductTypeId = 17, ProductAttributeId = 62,
            ProductAttribute = new ProductAttribute { Id = 62, Name = "Text", DataType = ProductAttributeDataType.Text } };
        Context.AttachRange(users); Context.Attach(project); Context.Attach(type); Context.Attach(definition);
        Authors.Add(new Author { AuthorId = 901, AppUserId = 41 });
        Authors.Add(new Author { AuthorId = 905, ExternalResearcherId = 82, ExternalResearcher = new ExternalResearcher { ExternalResearcherId = 82, FullName = "Current name", Email = "current@example.test" } });
        Context.AttachRange(Authors);
        var userRepo = Stub.For<IUnifiedAppUserRepository>((m, a) => Task.FromResult(users.FirstOrDefault(x => x.IdUser == (int)((object[])a[0]!)[0])));
        var projectRepo = Stub.For<IUnifiedProjectRepository>((m, a) => { ProjectReads++; return Task.FromResult((int)((object[])a[0]!)[0] == 73 ? project : null); });
        var typeRepo = Stub.For<IUnifiedCatalogRepository<ProductType>>((m, a) => Task.FromResult((int)((object[])a[0]!)[0] == 17 ? type : null));
        var definitionRepo = Stub.For<IUnifiedProductAttributeDefinitionRepository>((m, a) => new AsyncRows<ProductAttributeDefinition>([definition]));
        var authorRepo = Stub.For<IUnifiedAuthorRepository>((m, a) =>
        {
            if (m.Name == "AddAsync") { NewAuthors++; Authors.Add((Author)a[0]!); return Add(a[0]!); }
            if (m.Name != "GetByAppUserIdAsync") throw new InvalidOperationException(m.Name);
            if (FailAuthorRead) throw new TestReadException();
            var id = (int)a[0]!; AuthorReads[id] = AuthorReads.GetValueOrDefault(id) + 1;
            if (ConflictSource) return Task.FromResult<Author?>(new Author { AuthorId = 904, AppUserId = id, ExternalResearcherId = 82 });
            if (AuthorAppearsOnRecheck && id == 42 && AuthorReads[id] == 2)
            {
                var appeared = new Author { AuthorId = 904, AppUserId = 42 }; Authors.Add(appeared); Context.Attach(appeared);
            }
            var found = Authors.FirstOrDefault(x => x.AppUserId == id);
            // Match the actual repository's AsNoTracking behavior.
            return Task.FromResult(found is null ? null : new Author { AuthorId = found.AuthorId, AppUserId = found.AppUserId, ExternalResearcherId = found.ExternalResearcherId });
        });
        var productRepo = Stub.For<IUnifiedProductRepository>((m, a) => m.Name switch
        {
            "GetByIdWithRefsAsync" => Task.FromResult(Products.FirstOrDefault(x => x.Id == (int)a[0]!)),
            "GetByProjectAsync" => Task.FromResult(Products.Where(x => x.ProjectId == (int)a[0]!).ToList()),
            "QueryWithRefs" => new AsyncRows<Product>(Products),
            "AddAsync" => AddProduct((Product)a[0]!),
            "Remove" => Remove(a[0]!),
            _ => throw new InvalidOperationException(m.Name)
        });
        var links = Stub.For<IUnifiedProductAuthorRepository>((m, a) => m.Name switch
        {
            "AddAsync" => Add(a[0]!), "Remove" => Remove(a[0]!), "RemoveRange" => RemoveMany((IEnumerable<ProductAuthor>)a[0]!),
            _ => throw new InvalidOperationException(m.Name)
        });
        var values = Stub.For<IUnifiedProductValueRepository>((m, a) => m.Name switch
        {
            "AddAsync" => Add(a[0]!), "Remove" => Remove(a[0]!), "RemoveRange" => RemoveMany((IEnumerable<ProductValue>)a[0]!),
            _ => throw new InvalidOperationException(m.Name)
        });
        var uow = Stub.For<IUnifiedUnitOfWork>((m, a) =>
        {
            foreach (var ct in a.OfType<CancellationToken>()) ct.ThrowIfCancellationRequested();
            return m.Name switch
            {
                "get_AppUsers" => userRepo, "get_Authors" => authorRepo, "get_Projects" => projectRepo,
                "get_ProductTypes" => typeRepo, "get_ProductAttributeDefinitions" => definitionRepo,
                "get_Products" => productRepo, "get_ProductAuthors" => links, "get_ProductValues" => values,
                "SaveChangesAsync" => Save(), _ => throw new InvalidOperationException(m.Name)
            };
        });
        Service = new(uow);
    }
    internal Product SeedProduct(int? projectId = 73, int id = 501)
    {
        var p = new Product { Id = id, ProjectId = projectId, ProductTypeId = 17, Title = "Original", Authors = [new ProductAuthor { Id = id + 100, AuthorId = 901 }], Values = [new ProductValue { Id = id + 200, AttributeDefinitionId = 61, Value = "old" }] };
        Products.Add(p); Context.Attach(p); return p;
    }
    internal ProductAuthor AddExternalLink(Product p)
    {
        var link = new ProductAuthor { Id = 888, Product = p, AuthorId = 905, AuthorOrder = 1, IsPrimaryAuthor = true,
            Participation = "External contribution", NameSnapshot = "Submitted name", EmailSnapshot = "submitted@example.test",
            IdentificationSnapshot = "historical-id", AffiliationSnapshot = "Old institution", ParticipantTypeSnapshot = "External", UpdatedAt = DateTime.UtcNow };
        Context.Attach(link); return link;
    }
    private Task AddProduct(Product p) { if (FailAddProduct) throw new TestReadException(); Products.Add(p); return Add(p); }
    private Task Add(object row) { Context.Add(row); return Task.CompletedTask; }
    private object? Remove(object row) { Context.Remove(row); return null; }
    private object? RemoveMany<T>(IEnumerable<T> rows) where T : class { Context.RemoveRange(rows); return null; }
    private Task<int> Save()
    {
        Saves++;
        var added = Context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();
        InvalidAddedPrincipals += added.Count(e => e.Entity is not (Product or ProductAuthor or ProductValue or Author));
        foreach (var link in Context.ChangeTracker.Entries<ProductAuthor>().Where(e => e.State == EntityState.Added))
        {
            var productKey = Context.Entry(link.Entity.Product).Property(x => x.Id).CurrentValue;
            var authorKey = Context.Entry(link.Entity.Author).Property(x => x.AuthorId).CurrentValue;
            RelationshipsValid &= link.Property(x => x.ProductId).CurrentValue == productKey && link.Property(x => x.AuthorId).CurrentValue == authorKey;
        }
        foreach (var value in Context.ChangeTracker.Entries<ProductValue>().Where(e => e.State == EntityState.Added))
            RelationshipsValid &= value.Property(x => x.ProductId).CurrentValue == Context.Entry(value.Entity.Product!).Property(x => x.Id).CurrentValue;
        if (FailSave) throw new DbUpdateException("Simulated unique constraint race");
        foreach (var entry in added)
        foreach (var key in entry.Properties.Where(p => p.Metadata.IsPrimaryKey() && p.IsTemporary))
        { key.CurrentValue = ++generatedId; key.IsTemporary = false; }
        Context.ChangeTracker.AcceptAllChanges();
        return Task.FromResult(added.Count);
    }
    public void Dispose() => Context.Dispose();
}
