using System.Collections.Generic;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Entities.Core;

namespace tesisproject.shared.Entities.Catalogs
{
    public class ResearchCategory : CatalogEntityBase
    {
        // ================================
        //         Foreign Keys
        // ================================

        /// <summary>
        /// Tipo de categoría (Dominio, Línea, Sub-línea, Área, etc.)
        /// </summary>
        public int ResearchCategoryTypeId { get; set; }
        public ResearchCategoryType ResearchCategoryType { get; set; } = null!;


        /// <summary>
        /// Categoría padre dentro de la jerarquía (null si es raíz)
        /// </summary>
        public int? ParentCategoryId { get; set; }
        public ResearchCategory? ParentCategory { get; set; }


        // ================================
        //     Navigation Collections
        // ================================

        /// <summary>
        /// Hijos dentro de la jerarquía (subcategorías)
        /// </summary>
        public ICollection<ResearchCategory> SubCategories { get; set; }
            = new List<ResearchCategory>();


        /// <summary>
        /// Proyectos asociados a esta categoría
        /// </summary>
        public ICollection<ProjectResearchCategory> ProjectResearchCategories { get; set; }
            = new List<ProjectResearchCategory>();
    }
}
