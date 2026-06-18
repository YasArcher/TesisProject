using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.ConvocationRule.Response
{
    public class ConvocationRuleResponseDTO
    {
        public int Id { get; set; }
        public int ConvocationId { get; set; }
        public int ProductTypeId { get; set; }

        public int? MinDurationMonths { get; set; }
        public int? MaxDurationMonths { get; set; }

        public int Quantity { get; set; }
        public RequirementUnit Unit { get; set; }

        public Quartile MinQuartile { get; set; }
        public int? IndexingSourceId { get; set; }
        public string? IndexingSourceName { get; set; }

        public string? GroupCode { get; set; }
        public int? RequiredInGroup { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }
}
