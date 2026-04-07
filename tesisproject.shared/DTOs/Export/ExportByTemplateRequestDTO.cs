using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Export
{
    /// <summary>
    /// Solicitud para exportar Excel a partir de una plantilla persistida en BD,
    /// permitiendo seleccionar temporalmente qué columnas de la plantilla incluir.
    /// </summary>
    public class ExportByTemplateRequestDTO
    {
        /// <summary>
        /// Id de la plantilla a usar como base de exportación.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El TemplateId debe ser mayor a 0.")]
        public int TemplateId { get; set; }

        /// <summary>
        /// Lista de proyectos a exportar.
        /// Si es null o vacío, el backend puede decidir si exporta todos los del contexto actual
        /// o si lo trata como error, según la política definida.
        /// </summary>
        public IEnumerable<int>? ProjectIds { get; set; }

        /// <summary>
        /// Lista de ids de ExportTemplateColumn seleccionadas temporalmente para esta exportación.
        /// Deben pertenecer a la plantilla indicada en TemplateId.
        /// </summary>
        public IEnumerable<int>? IncludedTemplateColumnIds { get; set; }

        /// <summary>
        /// Nombre opcional para sobrescribir el nombre del archivo exportado.
        /// </summary>
        [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
        public string? NameOverride { get; set; }
    }
}