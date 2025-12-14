using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Export
{
    public class ExportColumnDTO
    {
        public string FieldKey { get; set; } = string.Empty;
        public string? Header { get; set; }
        public int OrderIndex { get; set; }
        public string? Format { get; set; }
        public string? Separator { get; set; }
    }
}
