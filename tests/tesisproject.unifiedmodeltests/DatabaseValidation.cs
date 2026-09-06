using System.Data;
using System.Data.Common;
using Regex = System.Text.RegularExpressions.Regex;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

internal static class DatabaseValidation
{
    public static async Task RunAsync()
    {
        await using var context = new UnifiedDideDbContextFactory().CreateDbContext([]);
        var target = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
        // The integration fixture is restricted to the newly created final development database.
        Require(target.InitialCatalog == "tesis_unified", "Unexpected integration-test database");
        await context.Database.OpenConnectionAsync();
        var connection = context.Database.GetDbConnection();
        var model = context.GetService<IDesignTimeModel>().Model;
        var relational = model.GetRelationalModel();
        var tables = relational.Tables.ToArray();
        var actualTables = await Rows(connection, "SELECT s.name AS [Schema], t.name AS [Table] FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id WHERE t.is_ms_shipped=0");
        Same(tables.Select(t => $"{t.Schema}.{t.Name}").Append("dbo.__EFMigrationsHistoryUnifiedDide"),
            actualTables.Select(r => $"{r["Schema"]}.{r["Table"]}"), "tables");

        var columns = await Rows(connection, """
            SELECT s.name AS [Schema], t.name AS [Table], c.name AS [Column], c.is_nullable AS Nullable,
              c.is_identity AS IsIdentity,
              ty.name + CASE
                WHEN ty.name IN ('nvarchar','nchar') THEN '(' + CASE WHEN c.max_length=-1 THEN 'max' ELSE CAST(c.max_length/2 AS varchar(10)) END + ')'
                WHEN ty.name IN ('varchar','char','varbinary','binary') THEN '(' + CASE WHEN c.max_length=-1 THEN 'max' ELSE CAST(c.max_length AS varchar(10)) END + ')'
                WHEN ty.name IN ('decimal','numeric') THEN '(' + CAST(c.precision AS varchar(10)) + ',' + CAST(c.scale AS varchar(10)) + ')'
                ELSE '' END AS StoreType
            FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id
            JOIN sys.schemas s ON t.schema_id=s.schema_id JOIN sys.types ty ON c.user_type_id=ty.user_type_id
            WHERE t.name <> '__EFMigrationsHistoryUnifiedDide'
            """);
        Same(tables.SelectMany(t => t.Columns.Select(c => $"{t.Schema}.{t.Name}.{c.Name}|{c.StoreType}|{c.IsNullable}")),
            columns.Select(r => $"{r["Schema"]}.{r["Table"]}.{r["Column"]}|{r["StoreType"]}|{r["Nullable"]}"), "column types/nullability");
        foreach (var column in columns)
        {
            var table = tables.Single(t => t.Name == (string)column["Table"] && t.Schema == (string)column["Schema"]);
            var mapped = table.Columns.Single(c => c.Name == (string)column["Column"]);
            var expectedIdentity = mapped.PropertyMappings.Any(m => m.Property.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn);
            Require(expectedIdentity == (bool)column["IsIdentity"], $"Identity column differs: {table.Name}.{mapped.Name}");
        }

        var keys = await Rows(connection, """
            SELECT s.name AS [Schema], t.name AS [Table], k.name AS [Name], c.name AS [Column], ic.key_ordinal AS Position
            FROM sys.key_constraints k JOIN sys.tables t ON k.parent_object_id=t.object_id
            JOIN sys.schemas s ON t.schema_id=s.schema_id
            JOIN sys.index_columns ic ON ic.object_id=t.object_id AND ic.index_id=k.unique_index_id
            JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=ic.column_id
            WHERE t.name <> '__EFMigrationsHistoryUnifiedDide'
            """);
        Same(tables.SelectMany(t => t.UniqueConstraints.Select(k => $"{t.Schema}.{t.Name}.{k.Name}|{string.Join(',', k.Columns.Select(c => c.Name))}")),
            GroupColumns(keys), "PK/unique keys");

        var indexes = await Rows(connection, """
            SELECT s.name AS [Schema], t.name AS [Table], i.name AS [Name], i.is_unique AS IsUnique,
              i.filter_definition AS Filter, c.name AS [Column], ic.key_ordinal AS Position
            FROM sys.indexes i JOIN sys.tables t ON i.object_id=t.object_id JOIN sys.schemas s ON t.schema_id=s.schema_id
            JOIN sys.index_columns ic ON ic.object_id=t.object_id AND ic.index_id=i.index_id
            JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=ic.column_id
            WHERE i.index_id>0 AND i.is_primary_key=0 AND i.is_unique_constraint=0 AND ic.key_ordinal>0
            """);
        Same(tables.SelectMany(t => t.Indexes.Select(i => $"{t.Schema}.{t.Name}.{i.Name}|{i.IsUnique}|{Normalize(i.Filter)}|{string.Join(',', i.Columns.Select(c => c.Name))}")),
            indexes.GroupBy(r => $"{r["Schema"]}.{r["Table"]}.{r["Name"]}").Select(g =>
                $"{g.Key}|{g.First()["IsUnique"]}|{Normalize(g.First()["Filter"] as string)}|{string.Join(',', g.OrderBy(r => r["Position"]).Select(r => r["Column"]))}"), "indexes, UNIQUE and filters");

        var fks = await Rows(connection, """
            SELECT s.name AS [Schema], t.name AS [Table], f.name AS [Name], c.name AS [Column],
              ps.name AS PrincipalSchema, pt.name AS PrincipalTable, pc.name AS PrincipalColumn, fc.constraint_column_id AS Position,
              f.is_disabled AS Disabled, f.is_not_trusted AS Untrusted, f.delete_referential_action AS DeleteAction
            FROM sys.foreign_keys f JOIN sys.tables t ON f.parent_object_id=t.object_id JOIN sys.schemas s ON t.schema_id=s.schema_id
            JOIN sys.tables pt ON f.referenced_object_id=pt.object_id JOIN sys.schemas ps ON pt.schema_id=ps.schema_id
            JOIN sys.foreign_key_columns fc ON f.object_id=fc.constraint_object_id
            JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=fc.parent_column_id
            JOIN sys.columns pc ON pc.object_id=pt.object_id AND pc.column_id=fc.referenced_column_id
            """);
        Same(tables.SelectMany(t => t.ForeignKeyConstraints.Select(f => $"{t.Schema}.{t.Name}.{f.Name}|{string.Join(',', f.Columns.Select(c => c.Name))}|{f.PrincipalTable.Schema}.{f.PrincipalTable.Name}|{string.Join(',', f.PrincipalColumns.Select(c => c.Name))}")),
            fks.GroupBy(r => $"{r["Schema"]}.{r["Table"]}.{r["Name"]}").Select(g =>
                $"{g.Key}|{string.Join(',', g.OrderBy(r => r["Position"]).Select(r => r["Column"]))}|{g.First()["PrincipalSchema"]}.{g.First()["PrincipalTable"]}|{string.Join(',', g.OrderBy(r => r["Position"]).Select(r => r["PrincipalColumn"]))}"), "FK targets/columns");
        Require(fks.All(r => !(bool)r["Disabled"] && !(bool)r["Untrusted"] && Convert.ToInt32(r["DeleteAction"]) == 0), "Disabled/untrusted/cascading FK");

        var checks = await Rows(connection, """
            SELECT s.name AS [Schema], t.name AS [Table], c.name AS [Name], c.definition AS Definition,
                c.is_disabled AS Disabled, c.is_not_trusted AS Untrusted
            FROM sys.check_constraints c JOIN sys.tables t ON c.parent_object_id=t.object_id JOIN sys.schemas s ON t.schema_id=s.schema_id
            """);
        Same(model.GetEntityTypes().SelectMany(e => e.GetCheckConstraints().Select(c => $"{e.GetSchema()}.{e.GetTableName()}.{c.Name}|{Normalize(c.Sql)}")),
            checks.Select(r => $"{r["Schema"]}.{r["Table"]}.{r["Name"]}|{Normalize((string)r["Definition"])}"), "CHECK definitions");
        Require(checks.All(r => !(bool)r["Disabled"] && !(bool)r["Untrusted"]), "Disabled/untrusted CHECK");
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Require(applied.SequenceEqual(context.Database.GetMigrations()), "Unexpected migration history");
        Console.WriteLine($"PASS physical: {tables.Length} tables, {keys.Select(r => r["Name"]).Distinct().Count()} PK/unique keys, {fks.Select(r => r["Name"]).Distinct().Count()} FKs, {indexes.Select(r => r["Name"]).Distinct().Count()} indexes, {checks.Count} CHECK; every column type/nullability and identity attribute matched EF.");
        await ExerciseData(context);
    }

    private static async Task ExerciseData(UnifiedDideDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var identity = new IdentityUser<int> { UserName = $"unified-{suffix}", NormalizedUserName = $"UNIFIED-{suffix}", SecurityStamp = Guid.NewGuid().ToString() };
            var faculty = new Faculty { Name = $"Faculty {suffix}", ExternalFacultyId = 910001 };
            var term = new AcademicTerm { Name = $"Term {suffix}", ExternalPeriodId = 910001, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 6, 30) };
            context.AddRange(identity, faculty, term);
            await context.SaveChangesAsync();
            var user = new AppUser { IdLocal = identity.Id, IdAsp = 910001 };
            var unusedUser = new AppUser { IdAsp = 910002 };
            var external = new ExternalResearcher { FullName = "External researcher", Email = "external@example.test" };
            var unusedExternal = new ExternalResearcher { FullName = "Other researcher", Email = "other@example.test" };
            context.AddRange(user, unusedUser, external, unusedExternal);
            await context.SaveChangesAsync();
            var project = new Project
            {
                ProjectName = $"Project {suffix}", FacultyId = faculty.FacultyId, CreatedByUserId = user.IdUser,
                ProjectType = new ProjectType { Name = $"Type {suffix}" }, ProjectState = new ProjectState { Name = $"State {suffix}" },
                ProjectOriginType = new ProjectOriginType { Name = $"Origin {suffix}" }, Convocation = new Convocation { Name = $"Call {suffix}" },
                ProjectGroup = new Group { Name = $"Team {suffix}", GroupType = new GroupType { Name = $"Group type {suffix}" } }
            };
            var productType = new ProductType { Name = $"Article {suffix}" };
            var product = new Product { Title = $"Project article {suffix}", Project = project, ProductType = productType };
            var independent = new Product { Title = $"Independent article {suffix}", ProductType = productType };
            var article = new Article { Product = product, FacultyId = faculty.FacultyId, AcademicTermId = term.AcademicTermId };
            var independentArticle = new Article { Product = independent, FacultyId = faculty.FacultyId, AcademicTermId = term.AcademicTermId };
            var institutionalAuthor = new Author { AppUser = user, Orcid = $"test-{suffix}" };
            var externalAuthor = new Author { ExternalResearcher = external, ExternalAuthorId = $"external-{suffix}" };
            var first = new ProductAuthor { Product = product, Author = institutionalAuthor, AuthorOrder = 1, Participation = "Autor", IsPrimaryAuthor = true, NameSnapshot = "Institutional byline" };
            var second = new ProductAuthor { Product = product, Author = externalAuthor, AuthorOrder = 2, Participation = "Coautor", AffiliationSnapshot = "External affiliation" };
            var visit = new Visit { Project = project, AcademicTermId = term.AcademicTermId, VisitState = new VisitState { Name = $"Visit state {suffix}" } };
            var objective = new ProjectObjective { Project = project, Objective = "Validation", Result = "Integrity", ObjectiveType = new ObjectiveType { Name = $"Objective {suffix}" }, WeightedPercentage = 100 };
            var activity = new ObjectiveActivity { Objective = objective, ActionText = "Validate", ActivityResult = "Validated" };
            var assignment = new ObjectiveActivityUser { ObjectiveActivity = activity, Visit = visit, UserId = user.IdUser };
            var token = new RefreshToken { UserId = identity.Id, TokenHash = $"fixture-{suffix}", ExpiresAtUtc = DateTime.UtcNow.AddHours(1) };
            context.AddRange(article, independentArticle, first, second, assignment, token);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var authors = await context.ProductAuthors.Where(p => p.ProductId == product.Id).Include(p => p.Author).OrderBy(p => p.AuthorOrder).ToListAsync();
            Require(authors.Count == 2 && authors[0].Author.AppUserId == user.IdUser && authors[1].Author.ExternalResearcherId == external.ExternalResearcherId, "Mixed institutional/external authors did not persist");
            Require(await context.Products.AnyAsync(p => p.Id == independent.Id && p.ProjectId == null && p.Article != null), "Independent article did not persist");
            Require(await context.Articles.AnyAsync(a => a.Id == article.Id && a.Product.ProjectId == project.ProjectId), "Project -> product -> article did not persist");

            // Constraint failures must happen in SQL Server. Each statement is parameterized.
            await Reject(context, "XOR empty", "CK_Authors_ExactlyOneSource", $"INSERT INTO [dbo].[Authors] DEFAULT VALUES", 547);
            await Reject(context, "XOR both", "CK_Authors_ExactlyOneSource", $"INSERT INTO [dbo].[Authors] ([AppUserId],[ExternalResearcherId]) VALUES ({unusedUser.IdUser},{unusedExternal.ExternalResearcherId})", 547);
            await Reject(context, "duplicate institutional profile", "IX_Authors_AppUserId", $"INSERT INTO [dbo].[Authors] ([AppUserId]) VALUES ({user.IdUser})", 2601, 2627);
            await Reject(context, "duplicate external profile", "IX_Authors_ExternalResearcherId", $"INSERT INTO [dbo].[Authors] ([ExternalResearcherId]) VALUES ({external.ExternalResearcherId})", 2601, 2627);
            await Reject(context, "duplicate product author", "IX_ProductAuthors_ProductId_AuthorId", $"INSERT INTO [dbo].[ProductAuthors] ([ProductId],[AuthorId],[AuthorOrder],[IsPrimaryAuthor],[CreatedAt]) VALUES ({product.Id},{institutionalAuthor.AuthorId},{3},{false},{DateTime.UtcNow})", 2601, 2627);
            await Reject(context, "duplicate author order", "IX_ProductAuthors_ProductId_AuthorOrder", $"UPDATE [dbo].[ProductAuthors] SET [AuthorOrder]={1} WHERE [Id]={second.Id}", 2601, 2627);
            await Reject(context, "nonpositive author order", "CK_ProductAuthors_PositiveOrder", $"UPDATE [dbo].[ProductAuthors] SET [AuthorOrder]={0} WHERE [Id]={second.Id}", 547);
            await Reject(context, "second article for product", "IX_Articles_ProductId", $"UPDATE [dbo].[Articles] SET [ProductId]={product.Id} WHERE [Id]={independentArticle.Id}", 2601, 2627);
            await Reject(context, "missing author FK", "FK_ProductAuthors_Authors_AuthorId", $"UPDATE [dbo].[ProductAuthors] SET [AuthorId]={int.MaxValue} WHERE [Id]={second.Id}", 547);
            await Reject(context, "missing identity FK", "FK_AppUsers_AspNetUsers_IdLocal", $"UPDATE [dbo].[AppUsers] SET [IdLocal]={int.MaxValue} WHERE [IdUser]={unusedUser.IdUser}", 547);
            await Reject(context, "activity user FK", "FK_ObjectiveActivityUsers_AppUsers_UserId", $"UPDATE [dbo].[ObjectiveActivityUsers] SET [UserId]={int.MaxValue} WHERE [Id]={assignment.Id}", 547);
            await Reject(context, "NoAction author delete", "FK_ProductAuthors_Authors_AuthorId", $"DELETE FROM [dbo].[Authors] WHERE [AuthorId]={externalAuthor.AuthorId}", 547);
            await Reject(context, "period dates", "CK_AcademicTerms_Dates", $"UPDATE [dbo].[AcademicTerms] SET [EndDate]={new DateTime(2025, 1, 1)} WHERE [AcademicTermId]={term.AcademicTermId}", 547);
            Console.WriteLine("PASS data: project article + independent article; one product with institutional and external authors; Identity/AppUser/RefreshToken; shared faculty/period and activity assignment; 13 invalid writes rejected by SQL Server.");
        }
        finally { await transaction.RollbackAsync(); context.ChangeTracker.Clear(); }
        Console.WriteLine("All validation data rolled back; no demonstration data or seeds retained.");
    }

    private static async Task Reject(UnifiedDideDbContext context, string label, string constraint, FormattableString sql, params int[] numbers)
    {
        try { await context.Database.ExecuteSqlInterpolatedAsync(sql); }
        catch (SqlException ex) when (numbers.Contains(ex.Number) && ex.Message.Contains(constraint)) { return; }
        throw new InvalidOperationException($"Expected SQL rejection did not occur: {label}");
    }

    private static async Task<List<Dictionary<string, object>>> Rows(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand(); command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
            rows.Add(Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, i => reader.IsDBNull(i) ? (object)"" : reader.GetValue(i)));
        return rows;
    }
    private static IEnumerable<string> GroupColumns(List<Dictionary<string, object>> rows) =>
        rows.GroupBy(r => $"{r["Schema"]}.{r["Table"]}.{r["Name"]}").Select(g => $"{g.Key}|{string.Join(',', g.OrderBy(r => r["Position"]).Select(r => r["Column"]))}");
    private static string Normalize(string? value) => Regex.Replace(value ?? "", @"[\s\[\]()]", "").ToLowerInvariant();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Same(IEnumerable<string> expected, IEnumerable<string> actual, string label)
    {
        var e = expected.ToHashSet(StringComparer.OrdinalIgnoreCase); var a = actual.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Require(e.SetEquals(a), $"Physical {label} mismatch. Missing: {string.Join("; ", e.Except(a))}. Extra: {string.Join("; ", a.Except(e))}");
    }
}
