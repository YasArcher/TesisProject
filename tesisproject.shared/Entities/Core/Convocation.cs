using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Core
{
    public class Convocation : CatalogEntityBase
    {
        [MaxLength(64)]
        public string? Code { get; set; }

        public ICollection<ConvocationRule>? Rules { get; set; }
        public ICollection<Project>? Projects { get; set; }
    }
}