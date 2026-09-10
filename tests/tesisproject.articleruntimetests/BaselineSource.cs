using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

internal static class BaselineSource
{
    public static async Task InspectAsync()
    {
        var config = new ConfigurationBuilder().SetBasePath(Path.GetFullPath("tesisproject.backend"))
            .AddJsonFile("appsettings.Development.json").AddJsonFile("appsettings.Local.json", optional:true).AddEnvironmentVariables().Build();
        var output = Path.GetFullPath("artifacts/articles-baseline-source"); Directory.CreateDirectory(output);
        foreach (var key in new[] { "DefaultConnection", "ArticlesOltpConnection", "UnifiedDideConnection" })
        {
            var connection = config.GetConnectionString(key);
            if (string.IsNullOrWhiteSpace(connection)) { Console.WriteLine(key + ": missing configuration"); continue; }
            var target = new SqlConnectionStringBuilder(connection) { ConnectTimeout = 10 };
            Console.WriteLine(key + ": " + target.DataSource + "/" + target.InitialCatalog);
            await using var db = new SqlConnection(target.ConnectionString);
            try { await db.OpenAsync(); }
            catch (SqlException e) { Console.WriteLine("Unavailable SQL source: " + e.Number); continue; }
            using var tables = db.CreateCommand();
            tables.CommandText = "SELECT s.name+'.'+t.name FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id WHERE t.name IN ('ProductTypes','ProductAttributes','ProductAttributeDefinitions','FormDefinitions','FormFields','FormFieldDefinitions','FieldCatalog','FieldCatalogEntries','DynamicFieldOptions') ORDER BY s.name,t.name";
            var names = new List<string>();
            await using (var reader = await tables.ExecuteReaderAsync()) while (await reader.ReadAsync()) names.Add(reader.GetString(0));
            foreach (var name in names)
            {
                using var query = db.CreateCommand();
                query.CommandText = "SELECT * FROM " + string.Join('.', name.Split('.').Select(n => "["+n.Replace("]","]]")+"]"));
                var rows = new List<Dictionary<string,object?>>();
                await using (var reader = await query.ExecuteReaderAsync()) while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string,object?>();
                    for (int i=0;i<reader.FieldCount;i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(row);
                }
                await File.WriteAllTextAsync(Path.Combine(output,key+"-"+name+".json"), JsonSerializer.Serialize(rows,new JsonSerializerOptions { WriteIndented=true }));
                Console.WriteLine(name + ": " + rows.Count + " rows");
            }
        }
    }
}
