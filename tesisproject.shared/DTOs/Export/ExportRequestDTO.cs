using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Export
{
    public class ExportRequestDTO
    {
        public string? Name { get; set; } // opcional, para el nombre del archivo
        public List<ExportColumnDTO> Columns { get; set; } = new();

        public IEnumerable<int>? ProjectIds { get; set; }
    }
}
