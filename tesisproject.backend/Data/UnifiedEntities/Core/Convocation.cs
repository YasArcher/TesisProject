using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class Convocation : CatalogEntityBase
    {
        public string? Code { get; set; }

        public ICollection<ConvocationRule>? Rules { get; set; }
        public ICollection<Project>? Projects { get; set; }
    }
}