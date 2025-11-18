using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.ConvocationRule.Response;

namespace tesisproject.shared.DTOs.Convocation.Response
{
    public class ConvocationDetailResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }

        public List<ConvocationRuleResponseDTO> Rules { get; set; } = new();
    }
}
