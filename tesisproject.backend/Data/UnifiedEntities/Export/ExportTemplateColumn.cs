using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.backend.Data.UnifiedEntities.Export
{
    /// <summary>
    /// Columna concreta dentro de una plantilla:
    /// qué campo usa, qué cabecera lleva y en qué orden va.
    /// </summary>
    public class ExportTemplateColumn
    {
        public int Id { get; set; }

        // ================================
        //            FKs
        // ================================
        [Required]
        public int TemplateId { get; set; }

        [Required]
        public int ExportFieldId { get; set; }

        // ================================
        //          Column config
        // ================================
        /// <summary>
        /// Cabecera exacta que debe ir en el archivo (p.ej. "CODIGO", "LINEA_INVESTIGACION").
        /// </summary>
        [Required, StringLength(200)]
        public string TargetHeader { get; set; } = string.Empty;

        /// <summary>
        /// Posición (1, 2, 3, ...) dentro del archivo de exportación.
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// Indica si la columna es obligatoria para la plantilla.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Formato opcional (p.ej. "dd/MM/yyyy", "N2").
        /// </summary>
        [StringLength(100)]
        public string? Format { get; set; }

        /// <summary>
        /// Separador para listas (p.ej. "; " o " | ").
        /// </summary>
        [StringLength(10)]
        public string? Separator { get; set; }

        // ================================
        //          Navigation
        // ================================
        [ForeignKey(nameof(TemplateId))]
        public ExportTemplate Template { get; set; } = null!;

        [ForeignKey(nameof(ExportFieldId))]
        public ExportField ExportField { get; set; } = null!;
    }
}