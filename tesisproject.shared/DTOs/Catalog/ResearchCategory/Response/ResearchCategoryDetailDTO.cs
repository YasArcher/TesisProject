using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ResearchCategory.Response
{
    public class ResearchCategoryDetailDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public int ResearchCategoryTypeId { get; set; }
        public string? ResearchCategoryTypeName { get; set; }

        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
    }
}
