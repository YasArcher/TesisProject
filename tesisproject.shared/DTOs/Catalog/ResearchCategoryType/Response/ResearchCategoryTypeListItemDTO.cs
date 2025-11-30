using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response
{
    public class ResearchCategoryTypeListItemDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int ResearchCategoryGroupId { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// Number of research categories linked to this type.
        /// </summary>
        public int CategoriesCount { get; set; }
    }
}
