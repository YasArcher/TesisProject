using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Identity;

namespace tesisproject.backend.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    // Tu dominio existente
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleParticipant> ArticleParticipants => Set<ArticleParticipant>();

    // Auditoría
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m); // MUY IMPORTANTE para Identity

        m.Entity<Article>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(1000);
            e.Property(x => x.NombreRevista).HasMaxLength(300);
            e.Property(x => x.CodigoPublicacion).HasMaxLength(200);
            e.Property(x => x.CodigoISSN).HasMaxLength(32);
            e.Property(x => x.VolumenRevista).HasMaxLength(50);
            e.Property(x => x.NumeroRevista).HasMaxLength(50);
            e.Property(x => x.BaseDatos).HasMaxLength(100);
            e.Property(x => x.CampoAmplio).HasMaxLength(200);
            e.Property(x => x.CampoEspecifico).HasMaxLength(200);
            e.Property(x => x.CampoDetallado).HasMaxLength(200);
            e.Property(x => x.Quartil).HasMaxLength(10);
            e.Property(x => x.Filiacion).HasMaxLength(300);
            e.Property(x => x.Estado).HasMaxLength(50);
            e.Property(x => x.AccesoAbierto).HasMaxLength(10);
            e.Property(x => x.LinkPublicacion).HasMaxLength(1000);
            e.Property(x => x.EnlaceRevista).HasMaxLength(1000);
            e.Property(x => x.SJR).HasPrecision(6, 3);
        });

        m.Entity<ArticleParticipant>(e =>
        {
            e.Property(x => x.Identificacion).HasMaxLength(100);
            e.Property(x => x.Nombre).HasMaxLength(300);
            e.Property(x => x.Participacion).HasMaxLength(200);

            e.HasOne(x => x.Article)
             .WithMany(a => a.Participantes)
             .HasForeignKey(x => x.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ArticleId, x.Index }).IsUnique();
        });

        m.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(120);
            e.Property(x => x.EntityName).HasMaxLength(120);
            e.Property(x => x.EntityId).HasMaxLength(120);
            e.Property(x => x.Detail).HasMaxLength(4000);
        });
    }
}

public class Project
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
