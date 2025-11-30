using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Project.Request
{
    public sealed class UpdateProjectResearchCategoriesRequestDTO
    {
        /// <summary>
        /// Lista de IDs de ResearchCategory asociadas al proyecto.
        /// Normalmente serán las categorías de nivel más bajo (líneas/sub-líneas).
        /// </summary>
        public List<int> ResearchCategoryIds { get; set; } = new();
    }
}
