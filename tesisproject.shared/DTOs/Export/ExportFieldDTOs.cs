using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Export
{
    public class ExportFieldListItemDTO
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DefaultHeader { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}