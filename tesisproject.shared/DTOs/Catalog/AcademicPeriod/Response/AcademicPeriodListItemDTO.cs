using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.AcademicPeriod.Response
{
    public class AcademicPeriodListItemDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
