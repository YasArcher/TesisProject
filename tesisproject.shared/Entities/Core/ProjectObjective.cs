using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class ProjectObjective
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ObjectiveTypeId { get; set; }
        public string Objetive { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;

        //Navegaciones
        public Project Project { get; set; } = null!;
        public ObjectiveType ObjectiveType { get; set; } = null!;
    }
}
