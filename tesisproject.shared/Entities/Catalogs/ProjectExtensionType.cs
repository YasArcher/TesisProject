using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Entities.Core;

namespace tesisproject.shared.Entities.Catalogs
{
    public class ProjectExtensionType : CatalogEntityBase
    {
        public ICollection<ProjectExtension> ProjectExtensions { get; set; }
    = new List<ProjectExtension>();
    }
}
