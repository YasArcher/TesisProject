using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectResearchCategory.Request
{
    public class UpdateProjectResearchCategoryRequestDTO
    {
        public int Id { get; set; }  // alias estándar de clave primaria

        public int ProjectId { get; set; }
        public int ResearchCategoryId { get; set; }
    }
}
