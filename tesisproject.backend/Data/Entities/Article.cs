namespace tesisproject.backend.Data.Entities;

public class Article
{
    public int Id { get; set; }
    public string? PeriodoAcademico { get; set; }
    public int? Anio { get; set; }

    public string? CodigoPublicacion { get; set; }
    public string? CodigoISSN { get; set; }
    public string? Titulo { get; set; }
    public string? NombreRevista { get; set; }
    public string? VolumenRevista { get; set; }
    public string? NumeroRevista { get; set; }
    public int? NumeroPaginas { get; set; }
    public decimal? SJR { get; set; }
    public DateTime? FechaPublicacion { get; set; }

    public string? BaseDatos { get; set; }
    public string? CampoAmplio { get; set; }
    public string? CampoEspecifico { get; set; }
    public string? CampoDetallado { get; set; }
    public string? Quartil { get; set; }
    public string? Filiacion { get; set; }

    public string? Estado { get; set; }
    public string? AccesoAbierto { get; set; }

    public string? LinkPublicacion { get; set; }
    public string? EnlaceRevista { get; set; }

    public string? CodigoProyectoArticulado { get; set; }
    public string? ProyectoArticulado { get; set; }
    public string? LineaInvestigacionArticulada { get; set; }
    public string? GrupoInvestigacionArticulado { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ArticleParticipant> Participantes { get; set; } = new List<ArticleParticipant>();
}
