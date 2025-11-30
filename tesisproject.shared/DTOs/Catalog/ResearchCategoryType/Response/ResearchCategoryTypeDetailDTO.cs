using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response
{
    public class ResearchCategoryTypeDetailDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public int ResearchCategoryGroupId { get; set; }

        public List<ResearchCategoryItem> Categories { get; set; } = new();

        public class ResearchCategoryItem
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }
    }
}