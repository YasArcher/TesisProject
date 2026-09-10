using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

internal static class ProjectSeedTests
{
    public static async Task RunAsync()
    {
        var name = "tesis_project_seed_test_" + Guid.NewGuid().ToString("N");
        var cs = $@"Server=.\DINNOVA;Database={name};Integrated Security=True;TrustServerCertificate=True";
        await using var db = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).Options);
        var sql = await File.ReadAllTextAsync("scripts/seeds/projects-unified.sql");
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync("scripts/seeds/projects-unified.manifest.json"));
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; Console.WriteLine("PASS: " + message); }
        async Task<(int inserts, int updates)> Run(bool apply, string? commandText = null, int asp = 1)
        {
            await using var connection = new SqlConnection(cs); await connection.OpenAsync();
            await using var command = connection.CreateCommand(); command.CommandTimeout = 120;
            command.CommandText = commandText ?? sql;
            command.Parameters.AddWithValue("@ExpectedDatabase", name);
            command.Parameters.AddWithValue("@Apply", apply);
            command.Parameters.AddWithValue("@OwnerAspId", asp);
            command.Parameters.AddWithValue("@OwnerEmail", "seedowner@example.test");
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) { }
            await reader.NextResultAsync(); int inserts=0, updates=0;
            while (await reader.ReadAsync()) { inserts+=reader.GetInt32(2); updates+=reader.GetInt32(3); }
            return (inserts, updates);
        }
        async Task Reject(Func<Task> action, int number, string message)
        {
            try { await action(); }
            catch (SqlException ex) when (ex.Number == number) { Check(true, message); return; }
            throw new Exception("Expected rejection: " + message);
        }
        try
        {
            await db.Database.MigrateAsync();
            var owner = new IdentityUser<int> { UserName="seedowner", Email="seedowner@example.test", NormalizedEmail="SEEDOWNER@EXAMPLE.TEST" };
            var role = new IdentityRole<int> { Name="superadmin", NormalizedName="SUPERADMIN" };
            db.AddRange(owner,role); await db.SaveChangesAsync();
            db.AddRange(new AppUser { IdAsp=1, IdLocal=owner.Id }, new IdentityUserRole<int> { UserId=owner.Id, RoleId=role.Id });
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.ProductTypes ON; INSERT dbo.ProductTypes (Id,Name,IsActive,IsLocked) VALUES (1,N'Producto smoke',1,0); SET IDENTITY_INSERT dbo.ProductTypes OFF;");
            var preview = await Run(false);
            Check(preview == (461,1) && !await db.Set<DocumentType>().AnyAsync(), "Preview reports 462 canonical rows without writing application tables");
            await Reject(async () => { await Run(true,asp:999); },51001,"Owner must resolve to an existing mapped superadmin");
            await db.Database.ExecuteSqlRawAsync("UPDATE dbo.ProductTypes SET Name=N'Real catalog' WHERE Id=1");
            await Reject(async () => { await Run(true); },51002,"Ambiguous catalog conflict aborts without overwrite");
            await db.Database.ExecuteSqlRawAsync("UPDATE dbo.ProductTypes SET Name=N'Producto smoke' WHERE Id=1");
            var realProduct = new Product { ProductTypeId=1, Title="Real product", CreatedAt=DateTime.UtcNow };
            db.Add(realProduct); await db.SaveChangesAsync();
            await Reject(async () => { await Run(true); },51003,"Smoke catalog referenced by a real product is protected");
            db.Remove(realProduct); await db.SaveChangesAsync();
            await Reject(async () => { await Run(true,sql.Replace("COMMIT TRANSACTION;", "THROW 51099, 'Injected seed failure', 1;\nCOMMIT TRANSACTION;")); },51099,"Injected late failure aborts the seed");
            Check(!await db.Set<DocumentType>().AnyAsync() && (await db.Set<ProductType>().AsNoTracking().SingleAsync()).Name=="Producto smoke",
                "Late failure rolls back inserts and known fixture corrections");
            Check(await Run(true)==(461,1), "Fresh Unified baseline applied with the explicit smoke correction");
            Check(await Run(true)==(0,0), "Second execution is idempotent");
            await using var verify = new SqlConnection(cs); await verify.OpenAsync();
            foreach (var table in manifest.RootElement.GetProperty("rows").EnumerateObject())
            {
                await using var query = verify.CreateCommand();
                query.CommandText=$"SELECT COUNT(*) FROM dbo.[{table.Name}]";
                Check(Convert.ToInt32(await query.ExecuteScalarAsync())==table.Value.GetInt32(), "Canonical row count: "+table.Name);
            }
            Check(await db.Users.CountAsync()==1 && await db.Set<AppUser>().CountAsync()==1 && !await db.Set<ExternalResearcher>().AnyAsync(),
                "No copied institutional accounts or fake external researcher");
            await using var exclusions = verify.CreateCommand();
            exclusions.CommandText="SELECT (SELECT COUNT(*) FROM dbo.Faculties)+(SELECT COUNT(*) FROM dbo.AcademicTerms)+(SELECT COUNT(*) FROM dbo.Countries WHERE Id=41)+(SELECT COUNT(*) FROM dbo.Institutions WHERE Id=37)+(SELECT COUNT(*) FROM dbo.FormDefinitions)";
            Check(Convert.ToInt32(await exclusions.ExecuteScalarAsync())==0,"No synchronized catalogs, fake country/institution or invented Article forms");
        }
        finally { await db.Database.EnsureDeletedAsync(); }
        Console.WriteLine($"PASS: {checks} Projects seed SQL checks.");
    }
}
