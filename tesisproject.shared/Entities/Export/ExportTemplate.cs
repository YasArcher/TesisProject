using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Export
{
    /// <summary>
    /// Plantilla de exportación (CASES, CNA, reporte interno, etc.).
    /// </summary>
    public class ExportTemplate
    {
        public int Id { get; set; }

        /// <summary>
        /// Clave estable (p.ej. "CASES", "CNA", "INTERNAL_DEFAULT").
        /// </summary>
        [Required, StringLength(50)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Nombre amigable para mostrar en la UI.
        /// </summary>
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Sistema destino (p.ej. "CASES", "SENESCYT", "DIDE").
        /// Opcional, pero ayuda a clasificar.
        /// </summary>
        [StringLength(100)]
        public string? TargetSystem { get; set; }

        /// <summary>
        /// Versión del formato (p.ej. "v1", "2025").
        /// </summary>
        [StringLength(50)]
        public string? Version { get; set; }

        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // ================================
        //          Navigation
        // ================================
        public ICollection<ExportTemplateColumn> Columns { get; set; } =
            new List<ExportTemplateColumn>();
    }
}