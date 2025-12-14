using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Export
{
    /// <summary>
    /// Campo lógico exportable (PROJECT_CODE, FACULTY_NAME, FIELD_BROAD, etc.).
    /// Define de dónde viene y cómo se llama internamente.
    /// </summary>
    public class ExportField
    {
        public int Id { get; set; }

        /// <summary>
        /// Clave interna estable (p.ej. PROJECT_CODE, FIELD_DETAILED, COORDINATOR_NAME).
        /// </summary>
        [Required, StringLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Nombre amigable para la UI (p.ej. "Código de proyecto").
        /// </summary>
        [Required, StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Cabecera por defecto si una plantilla no define una propia.
        /// </summary>
        [StringLength(200)]
        public string? DefaultHeader { get; set; }

        /// <summary>
        /// Descripción corta para ayudar al usuario a saber qué es el campo.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Nombre lógico de la entidad origen
        /// (p.ej. "Projects", "Budgets", "Objectives").
        /// Solo documental.
        /// </summary>
        [StringLength(100)]
        public string? SourceEntity { get; set; }

        /// <summary>
        /// Propiedad o ruta interna
        /// (p.ej. "ProjectCode", "Objectives[General][0]", etc.).
        /// Solo documental.
        /// </summary>
        [StringLength(300)]
        public string? SourcePath { get; set; }

        /// <summary>
        /// Permite desactivar campos sin borrarlos.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ================================
        //          Navigation
        // ================================
        public ICollection<ExportTemplateColumn> TemplateColumns { get; set; } =
            new List<ExportTemplateColumn>();
    }
}