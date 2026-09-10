using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

var database = "tesis_articles_write_test_" + Guid.NewGuid().ToString("N");
var server = Environment.GetEnvironmentVariable("ARTICLES_TEST_SQL_SERVER") ?? @".\DINNOVA";
var cs = $@"Server={server};Database={database};Integrated Security=True;TrustServerCertificate=True";
var failure = new FailAfterAggregateSave();
var services = new ServiceCollection();
services.AddUnifiedDide(new ConfigurationBuilder().Build(), options => options.UseSqlServer(cs,
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).AddInterceptors(failure));
services.AddSingleton<IExternalDirectoryClient, DirectoryFixture>();
await using var provider = services.BuildServiceProvider();
await using var fixture = provider.CreateAsyncScope();
var db = fixture.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
    Console.WriteLine("PASS: " + message);
}
async Task<ServiceResult<RegisterArticleAggregateResponse>> Register(RegisterArticleAggregateRequest request)
{
    await using var scope = provider.CreateAsyncScope();
    return await scope.ServiceProvider.GetRequiredService<IUnifiedArticleRegistrationCommandService>().RegisterAsync(request);
}
async Task<(int, int, int, int, int, int, int)> AggregateCounts() =>
    (await db.Set<Product>().CountAsync(), await db.Set<Article>().CountAsync(),
     await db.Set<ProductValue>().CountAsync(), await db.Set<Author>().CountAsync(),
     await db.Set<ProductAuthor>().CountAsync(), await db.Set<ProductAuthorDynamicFieldValue>().CountAsync(),
     await db.Set<ArticleFile>().CountAsync());
RegisterArticleAggregateRequest Request(int type, string doi) => new()
{
    FormKey = "ArticleForm",
    Article = new() { ProductTypeId = type, Title = "Article " + doi, Doi = doi, Year = 2026,
        PublicationUrl = "https://example.test/article", IndexingDatabase = "Scopus" },
    Venue = new() { JournalName = "Test journal", IssnCode = "1234-5678" },
    VenueMetric = new() { Sjr = 1.234567m, Quartile = "Q2" },
    Participants = [new() { Index = 1, Nombre = "External One", ExternalResearcherId = 1,
        Participacion = "Coautor", IsPrimaryAuthor = true, ParticipantType = "Externo", Email = "external@example.test" }]
};
ArticleParticipantAggregateDto Institutional(int number, int index = 1) => new()
{
    Index = index, Nombre = "Institutional " + number, InstitutionalPersonId = number,
    Identificacion = number.ToString("D10"), Email = $"person{number}@example.test",
    Participacion = "Autor", ParticipantType = "Docente", Affiliation = "Institution", Orcid = $"0000-0000-0000-{number:D4}"
};
FormDefinition Form(string entity, string key, string fieldKey, bool dynamic) => new()
{
    EntityName = entity, FormKey = key, FormName = key, IsActive = true, CreatedAt = DateTime.UtcNow,
    Fields = [new() { IsVisible = true, IsEditable = true, Field = new()
    {
        EntityName = entity, FieldKey = fieldKey, FieldLabel = fieldKey, DataType = "text", SourceType = dynamic ? "Dynamic" : "Physical",
        IsActive = true, IsVisible = true, IsEditable = true, IsDynamic = dynamic, MaxLength = 1024, CreatedAt = DateTime.UtcNow
    } }]
};

try
{
    await db.Database.MigrateAsync();
    Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "All migrations run from zero in isolated SQL database");
    await db.Database.ExecuteSqlRawAsync("""
        SET IDENTITY_INSERT dbo.ProductTypes ON;
        INSERT dbo.ProductTypes(Id,Name,IsActive,IsLocked) VALUES (1,N'Scientific',1,0),(2,N'Regional',1,0);
        SET IDENTITY_INSERT dbo.ProductTypes OFF;
        SET IDENTITY_INSERT dbo.ProductAttributes ON;
        INSERT dbo.ProductAttributes(Id,Name,IsActive,IsLocked,DataType) VALUES
          (3,N'Journal',1,0,0),(4,N'Database',1,0,0),(5,N'SJR',1,0,0),(6,N'Quartile',1,0,0),
          (7,N'ISSN',1,0,0),(8,N'DOI',1,0,0),(9,N'Year',1,0,0),(10,N'URL',1,0,0);
        SET IDENTITY_INSERT dbo.ProductAttributes OFF;
        SET IDENTITY_INSERT dbo.ProductAttributeDefinitions ON;
        INSERT dbo.ProductAttributeDefinitions(Id,ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder)
        SELECT 700+Id,1,Id,0,Id FROM dbo.ProductAttributes;
        INSERT dbo.ProductAttributeDefinitions(Id,ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder)
        SELECT 900+Id,2,Id,0,Id FROM dbo.ProductAttributes;
        SET IDENTITY_INSERT dbo.ProductAttributeDefinitions OFF;
        SET IDENTITY_INSERT dbo.ExternalResearchers ON;
        INSERT dbo.ExternalResearchers(ExternalResearcherId,FullName,Email) VALUES
          (1,N'External One',N'external@example.test'),(2,N'External Two',N'external2@example.test');
        SET IDENTITY_INSERT dbo.ExternalResearchers OFF;
        """);
    var articleForm = Form("Article", "ArticleForm", "Title", false);
    var extra = new FieldCatalogEntry { EntityName = "Article", FieldKey = "AdditionalNote", FieldLabel = "Note",
        DataType = "text", SourceType = "Dynamic", IsActive = true, IsDynamic = true, IsEditable = true, IsVisible = true, CreatedAt = DateTime.UtcNow };
    articleForm.Fields.Add(new() { Field = extra, IsEditable = true, IsVisible = true });
    var participantForm = Form("ArticleParticipant", "ArticleParticipantForm", "Contribution", true);
    var indexing = new IndexingSource { Name = "Indexing fixture" };
    db.AddRange(articleForm, participantForm, indexing);
    await db.SaveChangesAsync();
    var fieldId = participantForm.Fields.Single().FieldId;
    var role = await fixture.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>().CreateAsync(new("user"));
    Check(role.Succeeded, "Identity role fixture ready");

    var scientificRequest = Request(1, "10.test/scientific");
    scientificRequest.Participants[0].DynamicFields.Add(new() { FieldKey = "Contribution", ValueString = "Analysis" });
    scientificRequest.DynamicFields.Add(new() { FieldId = extra.FieldId, ValueString = "Structural note" });
    scientificRequest.Files.Add(new() { FileName = "paper.pdf", FileUrl = "https://example.test/paper.pdf" });
    scientificRequest.IndexingSourceIds.Add(indexing.Id);
    scientificRequest.Venue.Type = "Journal";
    scientificRequest.Venue.IssueNumber = "Issue 7";
    var scientific = await Register(scientificRequest);
    Check(scientific.Success, "ScientificProduction independent: " + scientific.Message);
    var read = fixture.ServiceProvider.GetRequiredService<IUnifiedArticleReadRepository>();
    var row = await read.Query().SingleAsync(x => x.ProductId == scientific.Data!.ProductId);
    Check(row.ProjectId is null && row.ProductTypeId == 1 && row.Doi == "10.test/scientific" && row.Year == 2026 && row.Quartile == "Q2"
        && row.Journal == "Test journal" && row.Issn == "1234-5678" && row.Sjr == 1.234567m && row.IndexingDatabase == "Scopus"
        && row.PublicationUrl == "https://example.test/article", "Canonical ProductValues immediately visible in ArticleReadView");
    Check(await db.Set<ProductValue>().CountAsync(v => v.ProductId == scientific.Data!.ProductId && v.AttributeDefinitionId >= 703 && v.AttributeDefinitionId <= 710) == 8,
        "Actual definition IDs used, eight canonical values persisted");
    Check(await db.Set<ProductAuthorDynamicFieldValue>().AnyAsync(v => v.FieldId == fieldId && v.ValueString == "Analysis"), "Participant dynamic value persisted by FieldKey");
    Check(await db.Set<DynamicFieldValue>().AnyAsync(v => v.ArticleId == scientific.Data!.ArticleId && v.ValueString == "Structural note")
        && await db.Set<ArticleFile>().AnyAsync(f => f.ArticleId == scientific.Data!.ArticleId && f.FileName == "paper.pdf")
        && await db.Set<Venue>().AnyAsync(v => v.IssueNumber == "Issue 7")
        && await db.Set<ArticleIndexing>().AnyAsync(v => v.ArticleId == scientific.Data!.ArticleId && v.IndexingSourceId == indexing.Id),
        "Article extra fields, files, indexings and structural Venue metadata preserved");
    Check(!await db.Set<VenueMetric>().AnyAsync(), "No second SJR/Quartile/Year persistence in VenueMetrics");

    var regional = await Register(Request(2, "10.test/regional"));
    Check(regional.Success && await read.Query().AnyAsync(v => v.ProductId == regional.Data!.ProductId && v.ProductTypeId == 2 && v.ProjectId == null), "RegionalProduction independent");
    Check(await db.Set<Author>().CountAsync(a => a.ExternalResearcherId == 1) == 1, "External author reused through ExternalResearcher identity");

    var multi = Request(1, "10.test/multiple");
    multi.Participants = [Institutional(101, 2), new() { Index = 1, Nombre = "External Two", ExternalResearcherId = 2,
        Participacion = "Coautor", IsPrimaryAuthor = true, ParticipantType = "Externo",
        DynamicFields = [new() { FieldId = fieldId, ValueString = "Writing" }] }];
    var multiple = await Register(multi);
    Check(multiple.Success, "Institutional author plus external author: " + multiple.Message);
    var authors = await db.Set<ProductAuthor>().AsNoTracking().Include(a => a.Author).Include(a => a.DynamicFieldValues)
        .Where(a => a.ProductId == multiple.Data!.ProductId).OrderBy(a => a.AuthorOrder).ToListAsync();
    var appUser = await db.Set<AppUser>().AsNoTracking().SingleAsync(a => a.IdAsp == 1101);
    Check(authors.Count == 2 && authors[1].Author.AppUserId == appUser.IdUser && authors[1].Author.ExternalResearcherId == null
        && appUser.IdLocal.HasValue && authors[1].Author.Orcid == "0000-0000-0000-0101", "Directory ASP_ID boundary resolves AppUser -> Author (person ID is different)");
    Check(authors[0].Author.ExternalResearcherId == 2 && authors[0].Author.AppUserId == null && authors[0].AuthorOrder == 1
        && authors[0].IsPrimaryAuthor && authors[0].Participation == "Coautor" && !authors[1].IsPrimaryAuthor
        && multiple.Data!.ParticipantIds.SequenceEqual(authors.Select(a => a.Id)), "Author order and primary/participation independent; response has ProductAuthor IDs");
    Check(authors[1].NameSnapshot == "Institutional 101" && authors[1].IdentificationSnapshot == "0000000101"
        && authors[1].EmailSnapshot == "person101@example.test" && authors[1].AffiliationSnapshot == "Institution"
        && authors[0].DynamicFieldValues.Single().ValueString == "Writing", "Participant snapshots and dynamic values preserved");

    // Only the minimum FK graph necessary for the concrete Product.ProjectId relationship.
    var faculty = new Faculty { Name = "Faculty" };
    var project = new Project { ProjectCode = "TEST", ProjectName = "Project fixture", ProjectNumber = 1, CreatedByUserId = appUser.IdUser,
        ProjectType = new ProjectType { Name = "Type" }, ProjectState = new ProjectState { Name = "State" },
        ProjectOriginType = new ProjectOriginType { Name = "Origin" },
        ProjectGroup = new Group { Name = "Group", GroupType = new GroupType { Name = "Group type" } },
        Convocation = new Convocation { Name = "Convocation" }, ConvocationId = null };
    db.Add(faculty);
    await db.SaveChangesAsync();
    project.FacultyId = faculty.FacultyId;
    db.Add(project);
    await db.SaveChangesAsync();
    var fromProject = Request(1, "10.test/project");
    fromProject.Article.ProjectId = project.ProjectId;
    // Legacy false does not override a concrete Product.ProjectId.
    var linked = await Register(fromProject);
    Check(linked.Success && await read.Query().AnyAsync(v => v.ProductId == linked.Data!.ProductId && v.ProjectId == project.ProjectId
        && v.ProjectFacultyId == faculty.FacultyId && v.FacultyId == null), "Concrete project result; no invented Article faculty precedence");

    var aggregateBeforeFailure = await AggregateCounts();
    var duplicate = Request(2, "  10.TEST/SCIENTIFIC  ");
    duplicate.Participants = [Institutional(102)];
    var conflict = await Register(duplicate);
    Check(!conflict.Success && conflict.ErrorCode == "ARTICLE_DOI_DUPLICATE", "Duplicate DOI rejected by SQL unique index, across product types");
    Check(await AggregateCounts() == aggregateBeforeFailure, "DOI failure rolls back every aggregate table");
    Check(await db.Set<AppUser>().AnyAsync(a => a.IdAsp == 1102 && a.IdLocal != null)
        && await db.Users.AnyAsync(u => u.Email == "person102@example.test"), "DOI failure preserves successful Identity/AppUser provisioning");

    var rollback = Request(1, "10.test/rollback");
    rollback.Article.Title = "FORCE_ROLLBACK";
    rollback.Participants = [Institutional(103)];
    rollback.Participants[0].DynamicFields.Add(new() { FieldId = fieldId, ValueString = "Rollback contribution" });
    rollback.Files.Add(new() { FileName = "rollback.pdf" });
    var failed = await Register(rollback);
    Check(failure.Triggered && !failed.Success, "Injected failure occurs AFTER aggregate SQL save, BEFORE transaction commit");
    Check(await AggregateCounts() == aggregateBeforeFailure && !await read.Query().AnyAsync(v => v.Doi == "10.test/rollback"),
        "Full aggregate rollback: Product/Article/Values/Authors/ProductAuthors/dynamics/files");
    var retained = await db.Set<AppUser>().AsNoTracking().SingleAsync(a => a.IdAsp == 1103);
    Check(retained.IdLocal.HasValue && await db.Users.AnyAsync(u => u.Id == retained.IdLocal && u.Email == "person103@example.test"),
        "New Identity/AppUser remains after failure following aggregate SQL save");

    var usersBeforeReuse = await db.Users.CountAsync();
    var bridgesBeforeReuse = await db.Set<AppUser>().CountAsync();
    var reuse = Request(1, "10.test/reused-user"); reuse.Participants = [Institutional(103)];
    var reused = await Register(reuse);
    Check(reused.Success && await db.Users.CountAsync() == usersBeforeReuse && await db.Set<AppUser>().CountAsync() == bridgesBeforeReuse
        && await db.Set<ProductAuthor>().AnyAsync(a => a.ProductId == reused.Data!.ProductId && a.Author.AppUserId == retained.IdUser)
        && await db.Set<AppUser>().AnyAsync(a => a.IdUser == retained.IdUser && a.IdAsp == 1103 && a.IdLocal == retained.IdLocal),
        "Existing provisioned user reused without duplicate Identity/AppUser or relink");

    var beforeConflict = await AggregateCounts();
    var originalLinks = await db.Set<AppUser>().AsNoTracking().OrderBy(a => a.IdUser)
        .Select(a => new { a.IdUser, a.IdAsp, a.IdLocal }).ToListAsync();
    // Different ASP_ID with an email already owned by the first institutional person.
    var mappingRequest = Request(1, "10.test/mapping-conflict"); mappingRequest.Participants = [Institutional(104)];
    mappingRequest.Participants[0].Email = "person101@example.test";
    var mappingConflict = await Register(mappingRequest);
    Check(!mappingConflict.Success && mappingConflict.ErrorCode == ErrorCodes.IdentityProvisioning.MappingConflict
        && mappingConflict.Error == ErrorType.Conflict && await AggregateCounts() == beforeConflict
        && originalLinks.SequenceEqual(await db.Set<AppUser>().AsNoTracking().OrderBy(a => a.IdUser)
            .Select(a => new { a.IdUser, a.IdAsp, a.IdLocal }).ToListAsync())
        && await db.Users.CountAsync() == usersBeforeReuse, "Mapping conflict: no Article, no Identity creation and no relink");

    await using (var dirtyScope = provider.CreateAsyncScope())
    {
        var dirtyDb = dirtyScope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
        dirtyDb.Add(new Product { Title = "Pending domain mutation", ProductTypeId = 1 });
        var dirtyResult = await dirtyScope.ServiceProvider.GetRequiredService<IUnifiedArticleRegistrationCommandService>().RegisterAsync(reuse);
        Check(!dirtyResult.Success && dirtyResult.ErrorCode == ErrorCodes.IdentityProvisioning.PendingChanges
            && dirtyDb.ChangeTracker.HasChanges() && !await db.Set<Product>().AnyAsync(p => p.Title == "Pending domain mutation"),
            "Identity boundary still rejects pending domain changes without flushing them");
    }

    var missingType = Request(1, "missing"); missingType.Article.ProductTypeId = null;
    Check(!(await Register(missingType)).Success, "Missing product type rejected without guessing");
    var missingProject = Request(1, "missing"); missingProject.Article.IsProjectResult = true;
    Check(!(await Register(missingProject)).Success, "Legacy project flag without ProjectId rejected");
    var duplicateField = Request(1, "missing"); duplicateField.Participants[0].DynamicFields =
        [new() { FieldId = fieldId, FieldKey = "Wrong", ValueString = "x" }];
    Check(!(await Register(duplicateField)).Success, "Conflicting dynamic field ID/key rejected");
    var wrongPerson = Request(1, "wrong-person"); wrongPerson.Participants = [Institutional(101)];
    wrongPerson.Participants[0].InstitutionalPersonId = 1101;
    Check(!(await Register(wrongPerson)).Success, "ASP_ID cannot be passed as the directory person ID");
    var orcidConflict = Request(1, "orcid-conflict"); orcidConflict.Participants = [Institutional(101)];
    orcidConflict.Participants[0].Orcid = "different-orcid";
    Check(!(await Register(orcidConflict)).Success, "Existing Author ORCID cannot be overwritten by a contradictory value");
    var baseDynamic = new FieldCatalogEntry { EntityName = "Article", FieldKey = "Doi", FieldLabel = "DOI", DataType = "text",
        SourceType = "Dynamic", IsActive = true, IsVisible = true, IsEditable = true, IsDynamic = true, CreatedAt = DateTime.UtcNow };
    articleForm.Fields.Add(new() { Field = baseDynamic, IsVisible = true, IsEditable = true });
    await db.SaveChangesAsync();
    var duplicateBase = Request(1, "duplicate-base");
    duplicateBase.DynamicFields.Add(new() { FieldId = baseDynamic.FieldId, ValueString = "second-doi" });
    Check(!(await Register(duplicateBase)).Success, "Base attributes rejected from Article dynamic values even when configured as dynamic");
    var latest = Form("Article", "OtherForm", "Title", false); latest.UpdatedAt = DateTime.UtcNow.AddDays(1);
    var normalized = Form("Article", "Article_Form", "Title", false); normalized.UpdatedAt = latest.UpdatedAt;
    db.AddRange(latest, normalized);
    await db.SaveChangesAsync();
    var selector = fixture.ServiceProvider.GetRequiredService<IUnifiedArticleFormSelector>();
    Check((await selector.SelectAsync("Article", "ArticleForm"))!.FormId == articleForm.FormId
        && (await selector.SelectAsync("Article", "article form"))!.FormId == normalized.FormId
        && (await selector.SelectAsync("Article", "missing"))!.FormId == normalized.FormId,
        "Single effective selector: exact key, normalized key, recency and ID tie-break");
    Console.WriteLine($"PASS: {checks} Unified Article write SQL/EF assertions.");
}
finally
{
    if (!database.StartsWith("tesis_articles_write_test_", StringComparison.Ordinal)) throw new Exception("Invalid fixture");
    await db.Database.EnsureDeletedAsync();
}

sealed class DirectoryFixture : IExternalDirectoryClient
{
    private static readonly IReadOnlyList<ExternalUserProfileModel> Profiles = Enumerable.Range(101, 4).Select(i =>
        new ExternalUserProfileModel { ExternalId = i, AspId = i + 1000, Document = i.ToString("D10"),
            Email = i == 104 ? "person101@example.test" : $"person{i}@example.test", FullName = "Institutional " + i }).ToList();
    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByDocumentsAsync(IEnumerable<string> documents, CancellationToken ct = default)
        => Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok(Profiles.Where(p => documents.Contains(p.Document)).ToList()));
    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken ct = default)
        => Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok(Profiles.Where(p => emails.Contains(p.Email)).ToList()));
    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
}

sealed class FailAfterAggregateSave : SaveChangesInterceptor
{
    public bool Triggered { get; private set; }
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<Product>().Any(e => e.Entity.Title == "FORCE_ROLLBACK"))
        {
            Triggered = true;
            throw new InvalidOperationException("Deliberate test failure after SQL SaveChanges.");
        }
        return ValueTask.FromResult(result);
    }
}
