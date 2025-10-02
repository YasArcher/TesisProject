using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Filters
{
    public sealed class KeyValueItemDTO
    {
        // Para catálogos
        public int Id { get; set; }
        public string Name { get; set; } = "";

        // Para selects en UI
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
    }
}

