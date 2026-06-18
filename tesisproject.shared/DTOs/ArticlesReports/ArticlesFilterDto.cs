// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using System;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesFilterDto
    {
        // Nivel de fecha a usar: "created" (por defecto) o "publication"
        public string? DateLevel { get; set; }

        // Rango global de años
        public int? FromYear { get; set; }
        public int? ToYear { get; set; }

        // Filtros más finos
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Quarter { get; set; }

        // Reservado si luego quieres manejar semana (WeekOfYear)
        public int? WeekOfYear { get; set; }

        // Dimensiones adicionales
        public int? FieldKey { get; set; }
        public int? ResearchLineKey { get; set; }

        // Open Access
        public bool? IsOpenAccess { get; set; }

        // Calidad
        public string? Quartile { get; set; }
    }
}

