using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ResearchCategory.Request
{
    public class AddResearchCategoryRequestDTO
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public int ResearchCategoryTypeId { get; set; }

        /// <summary>
        /// Parent category (null if this is a root category).
        /// </summary>
        public int? ParentCategoryId { get; set; }
    }
}