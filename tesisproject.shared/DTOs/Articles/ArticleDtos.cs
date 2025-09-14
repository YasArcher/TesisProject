using System;
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Articles;


public class ArticleParticipantDto
{
    public int Index { get; set; }
    public string? Identificacion { get; set; }
    public string? Nombre { get; set; }
    public string? Participacion { get; set; }
}

public class ArticleDto
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
    public List<ArticleParticipantDto> Participantes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ArticleParticipantRequest
{
    public int Index { get; set; }
    public string? Identificacion { get; set; }
    public string? Nombre { get; set; }
    public string? Participacion { get; set; }
}

public class CreateArticleRequest
{
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
    public List<ArticleParticipantRequest> Participantes { get; set; } = new();
}

public class UpdateArticleRequest : CreateArticleRequest
{
    public int Id { get; set; }
}