using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    /// <summary>
    /// Vincula un Project con un Document (opcional) y guarda metadatos
    /// de resoluciones/aprobaciones heredados de la matriz histórica.
    /// </summary>
    public class ProjectDocument
    {
        // ================================
        //             Identity
        // ================================
        [Key]
        public int ProjectDocumentId { get; set; }

        // ================================
        //           Foreign Keys
        // ================================
        public int ProjectId { get; set; }

        /// <summary>
        /// Opcional. Cuando exista el archivo físico se creará un Document
        /// y se enlazará aquí.
        /// </summary>
        public int DocumentId { get; set; }

        public Project Project { get; set; } = null!;
        public Document Document { get; set; } = null!;
    }
}