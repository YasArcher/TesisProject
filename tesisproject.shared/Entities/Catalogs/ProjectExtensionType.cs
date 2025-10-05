using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Catalogs
{
    public sealed class ProjectExtensionType : CatalogEntityBase
    {
        // ================================
        //        Core Information
        // ================================
        public bool IsBudgetExecutable { get; set; } = false; // Indica si la prórroga permite ejecutar presupuesto
    }
}
