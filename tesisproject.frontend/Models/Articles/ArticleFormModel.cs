using System.ComponentModel.DataAnnotations;

namespace tesisproject.frontend.Models.Articles;

public sealed class ArticleFormModel
{
    // Período
    [Required(ErrorMessage = "El período es obligatorio")]
    public string? PeriodoAcademico { get; set; }
    public string? Anio { get; set; }

    // Identificación/Revista
    public string? CodigoPublicacion { get; set; }
    [Required(ErrorMessage = "El título es obligatorio")]
    public string? Titulo { get; set; }
    public string? BaseDatos { get; set; }
    public string? CodigoISSN { get; set; }
    public string? NombreRevista { get; set; }
    public string? NumeroRevista { get; set; }
    public string? VolumenRevista { get; set; }
    public string? NumeroPaginas { get; set; }
    public string? SJR { get; set; }
    public string? FechaPublicacion { get; set; }   // date en UI

    // Clasificación
    public string? CampoAmplio { get; set; }
    public string? CampoEspecifico { get; set; }
    public string? CampoDetallado { get; set; }
    public string? Quartil { get; set; }
    public string? Filiacion { get; set; }

    // Estado & Acceso
    [Required(ErrorMessage = "El estado es obligatorio")]
    public string? Estado { get; set; }
    public string? AccesoAbierto { get; set; }

    // Enlaces
    [Url(ErrorMessage = "URL inválida")]
    public string? LinkPublicacion { get; set; }
    [Url(ErrorMessage = "URL inválida")]
    public string? EnlaceRevista { get; set; }

    // Participantes
    public string? Identificacion1 { get; set; }
    public string? Nombre1 { get; set; }
    public string? Participacion1 { get; set; }
    public string? Identificacion2 { get; set; }
    public string? Nombre2 { get; set; }
    public string? Participacion2 { get; set; }
    public string? Identificacion3 { get; set; }
    public string? Nombre3 { get; set; }
    public string? Participacion3 { get; set; }
    public string? Identificacion4 { get; set; }
    public string? Nombre4 { get; set; }
    public string? Participacion4 { get; set; }
    public string? Identificacion5 { get; set; }
    public string? Nombre5 { get; set; }
    public string? Participacion5 { get; set; }

    // Articulación
    public string? CodigoProyectoArticulado { get; set; }
    public string? ProyectoArticulado { get; set; }
    public string? LineaInvestigacionArticulada { get; set; }
    public string? GrupoInvestigacionArticulado { get; set; }
}
