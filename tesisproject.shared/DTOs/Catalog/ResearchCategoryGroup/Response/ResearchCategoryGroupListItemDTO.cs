using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Response
{
    public class ResearchCategoryGroupListItemDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
