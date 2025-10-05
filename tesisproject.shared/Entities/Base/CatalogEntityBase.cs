using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Abstractions;

namespace tesisproject.shared.Entities.Base
{
    public abstract class CatalogEntityBase : ICatalogEntity
    {
        // ================================
        //              Keys
        // ================================
        public int Id { get; set; } // Identificador único del catálogo

        // ================================
        //        Core Information
        // ================================
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty; // Nombre del elemento del catálogo

        public bool IsActive { get; set; } = true;       // Estado activo/inactivo
    }
}
