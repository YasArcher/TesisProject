using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Filters
{
    public sealed class ProjectsFilterBootstrapDTO
    {

        public List<KeyValueItemDTO> ProjectStates { get; set; } = [];
        public List<KeyValueItemDTO> ProjectTypes { get; set; } = [];
        public List<ProjectExtensionTypeItemDTO>? ExtensionTypes { get; set; } = [];
        public List<KeyValueItemDTO> Faculties { get; set; } = [];
        public List<KeyValueItemDTO> Funding{ get; set; } = [];
        public List<ResearchCategoryFilterTypeDTO> ResearchCategoryTypes { get; set; } = new();
    }

    public sealed class ProjectExtensionTypeItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsBudgetExecutable { get; set; }
    }
}
