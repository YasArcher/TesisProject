using tesisproject.shared.DTOs.Matrices.Import;

namespace tesisproject.shared.DTOs.Matrices.Response
{
    public class ProjectMatrixUploadSummaryDTO
    {
        public int TotalRows { get; set; }
        public int DataRows { get; set; }
        public int SkippedRows { get; set; }

        public List<string> Headers { get; set; } = new();

        public List<ProjectMatrixUploadErrorDTO> Errors { get; set; } = new();

        /// <summary>
        /// Mapa de secciones y columnas según la matriz histórica.
        /// </summary>
        public ProjectMatrixColumnMapDTO? ColumnMap { get; set; }

        /// <summary>
        /// Proyectos importados resultantes de la matriz consolidada.
        /// </summary>
        public List<ImportedProjectDTO> ImportedProjects { get; set; } = new();
    }

    public class ProjectMatrixColumnMapDTO
    {
        /// <summary>
        /// Columnas que alimentan directamente a Project (convocatoria, código, nro, nombre, etc.).
        /// </summary>
        public List<int> ProjectColumns { get; set; } = new();

        /// <summary>
        /// Columnas financieras (valores, % ejecución).
        /// </summary>
        public List<int> FinancialColumns { get; set; } = new();

        /// <summary>
        /// Columnas de miembros internos.
        /// </summary>
        public List<int> InternalMemberColumns { get; set; } = new();

        /// <summary>
        /// Columnas de miembros externos / instituciones externas.
        /// </summary>
        public List<int> ExternalMemberColumns { get; set; } = new();

        /// <summary>
        /// Rango de columnas para prórrogas (RESOLUCIÓN PRIMERA PRORROGA .. antes de AVANCES 2013/2014).
        /// </summary>
        public MatrixRangeDTO? ExtensionRange { get; set; }

        /// <summary>
        /// Rango de columnas para visitas/avances (AVANCES 2013/2014 .. antes de RESOLUCION INFORME FINAL HCU).
        /// </summary>
        public MatrixRangeDTO? VisitRange { get; set; }

        /// <summary>
        /// Rango de columnas para categorías de investigación (LÍNEA DE INVESTIGACIÓN .. antes de OBJETIVO GENERAL).
        /// </summary>
        public MatrixRangeDTO? ResearchCategoryRange { get; set; }

        /// <summary>
        /// Columna de OBJETIVO GENERAL.
        /// </summary>
        public int? ObjectiveColumn { get; set; }

        /// <summary>
        /// Columnas de datos documentales (aprobación HCU, informe final, etc.).
        /// </summary>
        public List<int> DocumentColumns { get; set; } = new();

        /// <summary>
        /// Columnas que forman la clave natural de la fila (Convocatoria real, Código, Nro).
        /// Sirve para identificar a qué proyecto pertenece todo el bloque de datos.
        /// </summary>
        public List<int> NaturalKeyColumns { get; set; } = new();
    }

    public class MatrixRangeDTO
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class ProjectMatrixUploadErrorDTO
    {
        public int RowNumber { get; set; }
        public string? ColumnName { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
