using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.ConvocationRule.Request
{
    public class ConvocationRuleUpdateRequestDTO
    {
        [Required] public int Id { get; set; }
        [Required] public int ConvocationId { get; set; }
        [Required] public int ProductTypeId { get; set; }

        public int? MinDurationMonths { get; set; }
        public int? MaxDurationMonths { get; set; }

        [Range(0, 999)]
        public int Quantity { get; set; } = 1;

        [Required]
        public RequirementUnit Unit { get; set; } = RequirementUnit.PerProject;

        public Quartile MinQuartile { get; set; } = Quartile.None;

        public int? IndexingSourceId { get; set; }

        [StringLength(64)]
        public string? GroupCode { get; set; }

        [Range(1, 99)]
        public int? RequiredInGroup { get; set; }

        [StringLength(256)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
