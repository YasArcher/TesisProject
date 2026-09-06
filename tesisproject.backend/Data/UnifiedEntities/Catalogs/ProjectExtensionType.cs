using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedEntities.Catalogs
{
    public class ProjectExtensionType : CatalogEntityBase
    {
        public ICollection<ProjectExtension> ProjectExtensions { get; set; }
    = new List<ProjectExtension>();
    }
}
