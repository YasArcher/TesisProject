using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimResearchCategory
    {
        [Key]
        public int ResearchCategoryKey { get; set; }   // surrogate en DW

        // Clave natural desde ResearchCategory
        public int ResearchCategoryId { get; set; }

        // Info básica de la categoría
        public string Name { get; set; } = string.Empty;

        // Tipo (Dominio, Línea, Sublínea) desde ResearchCategoryType
        public int ResearchCategoryTypeId { get; set; }
        public string CategoryTypeName { get; set; } = string.Empty;

        // Grupo desde ResearchCategoryGroup (ej. "Áreas de investigación")
        public int ResearchCategoryGroupId { get; set; }
        public string ResearchCategoryGroupName { get; set; } = string.Empty;

        // Jerarquía padre-hijo (si luego quieres navegación, se puede extender)
        public int? ParentCategoryKey { get; set; }

        // 1 categoría → N enlaces a proyectos
        public ICollection<BridgeProjectResearchCategory> ProjectLinks { get; set; } = new List<BridgeProjectResearchCategory>();
    }
}