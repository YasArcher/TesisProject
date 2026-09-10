using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.shared.Enums;

internal static class ArticleReadHardeningTests
{
    public static async Task RunAsync()
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        Check(Enum.GetValues<BaseProductTypeId>().Select(x => (x.ToString(), (int)x)).SequenceEqual(
            new[] { ("ScientificProduction",1), ("RegionalProduction",2), ("Conference",3),
                ("Book",4), ("BookChapter",5), ("DegreeWork",6), ("AssistantScholarshipInternship",7) }),
            "Base product type seed IDs changed");
        Check(Enum.GetValues<BaseProductAttributeId>().Select(x => (x.ToString(), (int)x)).SequenceEqual(
            new[] { ("Title",1), ("Authors",2), ("Journal",3), ("IndexingDatabase",4), ("Sjr",5),
                ("Quartile",6), ("IssnIsbn",7), ("Doi",8), ("Year",9), ("ConsultationUrl",10) }),
            "Base attribute seed IDs changed");

        var database = "tesis_articles_hardening_test_" + Guid.NewGuid().ToString("N");
        var connectionString = $@"Server=.\DINNOVA;Database={database};Integrated Security=True;TrustServerCertificate=True";
        await using var db = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).Options);
        async Task RejectDuplicate(string sql)
        {
            try { await db.Database.ExecuteSqlRawAsync(sql); }
            catch (SqlException e) when (e.Number is 2601 or 2627) { checks++; return; }
            throw new InvalidOperationException("SQL accepted duplicate: " + sql);
        }

        try
        {
            // A new unique database: all migrations, including the historical view, run from zero.
            await db.Database.MigrateAsync();
            Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Fresh migration chain incomplete");
            Check(!db.Database.HasPendingModelChanges(), "Hardening snapshot differs from model");
            await db.Database.ExecuteSqlRawAsync("""
                SET IDENTITY_INSERT dbo.ProductTypes ON;
                INSERT dbo.ProductTypes(Id,Name,IsActive,IsLocked) VALUES
                  (1,N'Scientific',1,0),(2,N'Regional',1,0),(3,N'Conference',1,0),(99,N'Custom type',1,0);
                SET IDENTITY_INSERT dbo.ProductTypes OFF;
                SET IDENTITY_INSERT dbo.ProductAttributes ON;
                INSERT dbo.ProductAttributes(Id,Name,IsActive,IsLocked,DataType) VALUES
                  (8,N'DOI',1,0,0),(99,N'Custom attribute',1,0,0);
                SET IDENTITY_INSERT dbo.ProductAttributes OFF;
                SET IDENTITY_INSERT dbo.ProductAttributeDefinitions ON;
                INSERT dbo.ProductAttributeDefinitions(Id,ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder) VALUES
                  (108,1,8,0,0),(208,2,8,0,0),(308,3,8,0,0),(199,1,99,0,0),(999,99,99,0,0);
                SET IDENTITY_INSERT dbo.ProductAttributeDefinitions OFF;
                SET IDENTITY_INSERT dbo.Products ON;
                INSERT dbo.Products(Id,Title,ProductTypeId,IsActive,CreatedAt) VALUES
                  (101,N'First',1,1,SYSUTCDATETIME()),(102,N'Second',2,1,SYSUTCDATETIME()),
                  (103,N'Outside scope',3,1,SYSUTCDATETIME()),(104,N'Blank',1,1,SYSUTCDATETIME());
                SET IDENTITY_INSERT dbo.Products OFF;
                INSERT dbo.ProductValues(ProductId,AttributeDefinitionId,Value,CreatedAt) VALUES
                  (101,108,N'10.example/unique',SYSUTCDATETIME()),
                  (103,308,N'10.example/unique',SYSUTCDATETIME()),
                  (103,108,N'10.example/unique',SYSUTCDATETIME()),
                  (104,108,N'   ',SYSUTCDATETIME());
                """);
            await RejectDuplicate("""
                INSERT dbo.ProductAttributeDefinitions(ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder)
                VALUES(1,8,0,0);
                """);
            await RejectDuplicate("UPDATE dbo.ProductAttributeDefinitions SET ProductAttributeId=8 WHERE Id=199;");
            await RejectDuplicate("""
                INSERT dbo.ProductValues(ProductId,AttributeDefinitionId,Value,CreatedAt)
                VALUES(102,208,N'10.example/unique',SYSUTCDATETIME());
                """);
            await RejectDuplicate("""
                INSERT dbo.ProductValues(ProductId,AttributeDefinitionId,Value,CreatedAt)
                VALUES(102,208,N'  10.EXAMPLE/UNIQUE  ',SYSUTCDATETIME());
                """);
            await db.Database.ExecuteSqlRawAsync("""
                INSERT dbo.ProductValues(ProductId,AttributeDefinitionId,Value,CreatedAt)
                VALUES(102,208,N'10.example/other',SYSUTCDATETIME());
                """);
            await RejectDuplicate("UPDATE dbo.ProductValues SET Value=N'10.example/unique' WHERE ProductId=102;");
            // The same index also guards entering the Article scope via a type change.
            await RejectDuplicate("UPDATE dbo.Products SET ProductTypeId=1 WHERE Id=103;");
            // Changing a definition to DOI must not introduce a duplicate either.
            await db.Database.ExecuteSqlRawAsync("""
                DELETE dbo.ProductValues WHERE AttributeDefinitionId=108;
                DELETE dbo.ProductAttributeDefinitions WHERE Id=108;
                INSERT dbo.ProductValues(ProductId,AttributeDefinitionId,Value,CreatedAt)
                VALUES(101,199,N'10.example/other',SYSUTCDATETIME());
                """);
            await RejectDuplicate("UPDATE dbo.ProductAttributeDefinitions SET ProductAttributeId=8 WHERE Id=199;");
            Check(await db.ArticleReads.CountAsync() == 3, "Historical read view must remain usable");
            Console.WriteLine($"PASS: {checks} Article hardening SQL/seed assertions (fresh migration chain).");
        }
        finally
        {
            if (!database.StartsWith("tesis_articles_hardening_test_", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid fixture name");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
