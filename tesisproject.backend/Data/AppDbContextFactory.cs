using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace tesisproject.backend.Data
{
    // EF usa esta fábrica en tiempo de diseño (dotnet ef ...)
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Carga appsettings.Development.json si existe, luego appsettings.json
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var config = builder.Build();
            var cs = config.GetConnectionString("DefaultConnection");

            // Fallback seguro — DEBE ser EXACTAMENTE la misma que usas en Program.cs
            if (string.IsNullOrWhiteSpace(cs))
            {
                cs = "Server=PERSONAL\\DINNOVA;Database=TesisDB_Extensible;User Id=sa;Password=admin123;Encrypt=False;TrustServerCertificate=True";
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(cs);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
